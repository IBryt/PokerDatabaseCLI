using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;

namespace PokerDatabaseCLI.Application;

/// <summary>
/// Represents the parameters required to start an import of poker hands.
/// </summary>
/// <param name="Path">The path of the folder containing hand files to import.</param>
/// <param name="Progress">An <see cref="IProgress{ImportProgress}"/> instance to report import progress.</param>
/// <param name="Token">A <see cref="CancellationToken"/> used to cancel the import process.</param>
/// <param name="ImportDependencies">Dependencies required for the import.</param>
public sealed record ImportServiceParams(
    string Path,
    IProgress<ImportProgress> Progress,
    CancellationToken Token,
    ImportDependencies ImportDependencies
);

/// <summary>
/// Represents progress information during the import process.
/// </summary>
/// <param name="SuccessCount">The number of successfully processed files.</param>
/// <param name="Percentage">The overall completion percentage of the import process.</param>
/// <param name="ProcessedFiles">The total number of files that have been processed so far.</param>
/// <param name="TotalFiles">The total number of files to process.</param>
/// <param name="TotalHands">The total number of poker hands successfully imported.</param>
/// <param name="ErrorCount">The number of files that failed to process due to errors.</param>
/// <param name="IsCompleted">Indicates whether the import process has completed.</param>
/// <param name="TotalDuplicates">The total number of duplicate hands detected during import.</param>
public record ImportProgress
(
    int SuccessCount,
    double Percentage,
    int ProcessedFiles,
    int TotalFiles,
    int TotalHands,
    int ErrorCount,
    bool IsCompleted,
    int TotalDuplicates
);

/// <summary>
/// Represents the results of saving poker hands to the database or storage.
/// </summary>
/// <param name="Hands">
/// A dictionary containing all imported poker hands, where the key is a unique hand Number.
/// </param>
/// <param name="DuplicatesCount">
/// The number of duplicate hands that were detected and skipped during the save process.
/// </param>
public record SaveStats(
    IReadOnlyDictionary<long, Hand> Hands,
    int DuplicatesCount
);