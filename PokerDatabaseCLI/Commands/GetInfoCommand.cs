using PokerDatabaseCLI.Application;
using PokerDatabaseCLI.Commands.Core;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;

namespace PokerDatabaseCLI.Commands;

public static class GetInfoCommand
{
    /// <summary>
    /// Definition of the information of db. Command for the CLI.
    /// </summary>
    public static readonly CommandDefinition Definition = new(
    Name: "GetInfo",
    Description: "Get info about players and count hands ",
    Parameters: Array.Empty<ParameterDefinition>(),
    Execute: ExecuteDeleteCommand
    );

    private static Result<string> ExecuteDeleteCommand(CommandContext ctx)
    {
        var getInfoDependecies = GetInfoDependecies.Default;

        var deleteHandServiceParams = new GetInfoServiceParams(
            GetInfoDependecies: getInfoDependecies
        );

        var result = GetInfoService.StartGetInfo(deleteHandServiceParams);

        var message = result.Match(
                onSuccess: info => "succses",
                onFailure: _ => "Error"
            );

        return result
            .Map(info => $"Total hands - {info.CountHands}, total players - {info.CountPlayers}")
            .MapError(error => "unexcpected error");
    }
}
