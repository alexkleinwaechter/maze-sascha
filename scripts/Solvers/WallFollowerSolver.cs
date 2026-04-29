using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Linke-Hand-Regel fuer einfach zusammenhaengende Labyrinthe.
/// Prioritaet der Bewegung: links, geradeaus, rechts, dann umdrehen.
/// </summary>
public sealed class WallFollowerSolver : IMazeSolver
{
    public string Id => "wall-follower";
    public string Name => "Wall Follower (Linke Hand)";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        Cell current = start;
        Direction facing = Direction.North;
        int stepCount = 0;

        var visited = new HashSet<Cell> { start };
        var cameFrom = new Dictionary<Cell, Cell>();

        // Sicherheitslimit, damit bei ungeeigneten Maze-Eigenschaften keine Endlosschleife entsteht.
        int maxSteps = maze.Width * maze.Height * 4;

        while (current != goal && stepCount < maxSteps)
        {
            bool moved = false;
            foreach (var direction in new[]
                     {
                         TurnLeft(facing),
                         facing,
                         TurnRight(facing),
                         TurnAround(facing)
                     })
            {
                if (current.HasWall(direction))
                    continue;

                Cell next = maze.GetNeighbor(current, direction);
                if (next == null)
                    continue;

                facing = direction;
                if (!cameFrom.ContainsKey(next))
                    cameFrom[next] = current;

                current = next;
                stepCount++;

                if (current != start && current != goal)
                {
                    bool firstVisit = visited.Add(current);
                    yield return new SolverStep(
                        current,
                        firstVisit ? CellState.Visited : CellState.Frontier,
                        stepCount,
                        firstVisit ? "Walk" : "Revisit");
                }

                moved = true;
                break;
            }

            if (!moved)
                break;
        }

        if (current != goal)
            yield break;

        var path = new List<Cell>();
        Cell step = goal;
        path.Add(step);

        while (step != start)
        {
            if (!cameFrom.TryGetValue(step, out Cell parent))
                yield break;

            step = parent;
            path.Add(step);
        }

        path.Reverse();

        for (int index = 0; index < path.Count; index++)
        {
            Cell pathCell = path[index];
            if (pathCell == start || pathCell == goal)
                continue;

            yield return new SolverStep(pathCell, CellState.Path, index, "Path");
        }
    }

    private static Direction TurnLeft(Direction direction)
    {
        return (Direction)(((int)direction + 3) % 4);
    }

    private static Direction TurnRight(Direction direction)
    {
        return (Direction)(((int)direction + 1) % 4);
    }

    private static Direction TurnAround(Direction direction)
    {
        return (Direction)(((int)direction + 2) % 4);
    }
}