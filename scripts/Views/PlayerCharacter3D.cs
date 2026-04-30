using System.Collections.Generic;
using Godot;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// Die im 3D-Maze sichtbare Spielfigur. Haelt eine Liste von Wegpunkten
/// (in Welt-Koordinaten) und interpoliert pro Frame zwischen ihnen.
///
/// Die Figur wird in Phase 16 nur passiv vom Solver-Bot benutzt; in Phase 17
/// kommt der Manual-Modus dazu, in Phase 18 die Lichtquelle als Kind.
/// </summary>
public partial class PlayerCharacter3D : Node3D
{
    [Signal] public delegate void GoalReachedEventHandler();

    /// <summary>Geschwindigkeit in Zellen pro Sekunde.</summary>
    [Export] public float MoveSpeed = 4f;

    /// <summary>Y-Anhebung der Figur. Capsule mit Hoehe 1.0 sitzt mit Mitte auf 0.5.</summary>
    [Export] public float StandHeight = 0.5f;

    private readonly List<Vector3> _waypoints = new();
    private int _currentIndex;
    private bool _isMoving;
    private float _cellSize = 1f;

    /// <summary>
    /// Beendet jede laufende Animation und versteckt die Figur.
    /// Wird gerufen, wenn ein neues Maze gebaut wird, bevor der naechste Solver startet.
    /// </summary>
    public new void Hide()
    {
        _waypoints.Clear();
        _isMoving = false;
        Visible = false;
    }

    /// <summary>
    /// Setzt die Figur an die Startzelle und beginnt, der uebergebenen Cell-Liste zu folgen.
    /// Die Liste muss bereits Start- und Zielzelle einschliessen und in
    /// Reihenfolge des Pfades sortiert sein.
    /// </summary>
    public void StartFollowingPath(List<Cell> path, float cellSize)
    {
        _cellSize = cellSize;
        _waypoints.Clear();
        foreach (var cell in path)
            _waypoints.Add(CellToWorld(cell));

        if (_waypoints.Count == 0)
        {
            Visible = false;
            _isMoving = false;
            return;
        }

        Position = _waypoints[0];
        Visible = true;
        _currentIndex = 1;
        _isMoving = _waypoints.Count > 1;
    }

    public override void _Process(double delta)
    {
        if (!_isMoving) return;

        Vector3 target = _waypoints[_currentIndex];
        Vector3 toTarget = target - Position;
        float remaining = toTarget.Length();
        float step = MoveSpeed * _cellSize * (float)delta;

        if (step >= remaining)
        {
            // Zielwegpunkt mit kleinem Restschritt erreicht; an naechsten Wegpunkt weiterruecken.
            Position = target;
            _currentIndex++;
            if (_currentIndex >= _waypoints.Count)
            {
                _isMoving = false;
                EmitSignal(SignalName.GoalReached);
            }
        }
        else
        {
            Position += toTarget.Normalized() * step;
        }
    }

    /// <summary>Konvertiert Grid-Koordinaten in Welt-Koordinaten gemaess MazeView3D-Konvention.</summary>
    private Vector3 CellToWorld(Cell cell) =>
        new(cell.X * _cellSize + _cellSize / 2f, StandHeight, cell.Y * _cellSize + _cellSize / 2f);
}
