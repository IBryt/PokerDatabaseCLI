using PokerDatabaseCLI.Commands.Core;
using PokerDatabaseCLI.Core;

namespace PokerDatabaseCLI.CommandLine;

/// <summary>
/// Provides validation logic for commands and their parameters before execution.
/// </summary>
public static class Validator
{
    /// <summary>
    /// Validates a parsed command and its parameters against a command definition.
    /// </summary>
    /// <param name="parsed">The parsed command input to validate.</param>
    /// <param name="definition">The command definition containing expected parameters and metadata.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="CommandContext"/> if validation succeeds,
    /// or a failure result with an error message if validation fails.
    /// </returns>
    public static Result<CommandContext> Validate(
        ParsedCommand parsed,
        CommandDefinition definition)
    {
        var normalizedParams = NormalizeParameters(parsed.Parameters, definition);

        return ValidateRequiredParameters(normalizedParams, definition)
            .Map(_ => new CommandContext(parsed.CommandName, normalizedParams));
    }

    private static IReadOnlyDictionary<string, string> NormalizeParameters(
        IReadOnlyDictionary<string, string> parameters,
        CommandDefinition definition)
    {
        var normalized = new Dictionary<string, string>();

        foreach (var param in definition.Parameters)
        {
            var value = FindParameterValue(parameters, param);
            if (value != null)
                normalized[param.LongName] = value;
        }

        return normalized;
    }

    private static string? FindParameterValue(
        IReadOnlyDictionary<string, string> parameters,
        ParameterDefinition definition)
    {
        if (parameters.TryGetValue(definition.LongName, out var longValue))
            return longValue;

        if (parameters.TryGetValue(definition.ShortName, out var shortValue))
            return shortValue;

        return definition.DefaultValue;
    }

    private static Result<Unit> ValidateRequiredParameters(
        IReadOnlyDictionary<string, string> parameters,
        CommandDefinition definition)
    {
        var missing = definition.Parameters
            .Where(p => p.IsRequired && !parameters.ContainsKey(p.LongName))
            .Select(p => p.LongName)
            .ToList();

        return missing.Any()
            ? Result<Unit>.Fail($"Required parameters are missing: {string.Join(", ", missing)}")
            : Result<Unit>.Ok(Unit.Value);
    }
}
