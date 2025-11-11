using PokerDatabaseCLI.Infrastructure.Persistence;

namespace PokerDatabaseCLI.Core.Dependencies;

/// <summary>
/// Represents the dependencies required for deleting a hand from the repository.
/// </summary>
public record DeleteDependencies(
    Func<long, Result<bool>> DeleteHandByNumber
)
{
    public static readonly DeleteDependencies Default = new(
        DeleteHandByNumber: HandRepository.DeleteHandByNumber
    );
}
