using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Gemeinsame Schnittstelle aller Loesungsalgorithmen.
/// Liefert eine Sequenz von <see cref="SolverStep"/>-Objekten fuer die Animation.
/// </summary>
public interface IMazeSolver
{
    string Id { get; }
    string Name { get; }

    /// <summary>
    /// Loest das uebergebene Labyrinth von Start nach Ziel in-place und liefert die Schritte.
    /// Implementierungen markieren am Ende den finalen Pfad als <see cref="CellState.Path"/>.
    /// </summary>
    IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal);
}