using Open.ChannelExtensions;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Domain.Poker.Import;
using PokerDatabaseCLI.Domain.Poker.Models;
using PokerDatabaseCLI.Infrastructure.Persistence;
using System.Diagnostics;

namespace PokerDatabaseCLI.Commands;

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
     int TotalDublicates
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
    int DublicatesCount
);

/// <summary>
/// Command for importing poker hands from a folder.
/// </summary>
public static class ImportCommand
{
    private const int BATCH_SIZE = 10_000;

    /// <summary>
    /// Definition of the "import" command for the CLI.
    /// </summary>
    public static readonly CommandDefinition Definition = new(
        Name: "import",
        Description: "Import hands from folder",
        Parameters: new[]
        {
            new ParameterDefinition("path", "p", "Path to folder", true),
        },
        Execute: ExecuteImportCommand
    );

    private static Result<string> ExecuteImportCommand(CommandContext ctx)
    {
        var path = ctx.Parameters["path"];
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = Import.ValidatePath(path)
                .Bind(Import.ScanDirectory)
                .Map(directories =>
                {
                    var batchSize = directories.FilePaths.Count / 20;
                    var stats = ProcessPipeline(directories, batchSize);
                    return FormatResult(stats, stopwatch.Elapsed);
                });
            return result;
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Import error: {ex.Message}");
        }
    }

    private static PipelineStats ProcessPipeline(ScanResult scan, int batchSize)
    {
        var stats = new PipelineStats(
            TotalFiles: scan.FilePaths.Count,
            ProcessedFiles: 0,
            SuccessCount: 0,
            ErrorCount: 0,
            TotalHands: 0,
            TotalDublicates: 0
        );

        var statsLock = new object();
        var processedCount = 0;

        var handsAccumulator = new Dictionary<long, Hand>();
        var accumulatorLock = new object();

        var processTask = Import.ProcessFiles(scan)
            .ToChannel(capacity: 10, singleReader: true)
            .Pipe(
                maxConcurrency: Environment.ProcessorCount - 3,
                capacity: 10,
                transform: ProcessHands
            )
            .ReadAll(result =>
            {
                Result<SaveStats>? saveResult = null;

                lock (accumulatorLock)
                {
                    if (result is Result<IReadOnlyDictionary<long, Hand>>.Success success)
                    {
                        foreach (var hand in success.Value)
                        {
                            handsAccumulator[hand.Key] = hand.Value;
                        }

                        if (handsAccumulator.Count >= BATCH_SIZE)
                        {
                            saveResult = SaveBatchToDb(handsAccumulator);
                            handsAccumulator.Clear();
                        }
                    }
                }

                lock (statsLock)
                {
                    processedCount++;

                    if (saveResult != null)
                    {
                        stats = UpdateStats(stats, saveResult);
                    }
                    else if (result is Result<IReadOnlyDictionary<long, Hand>>.Success)
                    {
                        stats = stats with { SuccessCount = stats.SuccessCount + 1 };
                    }
                    else if (result is Result<IReadOnlyDictionary<long, Hand>>.Failure)
                    {
                        stats = stats with { ErrorCount = stats.ErrorCount + 1 };
                    }

                    if (processedCount % batchSize == 0 || processedCount == stats.TotalFiles)
                    {
                        DisplayProgress(stats with { ProcessedFiles = processedCount }, processedCount);
                    }
                }
            });

        processTask.AsTask().GetAwaiter().GetResult();

        Result<SaveStats>? finalSaveResult = null;
        lock (accumulatorLock)
        {
            if (handsAccumulator.Count > 0)
            {
                finalSaveResult = SaveBatchToDb(handsAccumulator);
                handsAccumulator.Clear();
            }
        }

        lock (statsLock)
        {
            if (finalSaveResult != null)
            {
                stats = UpdateStats(stats, finalSaveResult);
            }
            stats = stats with { ProcessedFiles = processedCount };
        }

        return stats;
    }

    private static Result<SaveStats> SaveBatchToDb(Dictionary<long, Hand> hands)
    {
        try
        {
            var dublicatesId = HandRepository.GetDublicates(hands);
            var uniqHands = hands.Where(h => !dublicatesId.Contains(h.Key)).ToDictionary();
            HandRepository.AddHands(uniqHands);
            return Result<SaveStats>.Ok(
                new SaveStats(Hands: hands, DublicatesCount: dublicatesId.Count)
            );
        }
        catch (Exception ex)
        {
            return Result<SaveStats>.Fail($"Database save error: {ex.Message}");
        }
    }

    private static PipelineStats UpdateStats(PipelineStats stats, Result<SaveStats> result)
    {
        return result switch
        {
            Result<SaveStats>.Success success => stats with
            {
                TotalHands = stats.TotalHands + success.Value.Hands.Count,
                TotalDublicates = stats.TotalDublicates + success.Value.DublicatesCount
            },
            Result<SaveStats>.Failure => stats with
            {
                ErrorCount = stats.ErrorCount + 1
            },
            _ => stats
        };
    }

    /// <summary>
    /// Additional processing of poker hands and other complex calculations.
    /// </summary>
    /// <param name="parseResult">The result of parsing a file.</param>
    /// <returns>A result containing a read-only list of hands.</returns>
    public static Result<IReadOnlyDictionary<long, Hand>> ProcessHands(Result<ParseFileResult> parseResult)
    {
        return parseResult.Map(v => v.Hands);
    }

    private static void DisplayProgress(PipelineStats stats, int processedCount)
    {
        var percentage = stats.TotalFiles > 0
            ? (int)(processedCount * 100.0 / stats.TotalFiles)
            : 0;

        var barLength = 40;
        var filledLength = (int)(barLength * percentage / 100);
        var bar = new string('█', filledLength) + new string('░', barLength - filledLength);

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write($"[{bar}] {percentage,3}% | " +
                     $"Files - {processedCount}/{stats.TotalFiles} " +
                     $"Hands - {stats.TotalHands} " +
                     $"Error - {stats.ErrorCount}");
    }

    private static string FormatResult(PipelineStats stats, TimeSpan duration)
    {
        var filesPerSecond = duration.TotalSeconds > 0
            ? stats.ProcessedFiles / duration.TotalSeconds
            : 0;

        var handsPerSecond = duration.TotalSeconds > 0
            ? stats.TotalHands / duration.TotalSeconds
            : 0;

        var successRate = stats.TotalFiles > 0
            ? (stats.SuccessCount * 100.0 / stats.TotalFiles)
            : 0;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                        Import Completed                        ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"  Statistics:");
        sb.AppendLine($"     • Total files:         {stats.TotalFiles}");
        sb.AppendLine($"     • Processed:           {stats.ProcessedFiles}");
        sb.AppendLine($"     • Successful:          {stats.SuccessCount}");
        sb.AppendLine($"     • Errors:              {stats.ErrorCount}");
        sb.AppendLine($"     • Dublicates hands:    {stats.TotalDublicates}");
        sb.AppendLine($"     • Total hands:         {stats.TotalHands}");
        sb.AppendLine($"     • Hands per second:    {stats.TotalHands / duration.TotalSeconds:F1}");
        sb.AppendLine();
        sb.AppendLine($"  Performance:");
        sb.AppendLine($"     • Duration:            {duration.TotalSeconds:F2} sec");
        sb.AppendLine($"     • Speed:               {filesPerSecond:F1} files/sec");
        sb.AppendLine($"     • Hands per second:    {handsPerSecond:F1}");
        sb.AppendLine();
        sb.AppendLine($"  Success rate: {successRate:F1}%");

        return sb.ToString();
    }
}