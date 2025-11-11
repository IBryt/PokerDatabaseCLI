namespace PokerDatabaseCLI.Domain.Poker;

/// <summary>
/// Represents a poker hand.
/// </summary>
public record Hand(
    long Number,
    DateTime DateTime,
    IReadOnlyList<Player> Players
);