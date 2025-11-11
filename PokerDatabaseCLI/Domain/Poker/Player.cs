namespace PokerDatabaseCLI.Domain.Poker;

/// <summary>
/// Represents a poker player.
/// </summary>
public record Player(
    string Name,
    decimal Chips,
    string? Cards = null
);