namespace PokerDatabaseCLI.Domain.Poker.Models;

/// <summary>
/// Represents a poker hand.
/// </summary>
public record Hand(
    long Number,
    IReadOnlyList<Player> Players
);