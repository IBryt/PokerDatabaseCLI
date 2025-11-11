using PokerDatabaseCLI.Core.Dependencies;

namespace PokerDatabaseCLI.Application;

public sealed record DeleteHandServiceParams(
    DeleteDependencies DeleteDependencies,
    long Number
);


public static class DeleteHandService
{
    public static bool StartDelete(DeleteHandServiceParams deleteHandServiceParams)
    {
        var fn = deleteHandServiceParams.DeleteDependencies.DeleteHandByNumber;
        var number = deleteHandServiceParams.Number;
        return fn(number);
    }
}
