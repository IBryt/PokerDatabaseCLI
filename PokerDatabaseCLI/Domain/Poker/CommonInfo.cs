namespace PokerDatabaseCLI.Domain.Poker;

/// <summary>
/// Represents common statistical information about the current dataset or game state.
/// </summary>
/// <param name="CountPlayers">The total number of unique players.</param>
/// <param name="CountHands">The total number of hands stored.</param>
public record CommonInfo(
     int CountPlayers,
     int CountHands
);
