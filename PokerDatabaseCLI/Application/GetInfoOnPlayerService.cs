using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;

namespace PokerDatabaseCLI.Application;

/// <summary>
/// Represents the parameters required to execute the GetInfoOnPlayer service.
/// </summary>
public sealed record GetInfoOnPlayerServiceParams(
    GetInfoOnPlayerDependencies GetInfoOnPlayerDependencies,
    string Name,
    int Count
);

/// <summary>
/// Provides functionality to retrieve detailed information about a specific player.
/// </summary>
public static class GetInfoOnPlayerService
{
    public static Result<InfoOnPlayer> StartGetInfo(GetInfoOnPlayerServiceParams getInfoOnPlayerServiceParams)
    {
        var fn = getInfoOnPlayerServiceParams.GetInfoOnPlayerDependencies.GetInfoOnPlayer;
        var name = getInfoOnPlayerServiceParams.Name;
        var count = getInfoOnPlayerServiceParams.Count;
        return fn(name, count);
    }
}

