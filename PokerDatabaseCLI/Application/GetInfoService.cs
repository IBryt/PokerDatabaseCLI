using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;

namespace PokerDatabaseCLI.Application;

public sealed record GetInfoServiceParams(
    GetInfoDependecies GetInfoDependecies
);

public static class GetInfoService
{
    public static Result<CommonInfo> StartGetInfo(GetInfoServiceParams getInfoServiceParams)
    {
        var fn = getInfoServiceParams.GetInfoDependecies.GetInfo;
        return fn();
    }
}
