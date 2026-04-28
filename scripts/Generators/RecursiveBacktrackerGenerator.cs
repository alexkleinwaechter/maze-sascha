using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Klassischer Recursive Backtracker (DFS-Carving), iterativ mit Stack.
/// </summary>
public sealed class RecursiveBacktrackerGenerator : IMazeGenerator
{
    public string Id => "recursive-backtracker";
    public string Name => "Recursive Backtracker";

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        var visited = new bool[maze.Width, maze.Height];
        var stack = new Stack<Cell>();

        Cell start = maze.GetCell(0, 0);
        visited[start.X, start.Y] = true;
        stack.Push(start);
        yield return new GenerationStep(start, null, null, CellState.Carving, "Start");

        while (stack.Count > 0)
        {
            Cell current = stack.Peek();

            var unvisited = new List<Direction>();
            foreach (var direction in DirectionHelper.All)
            {
                Cell neighbor = maze.GetNeighbor(current, direction);
                if (neighbor != null && !visited[neighbor.X, neighbor.Y])
                {
                    unvisited.Add(direction);
                }
            }

            if (unvisited.Count == 0)
            {
                stack.Pop();
                yield return new GenerationStep(current, null, null, CellState.Open, "Backtrack");
                continue;
            }

            Direction pick = unvisited[random.Next(unvisited.Count)];
            Cell next = maze.GetNeighbor(current, pick);

            maze.RemoveWallBetween(current, pick);
            visited[next.X, next.Y] = true;
            stack.Push(next);

            yield return new GenerationStep(next, current, pick, CellState.Carving, "Carve");
        }
    }
}
