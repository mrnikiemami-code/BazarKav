using Serilog.Events;

namespace BazarKav.BuildingBlocks.Observability.Logging;

public sealed class BazarKavLoggingOptions
{
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;
    public bool ConsoleEnabled { get; set; } = true;
    public string? FilePath { get; set; }
    public int RetainedFileCountLimit { get; set; } = 14;
}
