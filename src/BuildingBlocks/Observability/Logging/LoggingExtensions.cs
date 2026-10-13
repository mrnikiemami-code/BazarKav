using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

namespace BazarKav.BuildingBlocks.Observability.Logging;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddBazarKavLogging(this ILoggingBuilder builder,
        Action<BazarKavLoggingOptions>? configure = null)
    {
        var options = new BazarKavLoggingOptions();
        configure?.Invoke(options);
        if (options.RetainedFileCountLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(options.RetainedFileCountLimit));
        if (!options.ConsoleEnabled && string.IsNullOrWhiteSpace(options.FilePath))
            throw new ArgumentException("At least one logging sink is required.");
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(options.MinimumLevel)
            .Enrich.FromLogContext();
        if (options.ConsoleEnabled)
            configuration.WriteTo.Console(new JsonFormatter(renderMessage: true));
        if (!string.IsNullOrWhiteSpace(options.FilePath))
            configuration.WriteTo.File(new JsonFormatter(renderMessage: true), options.FilePath,
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: options.RetainedFileCountLimit);
        builder.ClearProviders();
        builder.AddSerilog(configuration.CreateLogger(), dispose: true);
        return builder;
    }
}
