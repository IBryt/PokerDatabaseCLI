using Open.ChannelExtensions;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;
using System.Globalization;
using System.Text;

namespace PokerDatabaseCLI.Application;

/// <summary>
/// Provides functionality to start and manage the import of poker hands from a folder.
/// This service validates the path, scans the directory for hand files, 
/// and executes the import pipeline.
/// </summary>
public static class ImportHandsService
{
    /// <summary>
    /// Starts the import process from the specified folder.
    /// </summary>
    public static Result<string> StartImport(ImportServiceParams importServiceParams)
    {
        try
        {
            return Import.ValidatePath(importServiceParams.Path)
                .Bind(Import.ScanDirectory)
                .Map(directories =>
                {
                    ProcessImportPipeline(directories, importServiceParams);
                    return "Import started.";
                })
                .MapError(_ => "unexcpected error");
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Import error: {ex.Message}");
        }
    }

    private static void ProcessImportPipeline(
        ScanResult scan,
        ImportServiceParams importServiceParams)
    {
        var stateLock = new object();

        var statistics = new ImportStatistics
        {
            TotalFiles = scan.FilePaths.Count
        };
        var capacity = CalculateDictionaryCapacity(importServiceParams);
        var handsAccumulator = new List<IReadOnlyList<Hand>>(capacity: 100);
        var handsDictionary = new Dictionary<long, Hand>(capacity: capacity);

        Import.ProcessFiles(scan)
            .ToChannel(capacity: 10, singleReader: true, importServiceParams.Token)
            .Pipe(
                maxConcurrency: Math.Max(1, Environment.ProcessorCount - 3),
                capacity: 10,
                transform: ProcessHands,
                cancellationToken: importServiceParams.Token
            )
            .ReadAll(
                cancellationToken: importServiceParams.Token,
                result => ProcessResult(
                    result,
                    importServiceParams,
                    statistics,
                    handsAccumulator,
                    handsDictionary,
                    stateLock
                )
            );
    }

    private static void ProcessResult(
        Result<IReadOnlyList<Hand>> result,
        ImportServiceParams importServiceParams,
        ImportStatistics statistics,
        List<IReadOnlyList<Hand>> handsAccumulator,
        Dictionary<long, Hand> handsDictionary,
        object stateLock)
    {
        if (importServiceParams.Token.IsCancellationRequested)
            return;

        lock (stateLock)
        {
            statistics.ProcessedCount++;

            result.Match(
                onSuccess: hands =>
                {
                    statistics.TotalHands += hands.Count;
                    handsAccumulator.Add(hands);
                },
                onFailure: error => statistics.ErrorCount++
            );

            var isCompleted = statistics.ProcessedCount == statistics.TotalFiles;
            var totalAccumulatedHands = CalculateTotalHands(handsAccumulator);

            if (!ShouldSaveBatch(
                importServiceParams.Token,
                importServiceParams.ImportDependencies,
                totalAccumulatedHands,
                isCompleted))
            {
                return;
            }

            SaveBatchAndUpdateStatistics(
                handsAccumulator,
                handsDictionary,
                importServiceParams.ImportDependencies,
                statistics
            );

            ReportProgress(importServiceParams.Progress, statistics, isCompleted);
        }
    }

    private static int CalculateTotalHands(List<IReadOnlyList<Hand>> handsAccumulator)
    {
        var total = 0;
        for (var i = 0; i < handsAccumulator.Count; i++)
        {
            total += handsAccumulator[i].Count;
        }
        return total;
    }

    private static void SaveBatchAndUpdateStatistics(
        List<IReadOnlyList<Hand>> handsAccumulator,
        Dictionary<long, Hand> handsDictionary,
        ImportDependencies importDependencies,
        ImportStatistics statistics)
    {
        handsDictionary.Clear();

        foreach (var handsList in handsAccumulator)
        {
            foreach (var hand in handsList)
            {
                handsDictionary[hand.Number] = hand;
            }
        }

        var saveResult = importDependencies.SaveBatch(handsDictionary);

        saveResult.Match(
            onSuccess: stats => statistics.TotalDuplicates += stats.DuplicatesCount,
            onFailure: _ => statistics.ErrorCount++
        );

        handsAccumulator.Clear();
    }

    private static void ReportProgress(
        IProgress<ImportProgress> progress,
        ImportStatistics statistics,
        bool isCompleted)
    {
        progress.Report(new ImportProgress(
            TotalDuplicates: statistics.TotalDuplicates,
            SuccessCount: 0,
            Percentage: (double)statistics.ProcessedCount / statistics.TotalFiles * 100,
            ProcessedFiles: statistics.ProcessedCount,
            TotalFiles: statistics.TotalFiles,
            TotalHands: statistics.TotalHands,
            ErrorCount: statistics.ErrorCount,
            IsCompleted: isCompleted
        ));
    }

    private static bool ShouldSaveBatch(
        CancellationToken token,
        ImportDependencies importAppDependencies,
        int handsCount,
        bool isCompleted)
    {
        return handsCount >= importAppDependencies.BatchSize ||
               isCompleted ||
               token.IsCancellationRequested;
    }

    private static Result<IReadOnlyList<Hand>> ProcessHands(
        Result<ParseFileResult> parseResult) =>
        parseResult.Map(x => x.Hands);

    private static int CalculateDictionaryCapacity(ImportServiceParams importServiceParams)
    {
        return importServiceParams.ImportDependencies.BatchSize * 2;
    }

    private static class Import
    {
        /// <summary>
        /// Validates the folder path.
        /// </summary>
        public static Result<string> ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Result<string>.Fail("The path cannot be empty.");

