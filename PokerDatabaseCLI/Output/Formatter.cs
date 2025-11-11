namespace PokerDatabaseCLI.Output;

/// <summary>
/// Provides methods for formatting command results.
/// </summary>
public static class Formatter
{
    /// <summary>
    /// Formats a successful result.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A formatted string representing success.</returns>
    public static string FormatSuccess(string value) => value;

    /// <summary>
    /// Formats an error result.
    /// </summary>
    /// <param name="error">The error message to format.</param>
    /// <returns>A formatted string representing an error.</returns>
    public static string FormatError(string error) => $"Error: {error}";
}