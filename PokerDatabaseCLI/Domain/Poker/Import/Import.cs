using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Domain.Poker.Models;
using System.Globalization;
using System.Text;

namespace PokerDatabaseCLI.Domain.Poker.Import;

public static class Import
{
    /// <summary>
    /// Validates the folder path.
    /// </summary>
    /// <param name="path">The folder path to validate.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the original path if valid, 
    /// or a failure message if invalid.
    /// </returns>
    public static Result<string> ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<string>.Fail("The path cannot be empty.");

        if (!Directory.Exists(path))
            return Result<string>.Fail($"Directory not found: {path}");

        return Result<string>.Ok(path);
    }

    /// <summary>
    /// Scans a folder for .txt files.
    /// </summary>
    /// <param name="path">The folder path to scan.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="ScanResult"/> if successful,
    /// or a failure message if no files are found or an error occurs.
    /// </returns>
    public static Result<ScanResult> ScanDirectory(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories)
                                 .ToList();

            if (files.Count == 0)
            {
                return Result<ScanResult>.Fail("No .txt files found in the folder.");
            }

            return Result<ScanResult>.Ok(new ScanResult(files));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<ScanResult>.Fail("Access to the folder is denied.");
        }
        catch (Exception ex)
        {
            return Result<ScanResult>.Fail($"Error scanning folder: {ex.Message}");
        }
    }

    public static IEnumerable<Result<ParseFileResult>> ProcessFiles(ScanResult scan)
    {
        foreach (var path in scan.FilePaths)
        {
            yield return Import.ImportFile(path);
        }
    }

    private static Result<ParseFileResult> ImportFile(string path)
    {
        try
        {
            var hands = ImportHandsFromFile(path);

            return Result<ParseFileResult>.Ok(new ParseFileResult(
                FilePath: path,
                Hands: hands,
                HandsCount: hands.Count
            ));
        }
        catch (Exception ex)
        {
            return Result<ParseFileResult>.Fail(
                $"Parsing error {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    private static Dictionary<long, Hand> ImportHandsFromFile(string path)
    {
        var hands = new Dictionary<long, Hand>();
        using var fileStream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024);

        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;
            var lineSpan = line.AsSpan();
            if (lineSpan.StartsWith("PokerStars Hand #"))
            {
                var hand = ParseSingleHand(lineSpan, reader);
                if (hand != null)
                {
                    hands.Add(hand.Number, hand);
                }
            }
        }

        return hands;
    }

    private static Hand? ParseSingleHand(ReadOnlySpan<char> lineSpan, StreamReader reader)
    {
        long number = GetNumberOptimized(lineSpan);
        var players = new List<Player>(10);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var span = line.AsSpan();

            if (span.StartsWith("Seat "))
            {
                var player = ParseSeatLineOptimized(span);
                players.Add(player);
            }
            else if (span.StartsWith("Dealt to "))
            {
                ParseDealtToLine(span, players);
                break;
            }
        }

        return new Hand(number, players);
    }

    private static long GetNumberOptimized(ReadOnlySpan<char> line)
    {
        int hashIndex = line.IndexOf('#') + 1;
        int colonIndex = line.Slice(hashIndex).IndexOf(':') + hashIndex;
        return long.Parse(line.Slice(hashIndex, colonIndex - hashIndex));
    }

    private static Player ParseSeatLineOptimized(ReadOnlySpan<char> line)
    {
        int colonIndex = line.IndexOf(':') + 2;
        int chipsStart = line.LastIndexOf('(');
        var nameSpan = line.Slice(colonIndex, chipsStart - colonIndex - 1);
        string playerName = nameSpan.ToString();

        int chipsEnd = line.LastIndexOf(" in chips)");
        var chipsSpan = line.Slice(chipsStart + 2, chipsEnd - chipsStart - 2);
        decimal chips = decimal.Parse(chipsSpan, NumberStyles.Any, CultureInfo.InvariantCulture);

        return new Player(playerName, chips);
    }

    private static void ParseDealtToLine(ReadOnlySpan<char> line, List<Player> players)
    {
        int toIndex = line.IndexOf(" to ");
        int bracketStart = line.IndexOf('[');
        int bracketEnd = line.IndexOf(']');

        var nameSpan = line.Slice(toIndex + 4, bracketStart - toIndex - 5);
        var cardsSpan = line.Slice(bracketStart + 1, bracketEnd - bracketStart - 1);

        string name = nameSpan.ToString();
        string cards = cardsSpan.ToString();

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Name.Equals(name))
            {
                players[i] = players[i] with { Cards = cards };
                break;
            }
        }
    }
}
