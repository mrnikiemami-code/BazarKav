namespace BazarKav.BuildingBlocks.Application.Abstractions;

public sealed class Result<T>
{
    private readonly T? value;
    private readonly Error? error;
    public bool IsSuccess { get; }
    public T Value => IsSuccess ? value! : throw new InvalidOperationException("A failed result has no value.");
    public Error Error => !IsSuccess ? error! : throw new InvalidOperationException("A successful result has no error.");

    private Result(T value) { this.value = value; IsSuccess = true; }
    private Result(Error error) { this.error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));
}
