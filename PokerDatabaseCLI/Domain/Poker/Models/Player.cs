namespace PokerDatabaseCLI.Domain.Poker.Models;

/// <summary>
/// Represents a poker player.
/// </summary>
public record Player(
    string Name,
    decimal Chips,
    string? Cards = null
);