using BazarKav.BuildingBlocks.Application;
using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.BuildingBlocks.Application.Behaviors;
using BazarKav.BuildingBlocks.Observability.Correlation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BazarKav.UnitTests;

public sealed class BuildingBlocksTests
{
    [Fact]
    public void Results_cannot_expose_the_wrong_branch()
    {
        var success = Result<int>.Success(7);
        Assert.Equal(7, success.Value);
        Assert.Throws<InvalidOperationException>(() => success.Error);
        var failure = Result<int>.Failure(new Error("sample.missing", "پیدا نشد.", ErrorKind.NotFound));
        Assert.False(failure.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => failure.Value);
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
    }

    [Theory]
    [InlineData("safe-ID_123", true)]
    [InlineData("", false)]
    [InlineData("id with space", false)]
    [InlineData("injection\r\n", false)]
    [InlineData("شناسه", false)]
    public void Correlation_accepts_only_bounded_safe_identifiers(string value, bool expected) =>
        Assert.Equal(expected, CorrelationId.IsValid(value));

    [Fact]
    public void Correlation_rejects_oversize() => Assert.False(CorrelationId.IsValid(new string('a', 65)));

    [Fact]
    public async Task Real_mediator_pipeline_validates_before_handler_and_never_logs_payload()
    {
        var recorder = new CaptureLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(recorder));
        services.AddBazarKavApplication(typeof(BuildingBlocksTests).Assembly);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var exception = await Assert.ThrowsAsync<ValidationException>(() => sender.Send(new SampleQuery("")));
        Assert.Single(exception.Errors);
        var response = await sender.Send(new SampleQuery("private-payload"));
        Assert.Equal(15, response);
        Assert.DoesNotContain(recorder.Messages, message => message.Contains("private-payload"));
        Assert.Contains(recorder.Messages, message => message.Contains("Failed"));
        Assert.Contains(recorder.Messages, message => message.Contains("Completed"));
    }

    [Fact]
    public async Task Cancellation_reaches_async_validator_and_blocks_handler()
    {
        using var cts = new CancellationTokenSource();
        var validator = new InlineValidator<SampleQuery>();
        validator.RuleFor(request => request.Text).MustAsync(async (_, token) =>
        {
            Assert.Equal(cts.Token, token);
            await cts.CancelAsync();
            token.ThrowIfCancellationRequested();
            return true;
        });
        var called = false;
        var behavior = new ValidationBehavior<SampleQuery, int>([validator]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(new SampleQuery("x"), _ => { called = true; return Task.FromResult(1); }, cts.Token));
        Assert.False(called);
    }

    [Fact]
    public async Task Validators_execute_sequentially_and_aggregate_failures()
    {
        var active = 0;
        var maximum = 0;
        InlineValidator<SampleQuery> MakeValidator()
        {
            var validator = new InlineValidator<SampleQuery>();
            validator.RuleFor(request => request.Text).MustAsync(async (_, _) =>
            {
                maximum = Math.Max(maximum, ++active);
                await Task.Yield();
                active--;
                return false;
            });
            return validator;
        }
        var behavior = new ValidationBehavior<SampleQuery, int>([MakeValidator(), MakeValidator()]);
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new SampleQuery("x"), _ => Task.FromResult(1), default));
        Assert.Equal(2, exception.Errors.Count());
        Assert.Equal(1, maximum);
    }

    [Fact]
    public async Task Precancelled_request_never_invokes_handler()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var behavior = new ValidationBehavior<SampleQuery, int>([]);
        var called = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(new SampleQuery("x"),
            _ => { called = true; return Task.FromResult(1); }, cts.Token));
        Assert.False(called);
    }

    [Fact]
    public async Task Logging_cancellation_is_not_an_error_and_is_rethrown()
    {
        var recorder = new CaptureLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(recorder));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var behavior = new LoggingBehavior<SampleQuery, int>(factory.CreateLogger<LoggingBehavior<SampleQuery, int>>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(new SampleQuery("secret"), _ => Task.FromResult(1), cts.Token));
        Assert.Contains(recorder.Messages, message => message.Contains("Cancelled"));
        Assert.Empty(recorder.Exceptions);
    }
}

public sealed record SampleQuery(string Text) : IQuery<int>;
public sealed class SampleQueryValidator : AbstractValidator<SampleQuery>
{
    public SampleQueryValidator() => RuleFor(request => request.Text).NotEmpty();
}
public sealed class SampleQueryHandler : IRequestHandler<SampleQuery, int>
{
    public Task<int> Handle(SampleQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(request.Text.Length);
}
internal sealed class CaptureLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = [];
    public List<Exception> Exceptions { get; } = [];
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this);
    public void Dispose() { }
    private sealed class CaptureLogger(CaptureLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            owner.Messages.Add(formatter(state, exception));
            if (exception is not null) owner.Exceptions.Add(exception);
        }
    }
}
