namespace Maze.Model;

/// <summary>
/// Vier Himmelsrichtungen fuer ein 4-Nachbarschafts-Gitter.
/// Reihenfolge ist wichtig, weil wir per Index gegen <c>_offsets</c> indexieren.
/// </summary>
public enum Direction
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

/// <summary>
/// Hilfsmethoden rund um die <see cref="Direction"/>-Aufzaehlung.
/// Bewusst als statische Klasse, damit Schueler die Funktionen ohne Instanzierung verwenden koennen.
/// </summary>
public static class DirectionHelper
{
    // Versatz (dx, dy) je Richtung. Reihenfolge passt zur Enum.
    private static readonly (int dx, int dy)[] _offsets =
    {
        (0, -1), // North
        (1, 0),  // East
        (0, 1),  // South
        (-1, 0)  // West
    };

    public static (int dx, int dy) Offset(Direction direction) =>
        _offsets[(int)direction];

    /// <summary>
    /// Liefert die entgegengesetzte Richtung. Praktisch beim Wanddurchbruch:
    /// wenn wir von A nach Osten zu B gehen, muessen wir bei B die Westwand entfernen.
    /// </summary>
    public static Direction Opposite(Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.East => Direction.West,
        Direction.South => Direction.North,
        Direction.West => Direction.East,
        _ => throw new System.ArgumentOutOfRangeException(nameof(direction))
    };

    /// <summary>
    /// Alle vier Richtungen als statisches Array (read-only View).
    /// Praktisch fuer <c>foreach</c> in Generatoren und Solvern.
    /// </summary>
    public static readonly Direction[] All = { Direction.North, Direction.East, Direction.South, Direction.West };
}