using PokerDatabaseCLI.Domain.Poker;
using PokerDatabaseCLI.Infrastructure.Persistence;

namespace PokerDatabaseCLI.Core.Dependencies;

/// <summary>
/// Represents the dependencies required for retrieving common information from the repository.
/// </summary>
public record GetInfoDependecies(
     Func<Result<CommonInfo>> GetInfo
)
{
    public static readonly GetInfoDependecies Default = new(
        GetInfo: HandRepository.GetInfo
    );
}
