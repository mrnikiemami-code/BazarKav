namespace BazarKav.BuildingBlocks.Observability.Correlation;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "BazarKav.CorrelationId";
    public const int MaximumLength = 64;

    public static bool IsValid(string? value) => value is { Length: > 0 and <= MaximumLength }
        && value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_');

    public static string Create() => Guid.NewGuid().ToString("N");
}
