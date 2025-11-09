using PokerDatabaseCLI.CommandLine;
using PokerDatabaseCLI.Core;

namespace PokerDatabaseCLI.Commands.Core;

/// <summary>
/// Responsible for executing parsed commands.
/// </summary>
public static class CommandExecutor
{
    /// <summary>
    /// Executes a single parsed command.
    /// 1. Retrieves the command definition from the registry.
    /// 2. Validates the parsed command against the definition.
    /// 3. Executes the command if validation succeeds.
    /// </summary>
    /// <param name="parsed">The parsed command containing the name and parameters.</param>
    /// <returns>A Result containing either the execution output or an error.</returns>
    public static Result<string> Execute(ParsedCommand parsed) =>
        CommandRegistry.GetCommand(parsed.CommandName)
            .Bind(def => Validator.Validate(parsed, def))
            .Bind(ctx => CommandRegistry.GetCommand(parsed.CommandName)
                .Bind(def => def.Execute(ctx)));
}