using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Dead-End Filling fuellt iterativ alle Sackgassen, bis nur der Loesungskorridor uebrig bleibt.
/// Funktioniert in perfekten Labyrinthen besonders gut.
/// </summary>
public sealed class DeadEndFillingSolver : IMazeSolver
{
    public string Id => "dead-end-filling";
    public string Name => "Dead-End Filling";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        var filled = new HashSet<Cell>();
        bool changed = true;
        int wave = 0;

        while (changed)
        {
            changed = false;

            foreach (var cell in maze.AllCells())
            {
                if (cell == start || cell == goal || filled.Contains(cell))
                    continue;

                if (CountOpenNeighbors(maze, cell, filled) > 1)
                    continue;

                filled.Add(cell);
                changed = true;
                yield return new SolverStep(cell, CellState.Filled, wave, "Fill");
            }

            wave++;
        }

        // Auf den verbleibenden Zellen (nicht gefuellt) den finalen Pfad von Start nach Ziel suchen.
        var queue = new Queue<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var seen = new HashSet<Cell> { start };

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();
            if (current == goal)
                break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction))
                    continue;

                Cell next = maze.GetNeighbor(current, direction);
                if (next == null || filled.Contains(next) || !seen.Add(next))
                    continue;

                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (goal != start && !cameFrom.ContainsKey(goal))
            yield break;

        var path = new List<Cell>();
        Cell step = goal;
        path.Add(step);

        while (step != start)
        {
            step = cameFrom[step];
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

    private static int CountOpenNeighbors(Model.Maze maze, Cell cell, HashSet<Cell> filled)
    {
        int count = 0;

        foreach (var direction in DirectionHelper.All)
        {
            if (cell.HasWall(direction))
                continue;

            Cell neighbor = maze.GetNeighbor(cell, direction);
            if (neighbor == null || filled.Contains(neighbor))
                continue;

            count++;
        }

        return count;
    }
}