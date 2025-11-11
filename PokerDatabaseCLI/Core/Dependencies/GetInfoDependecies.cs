using PokerDatabaseCLI.Domain.Poker;
using PokerDatabaseCLI.Infrastructure.Persistence;

namespace PokerDatabaseCLI.Core.Dependencies;

public record class GetInfoDependecies(
     Func<Result<CommonInfo>> GetInfo
)
{
    public static readonly GetInfoDependecies Default = new(
        GetInfo: HandRepository.GetInfo
    );
}
