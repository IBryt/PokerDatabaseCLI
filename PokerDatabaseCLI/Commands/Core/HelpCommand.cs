using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Output;

namespace PokerDatabaseCLI.Commands.Core;

/// <summary>
/// Implementation of the "help" command.
/// Provides general help or detailed help for a specific command.
/// </summary>
public static class HelpCommand
{
    /// <summary>
    /// Defines the "help" command for the CLI.
    /// </summary>
    public static readonly CommandDefinition Definition = new(
        Name: "Help",
        Description: "Show help for commands",
        Parameters: new[]
        {
            new ParameterDefinition("command", "c", "The command to display detailed help for", false)
        },
        Execute: ExecuteHelp
    );

    private static Result<string> ExecuteHelp(CommandContext ctx)
    {
        var hasCommand = ctx.Parameters.TryGetValue("command", out var commandName);

        return hasCommand
            ? ShowCommandHelp(commandName!)
            : Result<string>.Ok(HelpGenerator.GenerateGeneralHelp());
    }

    private static Result<string> ShowCommandHelp(string commandName) =>
        CommandRegistry.GetCommand(commandName)
            .Map(HelpGenerator.GenerateCommandHelp);
}