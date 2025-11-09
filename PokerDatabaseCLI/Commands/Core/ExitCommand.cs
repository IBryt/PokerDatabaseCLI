using PokerDatabaseCLI.Core;

namespace PokerDatabaseCLI.Commands.Core;

/// <summary>
/// Implementation of the "exit" command.
/// Terminates the console.
/// </summary>
public static class ExitCommand
{
    /// <summary>
    /// Defines the "exit" command for the CLI.
    /// </summary>
    public static readonly CommandDefinition Definition = new(
        Name: "exit",
        Description: "Exit the application",
        Parameters: Array.Empty<ParameterDefinition>(),
        Execute: ExecuteExit
    );

    private static Result<string> ExecuteExit(CommandContext ctx)
    {
        Environment.Exit(0);
        return Result<string>.Ok("Exit...");
    }
}