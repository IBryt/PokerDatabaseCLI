namespace PokerDatabaseCLI.Core;

/// <summary>
/// Represents a unit type that carries no data but indicates a successful result in functional operations.
/// </summary>
public record Unit
{
    public static readonly Unit Value = new();
    private Unit() { }
}