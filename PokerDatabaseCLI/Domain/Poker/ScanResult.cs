namespace PokerDatabaseCLI.Domain.Poker;

/// <summary>
/// Represents result of scanning a folder for files.
/// </summary>
public record ScanResult(IReadOnlyList<string> FilePaths);