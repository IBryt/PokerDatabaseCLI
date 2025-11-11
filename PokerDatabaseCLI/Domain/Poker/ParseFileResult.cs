namespace PokerDatabaseCLI.Domain.Poker;

/// <summary>
/// Represents parsed data of a single poker hand history file.
/// </summary>
public record ParseFileResult(
    string FilePath,
    IReadOnlyList<Hand> Hands
);
