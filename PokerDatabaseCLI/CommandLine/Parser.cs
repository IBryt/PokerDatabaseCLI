using PokerDatabaseCLI.Core;

namespace PokerDatabaseCLI.CommandLine;

/// <summary>
/// Provides parsing functionality for user input strings into structured command objects.
/// </summary>
public static class Parser
{
    /// <summary>
    /// Parses a raw input string into a <see cref="ParsedCommand"/>, including command name and parameters.
    /// </summary>
    /// <param name="input">The raw input string entered by the user.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="ParsedCommand"/> if successful,
    /// or a failure result with an error message if parsing fails.
    /// </returns>
    public static Result<ParsedCommand> ParseInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<ParsedCommand>.Fail("Empty command");

        var parts = SplitInput(input);
        if (parts.Length == 0)
            return Result<ParsedCommand>.Fail("No command specified");

        var commandName = parts[0];
        var parameters = ExtractParameters(parts.Skip(1).ToList());

        return Result<ParsedCommand>.Ok(new ParsedCommand(commandName, parameters));
    }

    private static string[] SplitInput(string input)
    {
        var parts = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (!string.IsNullOrEmpty(current))
                {
                    parts.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }

        if (!string.IsNullOrEmpty(current))
            parts.Add(current);

        return parts.ToArray();
    }

    private static IReadOnlyDictionary<string, string> ExtractParameters(List<string> args)
    {
        var parameters = new Dictionary<string, string>();
        var i = 0;

        while (i < args.Count)
        {
            if (IsParameter(args[i]))
            {
                var param = args[i];
                var key = param.TrimStart('-');

                if (i + 1 < args.Count && !IsParameter(args[i + 1]))
                {
                    parameters[key] = args[i + 1];
                    i += 2;
                }
                else
                {
                    parameters[key] = "true";
                    i++;
                }
            }
            else
            {
                i++;
            }
        }

        return parameters;
    }

    private static bool IsParameter(string arg) =>
        arg.StartsWith("-") || arg.StartsWith("--");
}