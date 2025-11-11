using PokerDatabaseCLI.Application;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Domain.Poker;
using System.Collections.Concurrent;

namespace PokerDatabaseCLI.Infrastructure.Persistence;

/// <summary>
/// Provides methods for managing poker hands in an in-memory repository.
/// </summary>
public static class HandRepository
{
    private static ConcurrentDictionary<long, Hand> _db = new();

    /// <summary>
    /// Saves a batch of hands to the repository, filtering out duplicates.
    /// </summary>
    /// <param name="hands">A dictionary of hands keyed by their unique number.</param>
    /// <returns>
    /// A <see cref="Result{SaveStats}"/> containing information about the saved hands and the number of duplicates.
    /// </returns>
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

    /// <summary>
    /// Deletes a hand from the repository by its unique number.
    /// </summary>
    /// <param name="number">The number of the hand to delete.</param>
    /// <returns>
    /// A <see cref="Result{Boolean}"/> indicating whether the hand was successfully removed.
    /// </returns>
    public static Result<bool> DeleteHandByNumber(long number)
    {
        return ResultUtils.Try(() =>
        {
            _db.TryRemove(number, out var removedValue);
            return removedValue == null ? false : true;
        });
    }

    /// <summary>
    /// Retrieves general information about the repository, including total hands and distinct players.
    /// </summary>
    /// <returns>A <see cref="Result{CommonInfo}"/> containing counts of hands and players.</returns>
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

    /// <summary>
    /// Retrieves detailed information for a specific player, including their total hands and a list of recent hands.
    /// </summary>
    /// <param name="name">The name of the player.</param>
    /// <param name="CountHands">The maximum number of recent hands to retrieve.</param>
    /// <returns>A <see cref="Result{InfoOnPlayer}"/> containing player information.</returns>
    public static Result<InfoOnPlayer> GetInfoOnPlayer(string name, int CountHands)
    {
        return ResultUtils.Try(() =>
        {
            var count = _db.Values
                .Where(h => h.Players.Any(p => p.Name.Equals(name)))
                .Count();

            var hands = _db.Values
                .OrderByDescending(h => h.DateTime)
                .Where(h => h.Players.Any(p => p.Name.Equals(name)))
                .Take(CountHands)
                .OrderBy(h => h.DateTime)
                .ToList();

            return new InfoOnPlayer(
                CountHands: count,
                Name: name,
                hands: hands
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
