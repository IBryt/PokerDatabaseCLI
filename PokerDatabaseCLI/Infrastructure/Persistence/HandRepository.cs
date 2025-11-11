using PokerDatabaseCLI.Application;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Domain.Poker;
using System.Collections.Concurrent;

namespace PokerDatabaseCLI.Infrastructure.Persistence;

public static class HandRepository
{
    private static ConcurrentDictionary<long, Hand> _db = new();

    public static Result<SaveStats> SaveBatch(IReadOnlyDictionary<long, Hand> hands)
    {
        return ResultUtils.Try(() =>
        {
            var duplicateIds = GetDuplicates(hands);
            var uniqueHands = FilterUnique(hands, duplicateIds);
            AddHands(uniqueHands);
            return new SaveStats(
                    Hands: uniqueHands,
                    DuplicatesCount: duplicateIds.Count
                );
        });
    }
    public static Result<bool> DeleteHandByNumber(long number)
    {
        return ResultUtils.Try(() =>
        {
            _db.TryRemove(number, out var removedValue);
            return removedValue == null ? false : true;
        });
    }

    public static Result<CommonInfo> GetInfo()
    {
        return ResultUtils.Try(() =>
        {
            return new CommonInfo(
                CountHands: _db.Count,
                CountPlayers: _db.Values.SelectMany(h => h.Players.Select(p => p.Name)).Distinct().Count()
            );
        });
    }


    private static void AddHands(IReadOnlyDictionary<long, Hand> hands)
    {
        foreach (var hand in hands)
        {
            _db.TryAdd(hand.Key, hand.Value);
        }
    }

    private static IReadOnlySet<long> GetDuplicates(IReadOnlyDictionary<long, Hand> hands)
    {
        return _db.Where(h => hands.Keys.Contains(h.Key)).Select(x => x.Key).ToHashSet();
    }

    private static IReadOnlyDictionary<long, Hand> FilterUnique(
       IReadOnlyDictionary<long, Hand> hands,
       IReadOnlySet<long> duplicateIds) =>
       hands
           .Where(h => !duplicateIds.Contains(h.Key))
           .ToDictionary();


}
