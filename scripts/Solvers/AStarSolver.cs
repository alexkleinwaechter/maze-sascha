using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// A*-Solver mit Manhattan-Heuristik fuer 4er-Nachbarschaft.
/// f(n) = g(n) + h(n), expandiert jeweils den Knoten mit dem kleinsten f-Wert.
/// </summary>
public sealed class AStarSolver : IMazeSolver
{
    public string Id => "a-star";
    public string Name => "A* (Manhattan)";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        long counter = 0;
        var openSet = new SortedSet<(int f, long counter, Cell cell)>(
            Comparer<(int f, long counter, Cell cell)>.Create((a, b) =>
            {
                int byF = a.f.CompareTo(b.f);
                if (byF != 0)
                    return byF;

                return a.counter.CompareTo(b.counter);
            }));

        var gScore = new Dictionary<Cell, int> { [start] = 0 };
        var cameFrom = new Dictionary<Cell, Cell>();
        var closed = new HashSet<Cell>();

        openSet.Add((Heuristic(start, goal), counter++, start));

        while (openSet.Count > 0)
        {
            var currentTuple = openSet.Min;
            openSet.Remove(currentTuple);
            Cell current = currentTuple.cell;

            if (!closed.Add(current))
                continue;

            int currentG = gScore[current];

            if (current != start && current != goal)
                yield return new SolverStep(current, CellState.Visited, currentG, "Expand");

            if (current == goal)
                break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction))
                    continue;

                Cell neighbor = maze.GetNeighbor(current, direction);
                if (neighbor == null || closed.Contains(neighbor))
                    continue;

                int tentativeG = currentG + 1;
                if (gScore.TryGetValue(neighbor, out int knownG) && tentativeG >= knownG)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;

                int f = tentativeG + Heuristic(neighbor, goal);
                openSet.Add((f, counter++, neighbor));

                if (neighbor != goal)
                    yield return new SolverStep(neighbor, CellState.Frontier, tentativeG, "Open");
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