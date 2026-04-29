using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Greedy Best-First Search nutzt nur die Heuristik h(n) und ignoriert g(n).
/// Dadurch ist die Suche oft schnell, liefert aber nicht immer den kuerzesten Pfad.
/// </summary>
public sealed class GreedyBestFirstSolver : IMazeSolver
{
    public string Id => "greedy";
    public string Name => "Greedy Best-First";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        long counter = 0;
        var openSet = new SortedSet<(int h, long counter, Cell cell)>(
            Comparer<(int h, long counter, Cell cell)>.Create((a, b) =>
            {
                int byH = a.h.CompareTo(b.h);
                if (byH != 0)
                    return byH;

                return a.counter.CompareTo(b.counter);
            }));

        var cameFrom = new Dictionary<Cell, Cell>();
        var seen = new HashSet<Cell> { start };

        openSet.Add((Heuristic(start, goal), counter++, start));

        while (openSet.Count > 0)
        {
            var currentTuple = openSet.Min;
            openSet.Remove(currentTuple);
            Cell current = currentTuple.cell;

            if (current != start && current != goal)
                yield return new SolverStep(current, CellState.Visited, Heuristic(current, goal), "Expand");

            if (current == goal)
                break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction))
                    continue;

                Cell neighbor = maze.GetNeighbor(current, direction);
                if (neighbor == null || seen.Contains(neighbor))
                    continue;

                seen.Add(neighbor);
                cameFrom[neighbor] = current;
                openSet.Add((Heuristic(neighbor, goal), counter++, neighbor));

                if (neighbor != goal)
                    yield return new SolverStep(neighbor, CellState.Frontier, Heuristic(neighbor, goal), "Open");
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

    private static int Heuristic(Cell a, Cell b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}