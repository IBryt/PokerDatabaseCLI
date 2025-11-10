using PokerDatabaseCLI.CommandLine;
using PokerDatabaseCLI.Commands.Core;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Output;

namespace PokerDatabaseCLI.REPL;

/// <summary>
/// Read-Eval-Print Loop implementation for interactive command execution.
/// Handles the main application loop, user input, and command processing.
/// </summary>
public static class ReplLoop
{
    private const string Prompt = "> ";

    /// <summary>
    /// Main REPL loop entry point.
    /// Displays welcome message and enters infinite loop waiting for user commands.
    /// </summary>
    public static void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        ShowWelcome();

        while (true)
        {
            PrintPrompt();
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            ProcessInput(input);
        }
    }

    /// <summary>
    /// Prints the REPL command prompt symbol ("> ") to the console,
    /// indicating that the system is ready to accept a new command.
    /// </summary>
    public static void PrintPrompt()
    {
        Console.Write(Prompt);
    }

    private static void ShowWelcome()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Poker Database CLI - Interactive Command Line Interface    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Type 'Help' to see the list of commands or 'Exit' to quit");
        Console.WriteLine();
    }

    private static void ProcessInput(string input)
    {
        Parser.ParseInput(input)
            .Bind(CommandExecutor.Execute)
            .Match(
                onSuccess: output => Console.WriteLine(Formatter.FormatSuccess(output)),
                onFailure: error => Console.WriteLine(Formatter.FormatError(error))
            );
    }
}