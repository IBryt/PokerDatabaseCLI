using PokerDatabaseCLI.Domain.Poker.Models;

namespace PokerDatabaseCLI.Domain.Poker;

/// <summary>
/// Represents the result of scanning files in a folder.
/// </summary>
public record ScanResult(
    IReadOnlyList<string> FilePaths
);

/// <summary>
/// Represents the result of parsing a single file.
/// </summary>
public record ParseFileResult(
    string FilePath,
    IReadOnlyDictionary<long, Hand> Hands,
    int HandsCount
);
