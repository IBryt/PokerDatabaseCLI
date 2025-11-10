namespace PokerDatabaseCLI.Core;

/// <summary>
/// Provides functional extension methods for working with <see cref="Result{T}"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Applies a transformation function to the value inside a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TIn">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">The mapping function applied when the result is successful.</param>
    /// <returns>
    /// A new <see cref="Result{T}"/> containing the mapped value if successful,
    /// or the original failure if not.
    /// </returns>
    public static Result<TOut> Map<TIn, TOut>(
           this Result<TIn> result,
           Func<TIn, TOut> mapper) =>
           result switch
           {
               Result<TIn>.Success success => Try(mapper, success.Value),
               Result<TIn>.Failure failure => Result<TOut>.Fail(failure.Error),
               _ => throw new InvalidOperationException()
           };

    /// <summary>
    /// Chains two operations that return <see cref="Result{T}"/> values (also known as <c>FlatMap</c>).
    /// </summary>
    /// <typeparam name="TIn">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="binder">A function that returns the next <see cref="Result{T}"/>.</param>
    /// <returns>
    /// The result of the binder if the current result is successful;
    /// otherwise, the current failure.
    /// </returns>
    public static Result<TOut> Bind<TIn, TOut>(
            this Result<TIn> result,
            Func<TIn, Result<TOut>> binder) =>
            result switch
            {
                Result<TIn>.Success success => Try(binder, success.Value),
                Result<TIn>.Failure failure => Result<TOut>.Fail(failure.Error),
                _ => throw new InvalidOperationException()
            };

    /// <summary>
    /// Pattern-matches a <see cref="Result{T}"/> to handle both success and failure cases.
    /// </summary>
    /// <typeparam name="TIn">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the return value.</typeparam>
    /// <param name="result">The result to match on.</param>
    /// <param name="onSuccess">The function to execute when successful.</param>
    /// <param name="onFailure">The function to execute when failed.</param>
    /// <returns>The result of either <paramref name="onSuccess"/> or <paramref name="onFailure"/>.</returns>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<string, TOut> onFailure) =>
        result switch
        {
            Result<TIn>.Success success => onSuccess(success.Value),
            Result<TIn>.Failure failure => onFailure(failure.Error),
            _ => throw new InvalidOperationException()
        };

    /// <summary>
    /// Pattern-matches a <see cref="Result{T}"/> and performs side effects (for example, I/O operations).
    /// </summary>
    /// <typeparam name="T">The type of the value contained in the result.</typeparam>
    /// <param name="result">The result to handle.</param>
    /// <param name="onSuccess">The action to perform on success.</param>
    /// <param name="onFailure">The action to perform on failure.</param>
    public static void Match<T>(
        this Result<T> result,
        Action<T> onSuccess,
        Action<string> onFailure)
    {
        switch (result)
        {
            case Result<T>.Success success:
                onSuccess(success.Value);
                break;
            case Result<T>.Failure failure:
                onFailure(failure.Error);
                break;
        }
    }

    private static Result<TOut> Try<TIn, TOut>(Func<TIn, TOut> func, TIn input)
    {
        try
        {
            return Result<TOut>.Ok(func(input));
        }
        catch (Exception e)
        {
            return Result<TOut>.Fail(e.Message);
        }
    }

    private static Result<TOut> Try<TIn, TOut>(Func<TIn, Result<TOut>> func, TIn input)
    {
        try
        {
            return func(input);
        }
        catch (Exception e)
        {
            return Result<TOut>.Fail(e.Message);
        }
    }
}


/// <summary>
/// Provides general-purpose functional programming utilities such as <c>Pipe</c> and <c>Compose</c>.
/// </summary>
public static class FunctionalExtensions
{
    /// <summary>
    /// Pipes a value through a pure function, allowing a functional composition style.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="value">The input value.</param>
    /// <param name="func">The transformation function.</param>
    /// <returns>The result of applying <paramref name="func"/> to <paramref name="value"/>.</returns>
    public static TOut Pipe<TIn, TOut>(this TIn value, Func<TIn, TOut> func) =>
        func(value);

    /// <summary>
    /// Composes two functions into a single function that applies <paramref name="f"/> first, then <paramref name="g"/>.
    /// </summary>
    /// <typeparam name="T1">The input type of the first function.</typeparam>
    /// <typeparam name="T2">The output type of the first function and input type of the second.</typeparam>
    /// <typeparam name="T3">The output type of the composed function.</typeparam>
    /// <param name="f">The first function to apply.</param>
    /// <param name="g">The second function to apply.</param>
    /// <returns>A composed function representing <c>g(f(x))</c>.</returns>
    public static Func<T1, T3> Compose<T1, T2, T3>(
        this Func<T1, T2> f,
        Func<T2, T3> g) =>
        x => g(f(x));
}

public static class ResultUtils
{
    /// <summary>
    /// Обёртка для try-catch в функциональном стиле
    /// </summary>
    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return Result<T>.Ok(func());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Обёртка для try-catch с кастомным сообщением об ошибке
    /// </summary>
    public static Result<T> Try<T>(Func<T> func, string errorPrefix)
    {
        try
        {
            return Result<T>.Ok(func());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail($"{errorPrefix}: {ex.Message}");
        }
    }

    /// <summary>
    /// Обёртка для try-catch с маппингом исключений
    /// </summary>
    public static Result<T> Try<T>(
        Func<T> func,
        Func<Exception, string> errorMapper)
    {
        try
        {
            return Result<T>.Ok(func());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(errorMapper(ex));
        }
    }

    /// <summary>
    /// Асинхронная обёртка для try-catch
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
    {
        try
        {
            var result = await func();
            return Result<T>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Асинхронная обёртка для try-catch с кастомным сообщением
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(
        Func<Task<T>> func,
        string errorPrefix)
    {
        try
        {
            var result = await func();
            return Result<T>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail($"{errorPrefix}: {ex.Message}");
        }
    }
}