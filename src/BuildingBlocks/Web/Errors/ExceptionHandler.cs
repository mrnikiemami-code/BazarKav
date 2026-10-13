using BazarKav.BuildingBlocks.Observability.Tracing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BazarKav.BuildingBlocks.Web.Errors;

public sealed class ExceptionHandler(IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        // Do not convert cancellation to a response, or to an unexpected server error.
        if (exception is OperationCanceledException) return false;
        if (httpContext.Response.HasStarted) return false;
        var problem = ProblemDetailsMapper.FromException(exception, httpContext);
        httpContext.Response.StatusCode = problem.Status!.Value;
        if (problem.Status >= 500)
        {
            BazarKavTelemetry.UnexpectedErrors.Add(1);
            // Exception messages/inner messages can contain credentials. Log safe diagnostics only.
            logger.LogError("Unhandled {ExceptionType} ({ErrorCode}); stack {StackTrace}",
                exception.GetType().FullName, exception.HResult, exception.StackTrace);
        }
        var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
        if (!written)
            await httpContext.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
