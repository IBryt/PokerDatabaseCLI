using PokerDatabaseCLI.Infrastructure.Persistence;

namespace PokerDatabaseCLI.Core.Dependencies;

public record DeleteDependencies(
    Func<long, bool> DeleteHandByNumber
)
{
    public static readonly DeleteDependencies Default = new(
        DeleteHandByNumber: HandRepository.DeleteHandByNumber
    );
}
