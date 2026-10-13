using System.Diagnostics;
using BazarKav.BuildingBlocks.Observability.Correlation;
using BazarKav.BuildingBlocks.Observability.Tracing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BazarKav.BuildingBlocks.Web.Middleware;

public sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Preserve the server's Activity. Without an active diagnostic listener, supply a fallback
        // so logs and ProblemDetails still have a genuine W3C TraceId.
        using var fallbackActivity = Activity.Current is null ? StartFallbackActivity(context) : null;
        var values = context.Request.Headers[CorrelationId.HeaderName];
        var id = values.Count == 1 && CorrelationId.IsValid(values[0]) ? values[0]! : CorrelationId.Create();
        context.Items[CorrelationId.ItemKey] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationId.HeaderName] = id;
            return Task.CompletedTask;
        });
        // ASP.NET Core owns the HTTP Activity; don't overwrite its traceparent or TraceId.
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = id,
            ["TraceId"] = Activity.Current?.TraceId.ToString(),
            ["SpanId"] = Activity.Current?.SpanId.ToString()
        });
        var started = Stopwatch.GetTimestamp();
        try { await next(context); }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            BazarKavTelemetry.RequestDuration.Record(duration,
                new KeyValuePair<string, object?>("http.response.status_code", context.Response.StatusCode));
            logger.LogInformation("HTTP request ended with {StatusCode} in {DurationMs} ms",
                context.Response.StatusCode, duration);
        }
    }

    private static Activity StartFallbackActivity(HttpContext context)
    {
        var activity = new Activity("BazarKav.Http").SetIdFormat(ActivityIdFormat.W3C);
        var values = context.Request.Headers["traceparent"];
        if (values.Count == 1 && ActivityContext.TryParse(values[0], null, out var parent))
            activity.SetParentId(parent.TraceId, parent.SpanId, parent.TraceFlags);
        return activity.Start();
    }
}
