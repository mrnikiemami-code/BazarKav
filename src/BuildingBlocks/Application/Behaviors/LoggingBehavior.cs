using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BazarKav.BuildingBlocks.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var outcome = "Failed";
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["Operation"] = typeof(TRequest).Name,
            ["Module"] = typeof(TRequest).Assembly.GetName().Name,
            ["TraceId"] = Activity.Current?.TraceId.ToString(),
            ["SpanId"] = Activity.Current?.SpanId.ToString()
        });
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await next(cancellationToken);
            outcome = "Completed";
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = "Cancelled";
            throw;
        }
        finally
        {
            // No request/response or exception is serialized here. The owning boundary logs failures.
            logger.LogInformation("Request {Operation} ended as {Outcome} in {DurationMs} ms",
                typeof(TRequest).Name, outcome, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
