using System;
using Maze.Model;

namespace Maze.Gameplay;

/// <summary>
/// Sicht-Logik fuer Guards. Pure Funktionen - kein Godot, kein State.
///
/// Zweistufig:
///  1) FOV-Test: Liegt der Spieler im Kegel +/- HalfAngleDeg um die Cardinal-FacingDirection?
///  2) LOS-Test: Sichtstrahl zellweise pruefen, Wand zwischen zwei Zellen blockiert.
///
/// Reichweite halbiert sich, wenn der Spieler schleicht.
///
/// Beispielfaelle (im Code als kommentierte Tests):
///   Korridor: Guard schaut Ost, Spieler 3 Zellen oestlich -> sichtbar.
///   Wand zwischen Guard und Spieler -> nicht sichtbar.
///   Spieler hinter Guard (West, FacingDirection=Ost) -> nicht sichtbar (FOV).
///   Spieler 1 Zelle westlich, FacingDirection=Ost, schleicht -> nicht sichtbar (Reichweite halbiert auf 4, Position OK aber FOV-fail).
/// </summary>
public static class GuardPerception
{
    /// <summary>Standard-Erkennungsreichweite in Zellen.</summary>
    public const float DefaultRangeCells = 8f;

    /// <summary>Halbwinkel des FOV-Kegels in Grad.</summary>
    public const float HalfAngleDeg = 70f;

    /// <summary>
    /// True, wenn der Guard den Spieler aktuell sehen kann.
    /// Spielerposition fuer den Sichttest: Quellzelle waehrend Cell-Animation.
    /// </summary>
    public static bool CanSee(
        Model.Maze maze,
        Cell guardCell,
        Direction guardFacing,
        Cell playerSightCell,
        bool playerIsSneaking,
        float baseRangeCells = DefaultRangeCells)
    {
        if (maze is null || guardCell is null || playerSightCell is null) return false;
        if (guardCell == playerSightCell) return true;

        float effectiveRange = playerIsSneaking ? baseRangeCells * 0.5f : baseRangeCells;

        int dx = playerSightCell.X - guardCell.X;
        int dy = playerSightCell.Y - guardCell.Y;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance > effectiveRange) return false;

        if (!IsInFov(guardFacing, dx, dy)) return false;

        return HasLineOfSight(maze, guardCell, playerSightCell);
    }

    /// <summary>
    /// FOV-Test: Vektor (dx,dy) liegt im Halbwinkel um Cardinal-FacingDirection.
    /// Halbwinkel = HalfAngleDeg.
    /// </summary>
    public static bool IsInFov(Direction facing, int dx, int dy)
    {
        if (dx == 0 && dy == 0) return true;
        var (fx, fy) = DirectionHelper.Offset(facing);

        // dot = cos(theta) * |v| * |f|; |f|=1 also dot/|v| = cos(theta).
        float length = MathF.Sqrt(dx * dx + dy * dy);
        float cosTheta = (dx * fx + dy * fy) / length;
        float cosLimit = MathF.Cos(HalfAngleDeg * MathF.PI / 180f);
        return cosTheta >= cosLimit;
    }

    /// <summary>
    /// Zellweiser LOS-Test ueber Bresenham-aehnliche Iteration.
    /// Wenn der Pfad eine Wand zwischen zwei aufeinanderfolgenden Zellen quert,
    /// blockiert sie die Sicht.
    /// </summary>
    public static bool HasLineOfSight(Model.Maze maze, Cell from, Cell to)
    {
        if (from == to) return true;

        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;

        // Sample-Punkte entlang der Linie. Schrittweite kleiner als 0.5 Zellen,
        // damit wir keinen Zelluebergang verpassen.
        int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)) * 2;
        if (steps == 0) return true;

        int prevX = x0, prevY = y0;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            int sx = (int)MathF.Round(x0 + (x1 - x0) * t);
            int sy = (int)MathF.Round(y0 + (y1 - y0) * t);

            if (sx == prevX && sy == prevY) continue;

            // Bestimme die Richtung von (prevX,prevY) zu (sx,sy). Nur Cardinal-Schritte zugelassen.
            int ddx = sx - prevX;
            int ddy = sy - prevY;

            // Wenn diagonale Bewegung gesampelt wird (sollte selten sein), splitten in zwei Cardinal-Schritte.
            if (ddx != 0 && ddy != 0)
            {
                // Versuch erst horizontaler Schritt, dann vertikal - beide Wege muessen frei sein.
                if (!StepBlocked(maze, prevX, prevY, ddx, 0))
                {
                    if (!StepBlocked(maze, prevX + ddx, prevY, 0, ddy))
                    {
                        prevX = sx; prevY = sy;
                        continue;
                    }
                }
                if (!StepBlocked(maze, prevX, prevY, 0, ddy))
                {
                    if (!StepBlocked(maze, prevX, prevY + ddy, ddx, 0))
                    {
                        prevX = sx; prevY = sy;
                        continue;
                    }
                }
                return false;
            }

            if (StepBlocked(maze, prevX, prevY, ddx, ddy)) return false;
            prevX = sx; prevY = sy;
        }
        return true;
    }

    /// <summary>True, wenn der Schritt von (x,y) um (ddx,ddy) durch eine Wand blockiert wird.</summary>
    private static bool StepBlocked(Model.Maze maze, int x, int y, int ddx, int ddy)
    {
        if (!maze.IsInside(x, y)) return true;
        if (Math.Abs(ddx) + Math.Abs(ddy) != 1) return true;

        Direction dir;
        if (ddx == 1) dir = Direction.East;
        else if (ddx == -1) dir = Direction.West;
        else if (ddy == 1) dir = Direction.South;
        else dir = Direction.North;

        var cell = maze.GetCell(x, y);
        return cell.HasWall(dir);
    }
}
