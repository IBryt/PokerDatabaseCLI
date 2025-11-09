namespace PokerDatabaseCLI.Core;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public abstract record Result<T>
{
    public record Success(T Value) : Result<T>;
    public record Failure(string Error) : Result<T>;
    public static Result<T> Ok(T value) => new Success(value);
    public static Result<T> Fail(string error) => new Failure(error);
}