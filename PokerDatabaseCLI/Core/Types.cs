namespace PokerDatabaseCLI.Core;

/// <summary>
/// Represents the execution context of a command, including its name and parsed parameters.
/// </summary>
public record CommandContext(
    string CommandName,
    IReadOnlyDictionary<string, string> Parameters
);

/// <summary>
/// Defines a command parameter, including its names, description, and requirement state.
/// </summary>
public record ParameterDefinition(
    string LongName,
    string ShortName,
    string Description,
    bool IsRequired,
    string? DefaultValue = null
);

/// <summary>
/// Defines a command with its metadata, parameters, and execution delegate.
/// </summary>
public record CommandDefinition(
    string Name,
    string Description,
    IReadOnlyList<ParameterDefinition> Parameters,
    Func<CommandContext, Result<string>> Execute
);

/// <summary>
/// Represents a command registered in the system, associated with its definition.
/// </summary>
public record Command(
    string Name,
    CommandDefinition Definition
);

/// <summary>
/// Represents a parsed command input containing the command name and parameter values.
/// </summary>
public record ParsedCommand(
    string CommandName,
    IReadOnlyDictionary<string, string> Parameters
);

/// <summary>
/// Represents a unit type that carries no data but indicates a successful result in functional operations.
/// </summary>
public record Unit
{
    public static readonly Unit Value = new();
    private Unit() { }
}