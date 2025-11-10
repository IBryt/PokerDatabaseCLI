using PokerDatabaseCLI.Application.Import;
using PokerDatabaseCLI.Application.Import.ImportHands;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;
using PokerDatabaseCLI.Domain.Poker.Models;
using PokerDatabaseCLI.REPL;
using System.Diagnostics;
using System.Text;

namespace PokerDatabaseCLI.Commands;

/// <summary>
/// Command for importing poker hands from a folder.
/// </summary>
public static class ImportCommand
{
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

        var cts = new CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();

        var progressReporter = CreateProgressReporter(cts, stopwatch);

        var importDependencies = ImportDependencies.Default;
        var importServiceParams = new ImportServiceParams(
            Path: path,
            Progress: progressReporter,
            CancellationToken: cts.Token,
            ImportDependencies: importDependencies
        );

        return ImportHandsService.StartImport(importServiceParams);
    }

    private static IProgress<ImportProgress> CreateProgressReporter(CancellationTokenSource cts, Stopwatch stopwatch)
    {
        return new Progress<ImportProgress>(progress =>
            DisplayImportProgress.Display(progress, cts, stopwatch)
        );
    }

    public static Result<IReadOnlyDictionary<long, Hand>> ProcessHands(Result<ParseFileResult> parseResult)
    {
        return parseResult.Map(x => x.Hands);
    }


    private static class DisplayImportProgress
    {
        private const int BAR_LENGTH = 40;

        /// <summary>
        /// Displays current pipeline statistics as a progress bar.
        /// </summary>
        public static void Display(ImportProgress progress, CancellationTokenSource cts, Stopwatch stopwatch)
        {

            if (cts.Token.IsCancellationRequested)
            {
                return;
            }

            var percentage = CalculatePercentage(progress.TotalFiles, progress.ProcessedFiles);
            var bar = CreateProgressBar(percentage);

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, Console.CursorTop);

            Console.Write($"[{bar}] {percentage,3}% | " +
                         $"Files - {progress.ProcessedFiles}/{progress.TotalFiles} " +
                         $"Hands - {progress.TotalHands} " +
                         $"Error - {progress.ErrorCount}");

            if (progress.IsCompleted)
            {
                var message = DisplayImportResult.GetDisplayMessage(progress, stopwatch.Elapsed);
                Console.WriteLine(message);
                ReplLoop.PrintPrompt();
            }
        }

        private static int CalculatePercentage(int total, int processed) =>
            total > 0 ? (int)(processed * 100.0 / total) : 0;

        private static string CreateProgressBar(int percentage)
        {
            var filledLength = BAR_LENGTH * percentage / 100;
            return new string('█', filledLength) + new string('░', BAR_LENGTH - filledLength);
        }
    }

    private static class DisplayImportResult
    {
        /// <summary>
        /// Formats pipeline statistics into a readable report.
        /// </summary>
        public static string GetDisplayMessage(ImportProgress progress, TimeSpan duration)
        {
            var metrics = CalculateMetrics(progress, duration);

            var sb = new StringBuilder();
            AppendHeader(sb);
            AppendStatistics(sb, progress, metrics);
            AppendPerformance(sb, duration, metrics);
            AppendSuccessRate(sb, metrics.SuccessRate);

            return sb.ToString();
        }

        private static PerformanceMetrics CalculateMetrics(ImportProgress progress, TimeSpan duration)
        {
            var seconds = duration.TotalSeconds;
            return new PerformanceMetrics(
                FilesPerSecond: seconds > 0 ? progress.ProcessedFiles / seconds : 0,
                HandsPerSecond: seconds > 0 ? progress.TotalHands / seconds : 0,
                SuccessRate: progress.TotalFiles > 0 ? progress.SuccessCount * 100.0 / progress.TotalFiles : 0
            );
        }

        private static void AppendHeader(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                        Import Completed                        ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
        }

        private static void AppendStatistics(StringBuilder sb, ImportProgress progress, PerformanceMetrics metrics)
        {
            sb.AppendLine($"  Statistics:");
            sb.AppendLine($"     • Total files:         {progress.TotalFiles}");
            sb.AppendLine($"     • Processed:           {progress.ProcessedFiles}");
            sb.AppendLine($"     • Successful:          {progress.SuccessCount}");
            sb.AppendLine($"     • Errors:              {progress.ErrorCount}");
            sb.AppendLine($"     • Duplicate hands:     {progress.TotalDuplicates}");
            sb.AppendLine($"     • Total hands:         {progress.TotalHands}");
            sb.AppendLine();
        }

        private static void AppendPerformance(StringBuilder sb, TimeSpan duration, PerformanceMetrics metrics)
        {
            sb.AppendLine($"  Performance:");
            sb.AppendLine($"     • Duration:            {duration.TotalSeconds:F2} sec");
            sb.AppendLine($"     • Speed:               {metrics.FilesPerSecond:F1} files/sec");
            sb.AppendLine($"     • Hands per second:    {metrics.HandsPerSecond:F1}");
            sb.AppendLine();
        }

        private static void AppendSuccessRate(StringBuilder sb, double successRate)
        {
            sb.AppendLine($"  Success rate: {successRate:F1}%");
        }

        private record PerformanceMetrics(
            double FilesPerSecond,
            double HandsPerSecond,
            double SuccessRate
        );
    }
}