namespace PokerDatabaseCLI.Domain.Poker;

/// <summary>
/// Represents detailed information about a specific player.
/// </summary>
public record InfoOnPlayer(
    int CountHands,
    string Name,
    IReadOnlyList<Hand> hands
);

