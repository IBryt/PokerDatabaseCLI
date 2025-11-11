using PokerDatabaseCLI.Core;
using System.Reflection;

namespace PokerDatabaseCLI.Commands.Core;

/// <summary>
/// Central registry for managing application commands.
/// Acts as a single source of truth for all available commands in the application.
/// </summary>
public static class CommandRegistry
{
    private static readonly Dictionary<string, CommandDefinition> Commands = new();

    /// <summary>
    /// Registers a command definition in the registry.
    /// Command names are normalized to lowercase for case-insensitive lookup.
    /// </summary>
    /// <param name="definition">Command definition to register</param>
    public static void Register(CommandDefinition definition)
    {
        Commands[definition.Name.ToLower()] = definition;
    }

    /// <summary>
    /// Retrieves a command definition by name.
    /// Performs case-insensitive lookup and returns a Result monad wrapping either the command or an error.
    /// </summary>
    /// <param name="name">Command name to lookup</param>
    /// <returns>Result containing either the CommandDefinition or an error message</returns>
    public static Result<CommandDefinition> GetCommand(string name)
    {
        var key = name.ToLower();
        return Commands.TryGetValue(key, out var command)
            ? Result<CommandDefinition>.Ok(command)
            : Result<CommandDefinition>.Fail($"Command '{name}' not found");
    }

    /// <summary>
    /// Returns all registered commands.
    /// Used primarily by the help system to display available commands.
    /// </summary>
    /// <returns>Enumerable collection of all registered command definitions</returns>
    public static IEnumerable<CommandDefinition> GetAllCommands() =>
        Commands.Values;

    /// <summary>
    /// Initializes the registry with all application commands.
    /// This method should be called once at application startup before processing any user input.
    /// New commands should be registered here to make them available in the application.
    /// </summary>
    public static void Initialize()
    {
        Register(HelpCommand.Definition);
        RegisterCustomCommands();
        Register(ExitCommand.Definition);
    }

    private static void RegisterCustomCommands()
    {
        var customCommands = DiscoverCustomCommands();

        foreach (var definition in customCommands)
        {
            Register(definition);
        }
    }

    private static IEnumerable<CommandDefinition> DiscoverCustomCommands()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(IsCustomCommandType)
            .SelectMany(GetCommandDefinitions)
            .Where(def => def != null)
            .Cast<CommandDefinition>();
    }

    private static bool IsCustomCommandType(Type type) =>
        type != typeof(ExitCommand) &&
        type != typeof(HelpCommand) &&
        type.IsClass &&
        type.IsPublic &&
        type.Namespace != null &&
        type.Namespace.StartsWith("PokerDatabaseCLI.Commands");

    private static IEnumerable<CommandDefinition?> GetCommandDefinitions(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(CommandDefinition))
            .Select(f => f.GetValue(null) as CommandDefinition);
}
