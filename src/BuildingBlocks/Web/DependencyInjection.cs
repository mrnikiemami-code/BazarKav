using System.Diagnostics;
using BazarKav.BuildingBlocks.Observability.Correlation;
using BazarKav.BuildingBlocks.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BazarKav.BuildingBlocks.Web;

public static class DependencyInjection
{
    public static IServiceCollection AddBazarKavWeb(this IServiceCollection services)
    {
        services.AddExceptionHandler<Errors.ExceptionHandler>();
        // .NET 10: our owner logs unexpected errors once; framework diagnostics are suppressed for handled errors.
        services.Configure<ExceptionHandlerOptions>(options =>
            options.SuppressDiagnosticsCallback = _ => true);
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var mapped = Errors.ProblemDetailsMapper.FromStatus(
                context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode, context.HttpContext);
            context.ProblemDetails.Title = mapped.Title;
            context.ProblemDetails.Type = mapped.Type;
            context.ProblemDetails.Extensions.TryAdd("code", $"http.{context.ProblemDetails.Status ?? 500}");
            context.ProblemDetails.Extensions["correlationId"] =
                context.HttpContext.Items[CorrelationId.ItemKey] as string ?? context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
        });
        return services;
    }

    public static IApplicationBuilder UseBazarKavWeb(this IApplicationBuilder app)
    {
        // Correlation remains alive while the exception handler produces ProblemDetails.
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
