using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Cellular Automata (4-5) fuer hoehlenartige Layouts.
/// </summary>
public sealed class CellularAutomataGenerator : IMazeGenerator
{
    public string Id => "cellular-automata";
    public string Name => "Cellular Automata (4-5)";

    private const int Iterations = 4;
    private const float InitialWallChance = 0.45f;
    private const int BirthThreshold = 5;
    private const int SurvivalThreshold = 4;

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        // true = Wand, false = offen
        var grid = new bool[maze.Width, maze.Height];
        for (int y = 0; y < maze.Height; y++)
        for (int x = 0; x < maze.Width; x++)
        {
            grid[x, y] = random.NextDouble() < InitialWallChance
                || x == 0 || y == 0 || x == maze.Width - 1 || y == maze.Height - 1;
        }

        for (int iter = 0; iter < Iterations; iter++)
        {
            var next = new bool[maze.Width, maze.Height];
            for (int y = 0; y < maze.Height; y++)
            for (int x = 0; x < maze.Width; x++)
            {
                int wallCount = CountWallNeighbors(grid, x, y, maze.Width, maze.Height);
                next[x, y] = grid[x, y]
                    ? wallCount >= SurvivalThreshold
                    : wallCount >= BirthThreshold;
            }

            grid = next;
            yield return new GenerationStep(maze.GetCell(0, 0), null, null, CellState.Carving, $"Iter {iter + 1}");
        }

        var labels = LabelComponents(grid, maze.Width, maze.Height);
        int dominant = DominantLabel(labels, maze.Width, maze.Height);
        if (dominant >= 0)
        {
            for (int y = 0; y < maze.Height; y++)
            for (int x = 0; x < maze.Width; x++)
            {
                if (labels[x, y] != dominant)
                {
                    grid[x, y] = true;
                }
            }
        }

        for (int y = 0; y < maze.Height; y++)
        for (int x = 0; x < maze.Width; x++)
        {
            Cell cell = maze.GetCell(x, y);
            cell.State = grid[x, y] ? CellState.Filled : CellState.Open;

            if (!grid[x, y] && x + 1 < maze.Width && !grid[x + 1, y])
            {
                maze.RemoveWallBetween(cell, Direction.East);
            }

            if (!grid[x, y] && y + 1 < maze.Height && !grid[x, y + 1])
            {
                maze.RemoveWallBetween(cell, Direction.South);
            }

            yield return new GenerationStep(cell, null, null, cell.State, "Project");
        }
    }

    private static int CountWallNeighbors(bool[,] grid, int x, int y, int w, int h)
    {
        int count = 0;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0)
            {
                continue;
            }

            int nx = x + dx;
            int ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= w || ny >= h)
            {
                count++;
                continue;
            }

            if (grid[nx, ny])
            {
                count++;
            }
        }

        return count;
    }

    private static int[,] LabelComponents(bool[,] grid, int w, int h)
    {
        var labels = new int[w, h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            labels[x, y] = -1;
        }

        int next = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (grid[x, y] || labels[x, y] != -1)
            {
                continue;
            }

            FloodFill(grid, labels, x, y, next++, w, h);
        }

        return labels;
    }

    private static void FloodFill(bool[,] grid, int[,] labels, int sx, int sy, int label, int w, int h)
    {
        var stack = new Stack<(int x, int y)>();
        stack.Push((sx, sy));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            if (x < 0 || y < 0 || x >= w || y >= h)
            {
                continue;
            }

            if (grid[x, y] || labels[x, y] != -1)
            {
                continue;
            }

            labels[x, y] = label;
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }
    }

    private static int DominantLabel(int[,] labels, int w, int h)
    {
        var counts = new Dictionary<int, int>();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int label = labels[x, y];
            if (label < 0)
            {
                continue;
            }

            counts.TryGetValue(label, out int count);
            counts[label] = count + 1;
        }

        int best = -1;
        int bestCount = -1;
        foreach (var pair in counts)
        {
            if (pair.Value > bestCount)
            {
                best = pair.Key;
                bestCount = pair.Value;
            }
        }

        return best;
    }
}
