using PokerDatabaseCLI.Application;
using PokerDatabaseCLI.Domain.Poker;
using PokerDatabaseCLI.Infrastructure.Persistence;

namespace PokerDatabaseCLI.Core.Dependencies;

/// <summary>
/// Application-level dependencies for the import process.
/// </summary>
public record ImportDependencies(
    Func<IReadOnlyDictionary<long, Hand>, Result<SaveStats>> SaveBatch,
    int BatchSize
)
{
    public static readonly ImportDependencies Default = new(
        SaveBatch: HandRepository.SaveBatch,
        BatchSize: 5_000
    );
}
