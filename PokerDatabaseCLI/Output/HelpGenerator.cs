using PokerDatabaseCLI.Commands.Core;
using System.Text;

namespace PokerDatabaseCLI.Output;

/// <summary>
/// Generates formatted help documentation for CLI commands.
/// </summary>
public static class HelpGenerator
{
    /// <summary>
    /// Generates general help overview showing all available commands.
    /// </summary>
    /// <returns>Formatted help text with command list and usage instructions</returns>
    public static string GenerateGeneralHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              Functional CLI - Command Reference                ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine("Available commands:");
        sb.AppendLine();

        foreach (var cmd in CommandRegistry.GetAllCommands())
        {
            sb.AppendLine($"  {cmd.Name,-15} - {cmd.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("Usage: <command> [parameters]");
        sb.AppendLine("Command help: help --command <command_name>");
        sb.AppendLine();
        sb.Append("To exit, type: exit");

        return sb.ToString();
    }

    /// <summary>
    /// Generates detailed help for a specific command including all parameters.
    /// </summary>
    /// <param name="cmd">Command definition to generate help for</param>
    /// <returns>Formatted help text with command details and parameter descriptions</returns>
    public static string GenerateCommandHelp(CommandDefinition cmd)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║ Command: {cmd.Name,-53} ║");
        sb.AppendLine($"╚════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"Description: {cmd.Description}");
        sb.AppendLine();

        if (cmd.Parameters.Any())
        {
            sb.AppendLine("Parameters:");
            foreach (var param in cmd.Parameters)
            {
                var required = param.IsRequired ? "[required]" : "[optional]";
                var defaultVal = param.DefaultValue != null ? $" (default: {param.DefaultValue})" : "";

                sb.AppendLine($"  --{param.LongName,-15} (-{param.ShortName,-2}) {required}{defaultVal}");
                sb.AppendLine($"  {param.Description}");
            }
        }
        else
        {
            sb.AppendLine("No parameters");
        }

        return sb.ToString();
    }
}