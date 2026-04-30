using System.Collections.Generic;
using Godot;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// 2D-Visualisierung des Labyrinths. Liest <see cref="Maze.Model.Maze"/> und zeichnet
/// jede Zelle als gefuelltes Rechteck (Farbe nach Zustand) plus Waende als Linien.
/// </summary>
public partial class MazeView2D : Node2D
{
    [Export] public int CellSizePx = 24;
    [Export] public int WallThicknessPx = 2;
    [Export] public bool ShowDistances = false;

    // Farb-Map fuer Zellzustaende. Public statisch, damit weitere UI-Teile spaeter
    // dieselben Farben fuer Legenden wiederverwenden koennen.
    public static readonly Dictionary<CellState, Color> StateColors = new()
    {
        { CellState.Untouched, new Color("#1e1e1e") },
        { CellState.Carving,   new Color("#ffaa00") },
        { CellState.Open,      new Color("#2c2c2c") },
        { CellState.Frontier,  new Color("#8ab4f8") },
        { CellState.Visited,   new Color("#3d5a80") },
        { CellState.Path,      new Color("#f6c177") },
        { CellState.Start,     new Color("#a3be8c") },
        { CellState.Goal,      new Color("#bf616a") },
        { CellState.Filled,    new Color("#000000") }
    };

    private static readonly Color WallColor = new("#dcdcdc");
    private static readonly Color HeatmapMin = new("#003366");
    private static readonly Color HeatmapMax = new("#ff6f3c");

    // Ab dieser Maze-Groesse pro Achse schaltet die View vom Pro-Schritt-Refresh
    // auf zeitbasiertes Throttling um. Animation bleibt fluessig, aber die Anzahl
    // der Neuzeichnungen ist von der Schrittfrequenz entkoppelt.
    private const int ThrottleThreshold = 250;
    private const double ThrottledRefreshHz = 30.0;
    private const double ThrottledRefreshPeriod = 1.0 / ThrottledRefreshHz;

    private bool _refreshDirty;
    private double _refreshAccumulator;

    private Maze.Model.Maze _maze = null!;
    private CameraController2D _camera = null!;

    public override void _Ready()
    {
        _camera = GetNode<CameraController2D>("Camera2D");
    }

    /// <summary>Setzt das aktuelle Maze und loest Neuzeichnung aus.</summary>
    public void SetMaze(Maze.Model.Maze maze)
    {
        _maze = maze;
        QueueRedraw();
        _camera.FitToMaze(maze);
    }

    /// <summary>
    /// Wird nach jedem Algorithmus-Schritt aufgerufen. Bei kleinen Mazes loest die
    /// Methode sofort eine Neuzeichnung aus, bei grossen Mazes nur ein Dirty-Flag,
    /// das im naechsten _Process zeitbasiert eingeloest wird.
    /// </summary>
    public void Refresh()
    {
        if (_maze == null) return;
        if (_maze.Width <= ThrottleThreshold && _maze.Height <= ThrottleThreshold)
            QueueRedraw();
        else
            _refreshDirty = true;
    }

    /// <summary>
    /// Erzwingt eine sofortige Neuzeichnung, unabhaengig vom Throttling.
    /// Wird am Ende von Generierung/Loesung verwendet, damit der Endzustand
    /// sicher sichtbar ist.
    /// </summary>
    public void ForceRefresh()
    {
        _refreshDirty = false;
        _refreshAccumulator = 0;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_refreshDirty) return;
        _refreshAccumulator += delta;
        if (_refreshAccumulator >= ThrottledRefreshPeriod)
        {
            // Periode abziehen (statt auf 0 setzen), damit die fraktionale Restzeit
            // erhalten bleibt - sonst driftet die Refresh-Rate bei variablen Frameraten.
            _refreshAccumulator -= ThrottledRefreshPeriod;
            _refreshDirty = false;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_maze == null)
            return;

        // ---- Zellfuellungen ----
        int maxDistance = ComputeMaxDistance(_maze);

        foreach (var cell in _maze.AllCells())
        {
            var rect = new Rect2(
                cell.X * CellSizePx,
                cell.Y * CellSizePx,
                CellSizePx,
                CellSizePx
            );

            Color fill = ShowDistances && cell.Distance >= 0
                ? Heatmap(cell.Distance, maxDistance)
                : StateColors[cell.State];

            DrawRect(rect, fill, filled: true);
        }

        // ---- Waende als Linien ----
        foreach (var cell in _maze.AllCells())
        {
            float x0 = cell.X * CellSizePx;
            float y0 = cell.Y * CellSizePx;
            float x1 = x0 + CellSizePx;
            float y1 = y0 + CellSizePx;

            if (cell.HasWall(Direction.North))
                DrawLine(new Vector2(x0, y0), new Vector2(x1, y0), WallColor, WallThicknessPx);
            if (cell.HasWall(Direction.West))
                DrawLine(new Vector2(x0, y0), new Vector2(x0, y1), WallColor, WallThicknessPx);

            // Sued- und Ostwaende werden nur am Rand gezeichnet, sonst doppelt.
            if (cell.Y == _maze.Height - 1 && cell.HasWall(Direction.South))
                DrawLine(new Vector2(x0, y1), new Vector2(x1, y1), WallColor, WallThicknessPx);
            if (cell.X == _maze.Width - 1 && cell.HasWall(Direction.East))
                DrawLine(new Vector2(x1, y0), new Vector2(x1, y1), WallColor, WallThicknessPx);
        }
    }

    private static int ComputeMaxDistance(Maze.Model.Maze maze)
    {
        int max = 0;
        foreach (var c in maze.AllCells())
            if (c.Distance > max) max = c.Distance;
        return max;
    }

    private static Color Heatmap(int distance, int maxDistance)
    {
        if (maxDistance <= 0) return HeatmapMin;
        float t = (float)distance / maxDistance;
        return HeatmapMin.Lerp(HeatmapMax, t);
    }
}
