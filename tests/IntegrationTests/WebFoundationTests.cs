using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.BuildingBlocks.Observability.Correlation;
using BazarKav.BuildingBlocks.Observability.Logging;
using BazarKav.BuildingBlocks.Web;
using BazarKav.BuildingBlocks.Web.Errors;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BazarKav.IntegrationTests;

public sealed class WebFoundationTests
{
    private static async Task<WebApplication> StartAsync(ILoggerProvider? provider = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        if (provider is not null) builder.Logging.AddProvider(provider);
        builder.Services.AddBazarKavWeb();
        var app = builder.Build();
        app.UseBazarKavWeb();
        app.Run(async context =>
        {
            switch (context.Request.Path.Value)
            {
                case "/boom": throw new InvalidOperationException("password=DO-NOT-EXPOSE");
                case "/invalid":
                    throw new ValidationException([new ValidationFailure("Name", "DO-NOT-EXPOSE")
                    { AttemptedValue = "DO-NOT-EXPOSE" }]);
                case "/conflict":
                    await Result<int>.Failure(new Error("sample.conflict", "تعارض وجود دارد.", ErrorKind.Conflict))
                        .ToHttpResult(context).ExecuteAsync(context);
                    break;
                case "/trace":
                    await context.Response.WriteAsJsonAsync(new { traceId = Activity.Current?.TraceId.ToString() });
                    break;
                case "/ok":
                    await Task.Yield();
                    await context.Response.WriteAsJsonAsync(new { correlationId = context.Items[CorrelationId.ItemKey] });
                    break;
                default: context.Response.StatusCode = 404; break;
            }
        });
        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task Unexpected_error_is_safe_correlated_and_logged_once()
    {
        var recorder = new ErrorRecorder();
        await using var app = await StartAsync(recorder);
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/boom");
        request.Headers.Add(CorrelationId.HeaderName, "request-123");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("request-123", response.Headers.GetValues(CorrelationId.HeaderName).Single());
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DO-NOT-EXPOSE", text);
        using var body = JsonDocument.Parse(text);
        Assert.Equal("unexpected.error", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("request-123", body.RootElement.GetProperty("correlationId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
        Assert.Single(recorder.Errors);
        Assert.DoesNotContain("DO-NOT-EXPOSE", recorder.Errors.Single());
    }

    [Fact]
    public async Task Validation_never_exposes_attempted_values_or_interpolated_messages()
    {
        var recorder = new ErrorRecorder();
        await using var app = await StartAsync(recorder);
        using var response = await app.GetTestClient().GetAsync("/invalid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DO-NOT-EXPOSE", text);
        using var body = JsonDocument.Parse(text);
        Assert.Equal("validation.failed", body.RootElement.GetProperty("code").GetString());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("Name", out _));
        Assert.Empty(recorder.Errors);
    }

    [Theory]
    [InlineData("safe-123", true)]
    [InlineData("bad id", false)]
    [InlineData("a,b", false)]
    public async Task Correlation_is_preserved_or_replaced(string supplied, bool preserve)
    {
        await using var app = await StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, supplied);
        using var response = await app.GetTestClient().SendAsync(request);
        var actual = response.Headers.GetValues(CorrelationId.HeaderName).Single();
        Assert.True(CorrelationId.IsValid(actual));
        Assert.Equal(preserve, actual == supplied);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(actual, body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Parallel_requests_do_not_share_correlation()
    {
        await using var app = await StartAsync();
        using var client = app.GetTestClient();
        var ids = await Task.WhenAll(Enumerable.Range(0, 12).Select(async i =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
            request.Headers.Add(CorrelationId.HeaderName, $"parallel-{i}");
            using var response = await client.SendAsync(request);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal($"parallel-{i}", body.RootElement.GetProperty("correlationId").GetString());
            return response.Headers.GetValues(CorrelationId.HeaderName).Single();
        }));
        Assert.Equal(12, ids.Distinct().Count());
    }

    [Fact]
    public async Task Traceparent_is_preserved_by_http_activity()
    {
        await using var app = await StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/trace");
        const string traceId = "0123456789abcdef0123456789abcdef";
        request.Headers.Add("traceparent", $"00-{traceId}-0123456789abcdef-01");
        using var response = await app.GetTestClient().SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(traceId, body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Existing_server_activity_is_preserved_when_a_listener_is_active()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        await using var app = await StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/trace");
        const string traceId = "fedcba9876543210fedcba9876543210";
        request.Headers.Add("traceparent", $"00-{traceId}-fedcba9876543210-01");
        using var response = await app.GetTestClient().SendAsync(request);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(traceId, body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Missing_traceparent_produces_a_w3c_trace_and_status_pages_are_correlated()
    {
        await using var app = await StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("http.404", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("اطلاعات موردنظر پیدا نشد.", body.RootElement.GetProperty("title").GetString());
        var trace = body.RootElement.GetProperty("traceId").GetString();
        Assert.Equal(32, trace?.Length);
        Assert.Equal(response.Headers.GetValues(CorrelationId.HeaderName).Single(),
            body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Typed_expected_error_maps_to_http_and_has_no_error_log()
    {
        var recorder = new ErrorRecorder();
        await using var app = await StartAsync(recorder);
        using var response = await app.GetTestClient().GetAsync("/conflict");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("sample.conflict", body.RootElement.GetProperty("code").GetString());
        Assert.Empty(recorder.Errors);
    }

    [Theory]
    [InlineData(ErrorKind.Validation, 400)]
    [InlineData(ErrorKind.Unauthorized, 401)]
    [InlineData(ErrorKind.Forbidden, 403)]
    [InlineData(ErrorKind.NotFound, 404)]
    [InlineData(ErrorKind.Conflict, 409)]
    [InlineData(ErrorKind.Unavailable, 503)]
    [InlineData(ErrorKind.Timeout, 504)]
    public void Error_mapping_is_explicit(ErrorKind kind, int expected) =>
        Assert.Equal(expected, ProblemDetailsMapper.FromError(new Error("test.code", "پیام امن", kind),
            new DefaultHttpContext()).Status);

    [Fact]
    public async Task Cancellation_is_left_to_the_transport_not_mapped_to_500()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBazarKavWeb();
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetServices<Microsoft.AspNetCore.Diagnostics.IExceptionHandler>().Single();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        context.RequestAborted = cts.Token;
        Assert.False(await handler.TryHandleAsync(context, new OperationCanceledException(cts.Token), cts.Token));
        Assert.Equal(0, context.Response.Body.Length);
        Assert.NotEqual(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task Serilog_writes_structured_scopes_and_applies_file_options()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using (var factory = LoggerFactory.Create(builder => builder.AddBazarKavLogging(options =>
            {
                options.ConsoleEnabled = false;
                options.FilePath = Path.Combine(directory, "events-.json");
                options.RetainedFileCountLimit = 2;
            })))
            {
                var logger = factory.CreateLogger("test");
                using var scope = logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = "file-test" });
                logger.LogInformation("Operation {Operation} finished", "Example");
            }
            var text = await File.ReadAllTextAsync(Directory.GetFiles(directory).Single());
            using var body = JsonDocument.Parse(text.Trim());
            Assert.Equal("file-test", body.RootElement.GetProperty("Properties").GetProperty("CorrelationId").GetString());
            Assert.Equal("Example", body.RootElement.GetProperty("Properties").GetProperty("Operation").GetString());
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private sealed class ErrorRecorder : ILoggerProvider
    {
        public ConcurrentQueue<string> Errors { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Recorder(this);
        public void Dispose() { }
        private sealed class Recorder(ErrorRecorder owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (level >= LogLevel.Error) owner.Errors.Enqueue(formatter(state, exception));
            }
        }
    }
}
