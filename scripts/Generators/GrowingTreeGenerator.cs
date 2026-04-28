using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Growing Tree mit konfigurierbarer Auswahlstrategie.
/// Default: 75% neuestes Element, 25% zufaelliges Element.
/// </summary>
public sealed class GrowingTreeGenerator : IMazeGenerator
{
    public string Id => "growing-tree";
    public string Name => "Growing Tree (75% newest, 25% random)";

    private readonly float _newestProbability;

    public GrowingTreeGenerator(float newestProbability = 0.75f)
    {
        _newestProbability = newestProbability;
    }

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        var visited = new bool[maze.Width, maze.Height];
        var active = new List<Cell>();

        Cell start = maze.GetCell(random.Next(maze.Width), random.Next(maze.Height));
        visited[start.X, start.Y] = true;
        active.Add(start);
        yield return new GenerationStep(start, null, null, CellState.Carving, "Start");

        while (active.Count > 0)
        {
            int idx = random.NextDouble() < _newestProbability
                ? active.Count - 1
                : random.Next(active.Count);

            Cell current = active[idx];

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
                active.RemoveAt(idx);
                yield return new GenerationStep(current, null, null, CellState.Open, "Drop");
                continue;
            }

            Direction pick = unvisited[random.Next(unvisited.Count)];
            Cell next = maze.GetNeighbor(current, pick);

            maze.RemoveWallBetween(current, pick);
            visited[next.X, next.Y] = true;
            active.Add(next);

            yield return new GenerationStep(next, current, pick, CellState.Carving, "Carve");
        }
    }
}
