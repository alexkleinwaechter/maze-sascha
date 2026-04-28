using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Ein einzelner Animationsschritt eines Solvers.
/// </summary>
public sealed record SolverStep(
    Cell Cell,
    CellState NewState,
    int Distance,
    string Description
);
