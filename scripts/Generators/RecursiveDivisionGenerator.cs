using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Recursive Division: Teilt einen Bereich rekursiv mit einer Wand und genau einem Durchgang.
/// </summary>
public sealed class RecursiveDivisionGenerator : IMazeGenerator
{
    public string Id => "recursive-division";
    public string Name => "Recursive Division";

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        // Ausgangszustand: komplett offene Flaeche (nur Aussenrand bleibt geschlossen).
        for (int y = 0; y < maze.Height; y++)
        for (int x = 0; x < maze.Width; x++)
        {
            Cell cell = maze.GetCell(x, y);
            if (x < maze.Width - 1)
            {
                maze.RemoveWallBetween(cell, Direction.East);
            }

            if (y < maze.Height - 1)
            {
                maze.RemoveWallBetween(cell, Direction.South);
            }

            cell.State = CellState.Open;
        }

        yield return new GenerationStep(maze.GetCell(0, 0), null, null, CellState.Open, "Cleared");

        var work = new Stack<(int x, int y, int w, int h)>();
        work.Push((0, 0, maze.Width, maze.Height));

        while (work.Count > 0)
        {
            var (x, y, w, h) = work.Pop();
            if (w < 2 || h < 2)
            {
                continue;
            }

            bool horizontal = ChooseOrientation(w, h, random);

            if (horizontal)
            {
                int wallRow = y + 1 + random.Next(h - 1);
                int passage = x + random.Next(w);

                for (int cx = x; cx < x + w; cx++)
                {
                    if (cx == passage)
                    {
                        continue;
                    }

                    Cell upper = maze.GetCell(cx, wallRow - 1);
                    Cell lower = maze.GetCell(cx, wallRow);
                    upper.SetWall(Direction.South, true);
                    lower.SetWall(Direction.North, true);
                    yield return new GenerationStep(upper, lower, Direction.South, CellState.Open, "Wall");
                }

                work.Push((x, y, w, wallRow - y));
                work.Push((x, wallRow, w, h - (wallRow - y)));
            }
            else
            {
                int wallCol = x + 1 + random.Next(w - 1);
                int passage = y + random.Next(h);

                for (int cy = y; cy < y + h; cy++)
                {
                    if (cy == passage)
                    {
                        continue;
                    }

                    Cell left = maze.GetCell(wallCol - 1, cy);
                    Cell right = maze.GetCell(wallCol, cy);
                    left.SetWall(Direction.East, true);
                    right.SetWall(Direction.West, true);
                    yield return new GenerationStep(left, right, Direction.East, CellState.Open, "Wall");
                }

                work.Push((x, y, wallCol - x, h));
                work.Push((wallCol, y, w - (wallCol - x), h));
            }
        }
    }

    private static bool ChooseOrientation(int width, int height, System.Random random)
    {
        if (width < height)
        {
            return true;
        }

        if (height < width)
        {
            return false;
        }

        return random.Next(2) == 0;
    }
}
