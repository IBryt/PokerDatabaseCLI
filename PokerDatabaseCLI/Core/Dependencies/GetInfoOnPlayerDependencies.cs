using PokerDatabaseCLI.Domain.Poker;
using PokerDatabaseCLI.Infrastructure.Persistence;

namespace PokerDatabaseCLI.Core.Dependencies;

/// <summary>
/// Represents the dependencies required for retrieving information about a specific player.
/// </summary>
public record GetInfoOnPlayerDependencies(
     Func<string, int, Result<InfoOnPlayer>> GetInfoOnPlayer
)
{
    public static readonly GetInfoOnPlayerDependencies Default = new(
        GetInfoOnPlayer: HandRepository.GetInfoOnPlayer
    );
}
