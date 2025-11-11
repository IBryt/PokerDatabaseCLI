using PokerDatabaseCLI.Infrastructure.Persistence;

namespace PokerDatabaseCLI.Core.Dependencies;

public record DeleteDependencies(
    Func<long, Result<bool>> DeleteHandByNumber
)
{
    public static readonly DeleteDependencies Default = new(
        DeleteHandByNumber: HandRepository.DeleteHandByNumber
    );
}
