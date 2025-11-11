using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;

namespace PokerDatabaseCLI.Application;

/// <summary>
/// Represents the parameters required to execute the DeleteHand service.
/// </summary>
public sealed record DeleteHandServiceParams(
    DeleteDependencies DeleteDependencies,
    long Number
);

/// <summary>
/// Provides functionality to delete a hand based on its unique number.
/// </summary>
public static class DeleteHandService
{
    public static Result<bool> StartDelete(DeleteHandServiceParams deleteHandServiceParams)
    {
        var fn = deleteHandServiceParams.DeleteDependencies.DeleteHandByNumber;
        var number = deleteHandServiceParams.Number;
        return fn(number);
    }
}
