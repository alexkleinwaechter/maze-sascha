using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Klassische Breitensuche. Garantiert den kuerzesten Pfad in einem ungewichteten Gitter.
/// Die Visualisierung breitet sich als gleichmaessige Welle vom Start aus.
/// </summary>
public sealed class BreadthFirstSolver : IMazeSolver
{
    public string Id => "bfs";
    public string Name => "Breadth-First Search";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        var queue = new Queue<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var distances = new Dictionary<Cell, int>();
        var seen = new HashSet<Cell>();

        queue.Enqueue(start);
        seen.Add(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();
            int currentDistance = distances[current];

            if (current != start && current != goal)
                yield return new SolverStep(current, CellState.Visited, currentDistance, "Visit");

            if (current == goal)
                break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction))
                    continue;

                Cell next = maze.GetNeighbor(current, direction);
                if (next == null || seen.Contains(next))
                    continue;

                seen.Add(next);
                cameFrom[next] = current;
                distances[next] = currentDistance + 1;
                queue.Enqueue(next);

                if (next != goal)
                    yield return new SolverStep(next, CellState.Frontier, distances[next], "Enqueue");
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
}