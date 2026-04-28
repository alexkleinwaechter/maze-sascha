using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Cellular Automata-Generator nach Justin A. Parr (2018).
/// Erzeugt ein perfektes Spanning-Tree-Labyrinth per Zustandsautomat.
/// </summary>
public sealed class CellularAutomataGenerator : IMazeGenerator
{
    public string Id => "cellular-automata";
    public string Name => "Cellular Automata (Parr, true maze)";

    private const int BranchProbabilityPercent = 5;
    private const int TurnProbabilityPercent = 10;
    private const int SafetyMaxTicks = 1_000_000;

    private enum CaState : byte
    {
        Disconnected = 0,
        Seed = 1,
        Invite = 2,
        Connected = 3
    }

    private struct CellInfo
    {
        public CaState State;
        public Direction? ConnectVector;
        public Direction? InviteVector;
    }

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        int width = maze.Width;
        int height = maze.Height;

        var current = new CellInfo[width, height];
        var next = new CellInfo[width, height];

        int startX = random.Next(width);
        int startY = random.Next(height);
        current[startX, startY].State = CaState.Seed;

        Cell startCell = maze.GetCell(startX, startY);
        startCell.State = CellState.Carving;
        yield return new GenerationStep(startCell, null, null, CellState.Carving, "Initial seed");

        for (int tick = 0; tick < SafetyMaxTicks; tick++)
        {
            Array.Copy(current, next, current.Length);

            bool anyActive = AnyActive(current, width, height);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                CellInfo cellInfo = current[x, y];

                switch (cellInfo.State)
                {
                    case CaState.Disconnected:
                        foreach (var step in TryAcceptInvitation(maze, current, next, x, y))
                        {
                            yield return step;
                        }
                        break;

                    case CaState.Seed:
                        foreach (var step in RunSeed(maze, current, next, x, y, cellInfo, random))
                        {
                            yield return step;
                        }
                        break;

                    case CaState.Invite:
                        foreach (var step in RunInvite(maze, next, x, y, random))
                        {
                            yield return step;
                        }
                        break;

                    case CaState.Connected:
                        if (!anyActive)
                        {
                            foreach (var step in TryRevive(maze, current, next, x, y, random))
                            {
                                yield return step;
                            }
                        }
                        break;
                }
            }

            Array.Copy(next, current, next.Length);

            if (!AnyDisconnected(current, width, height) && !AnyActive(current, width, height))
            {
                yield break;
            }
        }
    }

    private static IEnumerable<GenerationStep> TryAcceptInvitation(
        Model.Maze maze,
        CellInfo[,] current,
        CellInfo[,] next,
        int x,
        int y)
    {
        foreach (var dir in DirectionHelper.All)
        {
            var (dx, dy) = DirectionHelper.Offset(dir);
            int nx = x + dx;
            int ny = y + dy;
            if (!maze.IsInside(nx, ny))
            {
                continue;
            }

            CellInfo neighbor = current[nx, ny];
            if (neighbor.State != CaState.Invite)
            {
                continue;
            }

            if (neighbor.InviteVector != DirectionHelper.Opposite(dir))
            {
                continue;
            }

            next[x, y].State = CaState.Seed;
            next[x, y].ConnectVector = dir;
            next[x, y].InviteVector = null;

            Cell me = maze.GetCell(x, y);
            Cell parent = maze.GetCell(nx, ny);
            maze.RemoveWallBetween(me, dir);
            me.State = CellState.Carving;

            yield return new GenerationStep(me, parent, dir, CellState.Carving, "Accept invite");
            yield break;
        }
    }

    private static IEnumerable<GenerationStep> RunSeed(
        Model.Maze maze,
        CellInfo[,] current,
        CellInfo[,] next,
        int x,
        int y,
        CellInfo cellInfo,
        System.Random random)
    {
        int neighborMask = BuildDisconnectedMask(maze, current, x, y);
        if (neighborMask == 0)
        {
            next[x, y].State = CaState.Connected;
            next[x, y].InviteVector = null;
            Cell cell = maze.GetCell(x, y);
            cell.State = CellState.Open;
            yield return new GenerationStep(cell, null, null, CellState.Open, "Seed dies");
            yield break;
        }

        Direction picked = ChooseDirection(neighborMask, cellInfo.ConnectVector, random);
        next[x, y].State = CaState.Invite;
        next[x, y].InviteVector = picked;
    }

    private static IEnumerable<GenerationStep> RunInvite(
        Model.Maze maze,
        CellInfo[,] next,
        int x,
        int y,
        System.Random random)
    {
        if (random.Next(100) < BranchProbabilityPercent)
        {
            next[x, y].State = CaState.Seed;
            next[x, y].InviteVector = null;
        }
        else
        {
            next[x, y].State = CaState.Connected;
            next[x, y].InviteVector = null;
            Cell cell = maze.GetCell(x, y);
            cell.State = CellState.Open;
            yield return new GenerationStep(cell, null, null, CellState.Open, "Connected");
        }
    }

    private static IEnumerable<GenerationStep> TryRevive(
        Model.Maze maze,
        CellInfo[,] current,
        CellInfo[,] next,
        int x,
        int y,
        System.Random random)
    {
        bool bordersDisconnected = false;
        foreach (var dir in DirectionHelper.All)
        {
            var (dx, dy) = DirectionHelper.Offset(dir);
            int nx = x + dx;
            int ny = y + dy;
            if (!maze.IsInside(nx, ny))
            {
                continue;
            }

            if (current[nx, ny].State == CaState.Disconnected)
            {
                bordersDisconnected = true;
                break;
            }
        }

        if (!bordersDisconnected)
        {
            yield break;
        }

        if (random.Next(100) >= BranchProbabilityPercent)
        {
            yield break;
        }

        next[x, y].State = CaState.Seed;
        Cell cell = maze.GetCell(x, y);
        cell.State = CellState.Carving;
        yield return new GenerationStep(cell, null, null, CellState.Carving, "Revive");
    }

    private static int BuildDisconnectedMask(Model.Maze maze, CellInfo[,] grid, int x, int y)
    {
        int mask = 0;
        foreach (var dir in DirectionHelper.All)
        {
            var (dx, dy) = DirectionHelper.Offset(dir);
            int nx = x + dx;
            int ny = y + dy;
            if (!maze.IsInside(nx, ny))
            {
                continue;
            }

            if (grid[nx, ny].State != CaState.Disconnected)
            {
                continue;
            }

            mask |= 1 << (int)dir;
        }

        return mask;
    }

    private static Direction ChooseDirection(int neighborMask, Direction? connectVector, System.Random random)
    {
        bool turnNow = random.Next(100) < TurnProbabilityPercent;

        if (!turnNow && connectVector.HasValue)
        {
            Direction straight = DirectionHelper.Opposite(connectVector.Value);
            if ((neighborMask & (1 << (int)straight)) != 0)
            {
                return straight;
            }
        }

        Span<Direction> available = stackalloc Direction[4];
        int count = 0;
        foreach (var dir in DirectionHelper.All)
        {
            if ((neighborMask & (1 << (int)dir)) != 0)
            {
                available[count++] = dir;
            }
        }

        return available[random.Next(count)];
    }

    private static bool AnyActive(CellInfo[,] grid, int width, int height)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            CaState state = grid[x, y].State;
            if (state == CaState.Seed || state == CaState.Invite)
            {
                return true;
            }
        }

        return false;
    }

    private static bool AnyDisconnected(CellInfo[,] grid, int width, int height)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y].State == CaState.Disconnected)
            {
                return true;
            }
        }

        return false;
    }
}
