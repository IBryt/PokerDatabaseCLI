using PokerDatabaseCLI.Application.Import.ImportHands;
using PokerDatabaseCLI.Domain.Poker.Models;

namespace PokerDatabaseCLI.Application.Import;

public record ImportState(
    int TotalFiles,
    int ProcessedCount,
    int SuccessCount,
    int ErrorCount,
    int TotalDuplicates,
    int TotalHands,
    IReadOnlyDictionary<long, Hand> HandsAccumulator
)
{
    /// <summary>
    /// Вычисляемые свойства
    /// </summary>
    public int HandsCount => HandsAccumulator.Count;
    public bool IsCompleted => ProcessedCount == TotalFiles;

    /// <summary>
    /// Фабричный метод для создания начального состояния
    /// </summary>
    public static ImportState Create(int totalFiles) => new(
        TotalFiles: totalFiles,
        ProcessedCount: 0,
        SuccessCount: 0,
        ErrorCount: 0,
        TotalDuplicates: 0,
        TotalHands: 0,
        HandsAccumulator: new Dictionary<long, Hand>()
    );

    /// <summary>
    /// Функциональные "изменения" состояния - возвращают новое состояние
    /// </summary>
    public ImportState IncrementProcessed() => this with
    {
        ProcessedCount = ProcessedCount + 1
    };

    public ImportState IncrementSuccess() => this with
    {
        SuccessCount = SuccessCount + 1
    };

    public ImportState IncrementErrors() => this with
    {
        ErrorCount = ErrorCount + 1
    };

    public ImportState AddDuplicates(int count) => this with
    {
        TotalDuplicates = TotalDuplicates + count
    };

    public ImportState AddTotalHands(int count) => this with
    {
        TotalHands = TotalHands + count
    };

    /// <summary>
    /// Добавляет раздачу в аккумулятор (создаёт новый Dictionary)
    /// </summary>
    public ImportState AccumulateHand(long id, Hand hand)
    {
        var newAccumulator = new Dictionary<long, Hand>(
            HandsAccumulator as Dictionary<long, Hand> ??
            HandsAccumulator.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );
        newAccumulator[id] = hand;

        return this with { HandsAccumulator = newAccumulator };
    }

    /// <summary>
    /// Добавляет несколько раздач за раз (эффективнее чем по одной)
    /// </summary>
    public ImportState AccumulateHands(IReadOnlyDictionary<long, Hand> hands)
    {
        var newAccumulator = new Dictionary<long, Hand>(
            HandsAccumulator as Dictionary<long, Hand> ??
            HandsAccumulator.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );

        foreach (var hand in hands)
            newAccumulator[hand.Key] = hand.Value;

        return this with { HandsAccumulator = newAccumulator };
    }

    /// <summary>
    /// Очищает аккумулятор
    /// </summary>
    public ImportState ClearAccumulator() => this with
    {
        HandsAccumulator = new Dictionary<long, Hand>()
    };

    /// <summary>
    /// Создаёт снимок текущего прогресса
    /// </summary>
    public ImportProgress CreateProgress() => new(
        TotalDuplicates: TotalDuplicates,
        SuccessCount: SuccessCount,
        Percentage: (double)ProcessedCount / TotalFiles * 100,
        ProcessedFiles: ProcessedCount,
        TotalFiles: TotalFiles,
        TotalHands: TotalHands,
        ErrorCount: ErrorCount,
        IsCompleted: IsCompleted
    );
}