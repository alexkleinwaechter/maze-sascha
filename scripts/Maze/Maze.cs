using System;
using System.Collections.Generic;

namespace Maze.Model;

/// <summary>
/// Rechteckiges Zellgitter mit Waenden zwischen Nachbarn.
/// Reine Datenstruktur - kennt weder Godot noch Rendering.
/// </summary>
public sealed class Maze
{
    public int Width { get; }
    public int Height { get; }

    private readonly Cell[,] _cells;

    public Maze(int width, int height)
    {
        if (width < 2) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 2) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        _cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            _cells[x, y] = new Cell(x, y);
    }

    public Cell GetCell(int x, int y) => _cells[x, y];

    public bool IsInside(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>
    /// Liefert den Nachbarn in eine Richtung - oder null, wenn er ausserhalb des Gitters liegt.
    /// </summary>
    public Cell GetNeighbor(Cell cell, Direction direction)
    {
        var (dx, dy) = DirectionHelper.Offset(direction);
        int nx = cell.X + dx;
        int ny = cell.Y + dy;
        return IsInside(nx, ny) ? _cells[nx, ny] : null;
    }

    /// <summary>
    /// Entfernt die Wand zwischen zwei Zellen auf BEIDEN Seiten.
    /// </summary>
    public void RemoveWallBetween(Cell from, Direction toNeighbor)
    {
        Cell neighbor = GetNeighbor(from, toNeighbor);
        if (neighbor is null)
            throw new InvalidOperationException("Kein Nachbar in dieser Richtung.");

        from.RemoveWall(toNeighbor);
        neighbor.RemoveWall(DirectionHelper.Opposite(toNeighbor));
    }

    /// <summary>
    /// Iteriert alle Zellen zeilenweise. Praktisch fuer Renderer und Solver.
    /// </summary>
    public IEnumerable<Cell> AllCells()
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            yield return _cells[x, y];
    }

    /// <summary>
    /// Setzt alle CellState/Distance auf Default zurueck. Wird vor jedem Solver-Lauf aufgerufen.
    /// Waende bleiben erhalten.
    /// </summary>
    public void ResetSolverState()
    {
        foreach (var cell in AllCells())
        {
            cell.State = CellState.Open;
            cell.Distance = -1;
        }
    }
}
