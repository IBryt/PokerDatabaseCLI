using PokerDatabaseCLI.Domain.Poker.Models;

namespace PokerDatabaseCLI.Application.Import.ImportHands;

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
/// Represents the statistics of a pipeline execution.
/// </summary>
public record PipelineStats
(
    int TotalFiles,
    int ProcessedFiles,
    int SuccessCount,
    int ErrorCount,
    int TotalHands,
    int TotalDuplicates
);

/// <summary>
/// Represents the result of processing a single file.
/// </summary>
public record ProcessResult(
    string FilePath,
    int HandsCount,
    bool IsSuccess
);

/// <summary>
/// Represents statistics and hands after saving hands to the database.
/// </summary>
public record SaveStats(
    IReadOnlyDictionary<long, Hand> Hands,
    int DuplicatesCount
);

/// <summary>
/// Configuration for the import pipeline.
/// </summary>
public record PipelineConfig(
    int BatchSize = 10_000,
    int ChannelCapacity = 10,
    int? MaxConcurrency = null
)
{
    public int GetMaxConcurrency() =>
        MaxConcurrency ?? Math.Max(1, Environment.ProcessorCount - 3);
}