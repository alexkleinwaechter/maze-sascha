namespace Maze.Model;

/// <summary>
/// Visueller / logischer Zustand einer Zelle. Wird vom Renderer auf eine Farbe gemappt.
/// </summary>
public enum CellState
{
    /// <summary>Noch von keinem Algorithmus angefasst.</summary>
    Untouched = 0,
    /// <summary>Aktuell vom Generator besucht (Carving-Front).</summary>
    Carving = 1,
    /// <summary>Vom Generator fertig bearbeitet, aber kein Solver-Status.</summary>
    Open = 2,
    /// <summary>Solver hat die Zelle in der Frontier / OpenSet.</summary>
    Frontier = 3,
    /// <summary>Solver hat die Zelle bereits abgeschlossen (ClosedSet).</summary>
    Visited = 4,
    /// <summary>Teil des finalen, gefundenen Pfades.</summary>
    Path = 5,
    /// <summary>Startzelle des Solvers.</summary>
    Start = 6,
    /// <summary>Zielzelle des Solvers.</summary>
    Goal = 7,
    /// <summary>Markierung fuer Dead-End-Filling: Zelle wurde "verstopft".</summary>
    Filled = 8
}
