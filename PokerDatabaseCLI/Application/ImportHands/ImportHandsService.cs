using Open.ChannelExtensions;
using PokerDatabaseCLI.Application.Import.ImportHands;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;
using PokerDatabaseCLI.Domain.Poker.Models;

namespace PokerDatabaseCLI.Application.Import;

public static class ImportHandsService
{
    /// <summary>
    /// Запускает процесс импорта из указанной папки
    /// </summary>
    public static Result<string> StartImport(
        ImportServiceParams importServiceParams)
    {
        try
        {
            return PokerDatabaseCLI.Domain.Poker.Import.ValidatePath(importServiceParams.Path)
                .Bind(PokerDatabaseCLI.Domain.Poker.Import.ScanDirectory)
                .Map(directories =>
                {
                    ProcessImportPipeline(
                        directories,
                        importServiceParams.Progress,
                        importServiceParams.CancellationToken,
                        importServiceParams.ImportDependencies
                    );
                    return "Import started.";
                });
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Import error: {ex.Message}");
        }
    }

    /// <summary>
    /// Обрабатывает pipeline: файлы → парсинг → обработка → сохранение
    /// </summary>
    private static void ProcessImportPipeline(
        ScanResult scan,
        IProgress<ImportProgress> progress,
        CancellationToken token,
        ImportDependencies importAppDependencies)
    {
        var state = ImportState.Create(scan.FilePaths.Count);
        var stateLock = new object();

        PokerDatabaseCLI.Domain.Poker.Import.ProcessFiles(scan)
            .ToChannel(capacity: 10, singleReader: true, token)
            .Pipe(
                maxConcurrency: Environment.ProcessorCount - 3,
                capacity: 10,
                transform: ProcessHands,
                cancellationToken: token
            )
            .ReadAll(
                cancellationToken: token,
                result =>
                {
                    lock (stateLock)
                    {
                        state = ProcessImportResult(
                            result,
                            state,
                            progress,
                            token,
                            importAppDependencies
                        );
                    }
                }
            );
    }

    /// <summary>
    /// Обрабатывает результат парсинга одного файла
    /// Возвращает новое состояние
    /// </summary>
    private static ImportState ProcessImportResult(
        Result<IReadOnlyDictionary<long, Hand>> result,
        ImportState state,
        IProgress<ImportProgress> progress,
        CancellationToken token,
        ImportDependencies importAppDependencies)
    {
        if (token.IsCancellationRequested)
            return state;

        // Обновляем состояние
        var updatedState = state.IncrementProcessed();

        // Обрабатываем результат
        updatedState = result.Match(
            onSuccess: hands => HandleSuccessfulParse(hands, updatedState),
            onFailure: error => updatedState.IncrementErrors()
        );

        // Проверяем необходимость сохранения батча
        if (ShouldSaveBatch(updatedState, token, importAppDependencies))
        {
            updatedState = SaveBatchAndReport(
                updatedState,
                progress,
                importAppDependencies
            );
        }

        return updatedState;
    }

    /// <summary>
    /// Обрабатывает успешно распарсенные раздачи
    /// </summary>
    private static ImportState HandleSuccessfulParse(
        IReadOnlyDictionary<long, Hand> hands,
        ImportState state)
    {
        var newState = state.IncrementSuccess();
        return newState.AccumulateHands(hands);
    }

    /// <summary>
    /// Проверяет, нужно ли сохранить накопленный батч
    /// </summary>
    private static bool ShouldSaveBatch(
        ImportState state,
        CancellationToken token,
        ImportDependencies importAppDependencies)
    {
        return state.HandsCount >= importAppDependencies.BatchSize ||
               state.IsCompleted ||
               token.IsCancellationRequested;
    }

    /// <summary>
    /// Сохраняет батч в БД и отправляет отчёт о прогрессе
    /// Возвращает новое состояние
    /// </summary>
    private static ImportState SaveBatchAndReport(
        ImportState state,
        IProgress<ImportProgress> progress,
        ImportDependencies importAppDependencies)
    {
        var handsToSave = new Dictionary<long, Hand>(
            state.HandsAccumulator.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );

        var saveResult = importAppDependencies.SaveBatch(handsToSave);

        var updatedState = saveResult.Match(
            onSuccess: stats => state
                .AddDuplicates(stats.DuplicatesCount)
                .AddTotalHands(stats.Hands.Count),
            onFailure: error => state.IncrementErrors()
        );

        updatedState = updatedState.ClearAccumulator();

        progress.Report(updatedState.CreateProgress());

        return updatedState;
    }

    /// <summary>
    /// Дополнительная обработка раздач (расширяемо для будущей логики)
    /// </summary>
    private static Result<IReadOnlyDictionary<long, Hand>> ProcessHands(
        Result<ParseFileResult> parseResult) =>
        parseResult.Map(x => x.Hands);
}
