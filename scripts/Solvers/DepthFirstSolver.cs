using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Tiefensuche mit Stack. Findet einen Pfad, aber nicht zwingend den kuerzesten.
/// Die Visualisierung bewegt sich eher wie eine einzelne Suchspur tief in einen Ast hinein.
/// </summary>
public sealed class DepthFirstSolver : IMazeSolver
{
    public string Id => "dfs";
    public string Name => "Depth-First Search";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        var stack = new Stack<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var depths = new Dictionary<Cell, int>();
        var seen = new HashSet<Cell>();

        stack.Push(start);
        seen.Add(start);
        depths[start] = 0;

        while (stack.Count > 0)
        {
            Cell current = stack.Pop();
            int currentDepth = depths[current];

            if (current != start && current != goal)
                yield return new SolverStep(current, CellState.Visited, currentDepth, "Visit");

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
                depths[next] = currentDepth + 1;
                stack.Push(next);

                if (next != goal)
                    yield return new SolverStep(next, CellState.Frontier, depths[next], "Push");
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