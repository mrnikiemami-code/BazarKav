namespace BazarKav.BuildingBlocks.Application.Abstractions;

public enum ErrorKind { Validation, NotFound, Conflict, Unauthorized, Forbidden, Unavailable, Timeout }

/// <summary>Only reviewed, public-safe messages belong here. No upstream exception text.</summary>
public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorKind Kind { get; }

    public Error(string code, string message, ErrorKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Code = code;
        Message = message;
        Kind = kind;
    }
}
