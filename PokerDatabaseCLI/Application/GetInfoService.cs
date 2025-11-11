using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;

namespace PokerDatabaseCLI.Application;

/// <summary>
/// Represents the parameters required to execute the GetInfo service.
/// </summary>
public sealed record GetInfoServiceParams(
    GetInfoDependecies GetInfoDependecies
);

/// <summary>
/// Provides functionality to retrieve common information.
/// </summary>
public static class GetInfoService
{
    public static Result<CommonInfo> StartGetInfo(GetInfoServiceParams getInfoServiceParams)
    {
        var fn = getInfoServiceParams.GetInfoDependecies.GetInfo;
        return fn();
    }
}
