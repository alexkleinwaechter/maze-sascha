using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Gameplay;

/// <summary>
/// Pfadwahl-Helper fuer Guards. Liefert den naechsten Cell-Schritt zu einem Ziel via BFS.
/// Pure Funktionen - kein Godot, kein State.
/// </summary>
public static class GuardNavigator
{
    /// <summary>
    /// Naechster offener Nachbar von <paramref name="from"/> in Richtung <paramref name="goal"/>.
    /// Liefert null, wenn kein Pfad existiert (oder from == goal).
    /// Cooldowns/Repath-Limits stehen ausserhalb (im Director).
    /// </summary>
    public static Cell NextStepTowards(Model.Maze maze, Cell from, Cell goal)
    {
        if (maze is null || from is null || goal is null) return null;
        if (from == goal) return null;

        // BFS bis goal gefunden, dann Predecessor-Chain rueckwaerts laufen.
        var queue = new Queue<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var seen = new HashSet<Cell> { from };
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal) break;

            foreach (var dir in DirectionHelper.All)
            {
                if (current.HasWall(dir)) continue;
                var next = maze.GetNeighbor(current, dir);
                if (next == null || seen.Contains(next)) continue;
                seen.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal)) return null;

        Cell step = goal;
        while (cameFrom[step] != from)
            step = cameFrom[step];
        return step;
    }

    /// <summary>Random offener Nachbar als Fallback. Liefert null, wenn die Zelle isoliert ist.</summary>
    public static Cell RandomOpenNeighbor(Model.Maze maze, Cell from, Random random, Cell avoid = null)
    {
        if (maze is null || from is null) return null;
        random ??= new Random();
        var open = new List<Cell>(4);
        foreach (var dir in DirectionHelper.All)
        {
            if (from.HasWall(dir)) continue;
            var next = maze.GetNeighbor(from, dir);
            if (next == null) continue;
            if (avoid != null && next == avoid) continue;
            open.Add(next);
        }
        if (open.Count == 0)
        {
            // Wenn nur "avoid" verfuegbar war: dann doch nehmen, sonst stehen wir.
            if (avoid != null)
            {
                foreach (var dir in DirectionHelper.All)
                {
                    if (from.HasWall(dir)) continue;
                    var next = maze.GetNeighbor(from, dir);
                    if (next != null) return next;
                }
            }
            return null;
        }
        return open[random.Next(open.Count)];
    }

    /// <summary>Cardinal-Direction von <paramref name="from"/> nach <paramref name="to"/> oder null bei Nicht-Nachbar.</summary>
    public static Direction? DirectionTo(Cell from, Cell to)
    {
        if (from == null || to == null) return null;
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        if (Math.Abs(dx) + Math.Abs(dy) != 1) return null;
        if (dx == 1) return Direction.East;
        if (dx == -1) return Direction.West;
        if (dy == 1) return Direction.South;
        return Direction.North;
    }

    /// <summary>Manhattan-Distanz auf dem Grid (ohne Wand-Beruecksichtigung).</summary>
    public static int Manhattan(Cell a, Cell b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
