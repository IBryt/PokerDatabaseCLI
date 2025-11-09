using PokerDatabaseCLI.Domain.Poker.Models;

namespace PokerDatabaseCLI.Infrastructure.Persistence;

public static class HandRepository
{
    private static Dictionary<long, Hand> _db = new();

    public static IReadOnlyList<long> GetDublicates(IReadOnlyDictionary<long, Hand> hands)
    {
        return _db.Where(h => hands.Keys.Contains(h.Key)).Select(x => x.Key).ToList();
    }

    public static IReadOnlyList<Hand> AddHands(IReadOnlyDictionary<long, Hand> hands)
    {
        var list = new List<Hand>();
        foreach (var item in hands)
        {
            list.Add(item.Value);
            _db.Add(item.Key, item.Value);
        }
        return list;
    }
}