            if (!Directory.Exists(path))
                return Result<string>.Fail($"Directory not found: {path}");

            return Result<string>.Ok(path);
        }

        /// <summary>
        /// Scans a folder for .txt files.
        /// </summary>
        public static Result<ScanResult> ScanDirectory(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories)
                                     .ToList();

                if (files.Count == 0)
                {
                    return Result<ScanResult>.Fail("No .txt files found in the folder.");
                }

                return Result<ScanResult>.Ok(new ScanResult(files));
            }
            catch (UnauthorizedAccessException)
            {
                return Result<ScanResult>.Fail("Access to the folder is denied.");
            }
            catch (Exception ex)
            {
                return Result<ScanResult>.Fail($"Error scanning folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes all files from a <see cref="ScanResult"/> by importing and parsing their contents.
        /// </summary>
        public static IEnumerable<Result<ParseFileResult>> ProcessFiles(ScanResult scan)
        {
            foreach (var path in scan.FilePaths)
            {
                yield return ImportFile(path);
            }
        }

        private static Result<ParseFileResult> ImportFile(string path)
        {
            try
            {
                var hands = ImportHandsFromFile(path);

                return Result<ParseFileResult>.Ok(new ParseFileResult(
                    FilePath: path,
                    Hands: hands
                ));
            }
            catch (Exception ex)
            {
                return Result<ParseFileResult>.Fail(
                    $"Parsing error {Path.GetFileName(path)}: {ex.Message}");
            }
        }

        private static List<Hand> ImportHandsFromFile(string path)
        {
            var hands = new List<Hand>();
            using var fileStream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024);

            using var reader = new StreamReader(fileStream, Encoding.UTF8);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0) continue;
                var lineSpan = line.AsSpan();
                if (lineSpan.StartsWith("PokerStars Hand #"))
                {
                    var hand = ParseSingleHand(lineSpan, reader);
                    if (hand != null)
                    {
                        hands.Add(hand);
                    }
                }
            }
            return hands;
        }

        private static Hand? ParseSingleHand(ReadOnlySpan<char> lineSpan, StreamReader reader)
        {
            var number = GetNumberOptimized(lineSpan);
            var dateTime = GetDateTime(lineSpan);
            var players = new List<Player>(10);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var span = line.AsSpan();

                if (span.StartsWith("Seat "))
                {
                    var player = ParseSeatLineOptimized(span);
                    players.Add(player);
                }
                else if (span.StartsWith("Dealt to "))
                {
                    ParseDealtToLine(span, players);
                    break;
                }
            }

            return new Hand(number, dateTime, players);
        }

        private static DateTime GetDateTime(ReadOnlySpan<char> line)
        {
            var openBracketIndex = line.IndexOf('[') + 1;
            int lastSpaceIndex = line.LastIndexOf(' ');
            var dateSpan = line.Slice(openBracketIndex, lastSpaceIndex - openBracketIndex);
            var tzSpan = line.Slice(lastSpaceIndex + 1, line.Length - lastSpaceIndex - 2);

            string format = "yyyy/MM/dd H:mm:ss";

            DateTime dt = DateTime.ParseExact(
                dateSpan,
                format,
                CultureInfo.InvariantCulture
            );

            var tz = GetTimeZoneFromAbbreviation(tzSpan);

            return TimeZoneInfo.ConvertTimeToUtc(dt, tz);
        }

        private static TimeZoneInfo GetTimeZoneFromAbbreviation(ReadOnlySpan<char> tzAbbreviation)
        {
            return tzAbbreviation switch
            {
                "ET" or "EST" => TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
                "UTC" => TimeZoneInfo.Utc,
                _ => throw new ArgumentException($"Unknown timezone abbreviation: {tzAbbreviation.ToString()}")
            };
        }

        private static long GetNumberOptimized(ReadOnlySpan<char> line)
        {
            var hashIndex = line.IndexOf('#') + 1;
            var colonIndex = line.Slice(hashIndex).IndexOf(':') + hashIndex;
            return long.Parse(line.Slice(hashIndex, colonIndex - hashIndex));
        }

        private static Player ParseSeatLineOptimized(ReadOnlySpan<char> line)
        {
            var colonIndex = line.IndexOf(':') + 2;
            var chipsStart = line.LastIndexOf('(');
            var nameSpan = line.Slice(colonIndex, chipsStart - colonIndex - 1);
            var playerName = nameSpan.ToString();

            var chipsEnd = line.LastIndexOf(" in chips)");
            var chipsSpan = line.Slice(chipsStart + 2, chipsEnd - chipsStart - 2);
            var chips = decimal.Parse(chipsSpan, NumberStyles.Any, CultureInfo.InvariantCulture);

            return new Player(playerName, chips);
        }

        private static void ParseDealtToLine(ReadOnlySpan<char> line, List<Player> players)
        {
            var toIndex = line.IndexOf(" to ");
            var bracketStart = line.IndexOf('[');
            var bracketEnd = line.IndexOf(']');

            var nameSpan = line.Slice(toIndex + 4, bracketStart - toIndex - 5);
            var cardsSpan = line.Slice(bracketStart + 1, bracketEnd - bracketStart - 1);

            var name = nameSpan.ToString();
            var cards = cardsSpan.ToString();

            for (var i = 0; i < players.Count; i++)
            {
                if (players[i].Name.Equals(name))
                {
                    players[i] = players[i] with { Cards = cards };
                    break;
                }
            }
        }
    }

    private sealed class ImportStatistics
    {
        public int TotalDuplicates { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalFiles { get; set; }
        public int TotalHands { get; set; }
        public int ErrorCount { get; set; }
    }
}