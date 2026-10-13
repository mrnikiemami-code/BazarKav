using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BazarKav.BuildingBlocks.Observability.Tracing;

public static class BazarKavTelemetry
{
    public const string Name = "BazarKav";
    public static readonly ActivitySource Activities = new(Name);
    public static readonly Meter Meter = new(Name);
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "bazarkav.http.request.duration", "ms", "Duration of HTTP requests.");
    public static readonly Counter<long> UnexpectedErrors = Meter.CreateCounter<long>(
        "bazarkav.http.unexpected_errors");

    // Register the ActivitySource/Meter with a future OpenTelemetry exporter via the public Name.
    // Background owners must pass trusted identifiers; HTTP headers do not establish business scope.
    public static Activity? StartBackgroundActivity(string operation, string jobId,
        ActivityContext parent = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        var activity = Activities.StartActivity(operation, ActivityKind.Internal, parent);
        activity?.SetTag("job.id", jobId);
        return activity;
    }
}
