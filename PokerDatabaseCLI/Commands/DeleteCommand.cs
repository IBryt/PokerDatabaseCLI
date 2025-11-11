using PokerDatabaseCLI.Application;
using PokerDatabaseCLI.Commands.Core;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;

namespace PokerDatabaseCLI.Commands;

public static class DeleteCommand
{
    /// <summary>
    /// Definition of the delete hand by number. Command for the CLI.
    /// </summary>
    public static readonly CommandDefinition Definition = new(
        Name: "DeleteHand",
        Description: "Delete hand by number",
        Parameters: new[]
        {
            new ParameterDefinition("HandNumber", "h", "number of hand", true),
        },
        Execute: ExecuteDeleteCommand
    );

    private static Result<string> ExecuteDeleteCommand(CommandContext ctx)
    {
        var numberStr = ctx.Parameters["HandNumber"];

        if (!long.TryParse(numberStr, out long number))
        {
            return Result<string>.Fail("Can't parse hand number.");
        }

        var deleteDependencies = DeleteDependencies.Default;

        var deleteHandServiceParams = new DeleteHandServiceParams(
            DeleteDependencies: deleteDependencies,
            Number: number
        );

        var result = DeleteHandService.StartDelete(deleteHandServiceParams);

        return result
            .Map(res => res ? $"Hand #{number} has been deleted" : $"Hand #{number} not found")
            .MapError(_ => "unexcpected error");
    }
}
