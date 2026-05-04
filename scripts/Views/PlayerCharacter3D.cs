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

    // ---- Modus-Enum (Phase 17) ----
    public enum Mode
    {
        Idle,
        FollowingPath,
        Manual
    }

    public Mode CurrentMode { get; private set; } = Mode.Idle;

    // ---- Felder fuer den FollowingPath-Modus ----
    private readonly List<Vector3> _waypoints = new();
    private int _currentIndex;
    private bool _isMoving;
    private float _cellSize = 1f;

    // ---- Felder fuer den Manual-Modus (Phase 17) ----
    private Model.Maze _manualMaze;
    private Cell _manualCell;
    private Cell _manualGoal;

    // Cell-Lerp-Zustand: nur eine Zelle pro Tastendruck.
    private bool _isAnimatingCell;
    private Vector3 _animFrom;
    private Vector3 _animTo;
    private float _animElapsed;
    private float _animDuration;

    /// <summary>
    /// Beendet jede laufende Animation und versteckt die Figur.
    /// Wird gerufen, wenn ein neues Maze gebaut wird, bevor der naechste Solver startet.
    /// </summary>
    public new void Hide()
    {
        _waypoints.Clear();
        _isMoving = false;
        _manualMaze = null;
        _manualCell = null;
        _manualGoal = null;
        _isAnimatingCell = false;
        CurrentMode = Mode.Idle;
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
            CurrentMode = Mode.Idle;
            return;
        }

        Position = _waypoints[0];
        Visible = true;
        _currentIndex = 1;
        _isMoving = _waypoints.Count > 1;
        CurrentMode = Mode.FollowingPath;
    }

    /// <summary>
    /// Aktiviert den Manual-Modus. Die Figur springt an die Startzelle und reagiert
    /// ab sofort auf WASD-Eingaben in <see cref="_Process"/>. <paramref name="goal"/>
    /// wird beim Erreichen mit dem GoalReached-Signal quittiert.
    /// </summary>
    public void EnableManualMode(Model.Maze maze, Cell start, Cell goal, float cellSize)
    {
        _cellSize = cellSize;
        _manualMaze = maze;
        _manualCell = start;
        _manualGoal = goal;
        _isAnimatingCell = false;
        _waypoints.Clear();
        _isMoving = false;

        Position = CellToWorld(start);
        Visible = true;
        CurrentMode = Mode.Manual;
    }

    /// <summary>Beendet den Manual-Modus und versteckt die Figur.</summary>
    public void DisableManualMode()
    {
        _manualMaze = null;
        _manualCell = null;
        _manualGoal = null;
        _isAnimatingCell = false;
        Visible = false;
        CurrentMode = Mode.Idle;
    }

    public override void _Process(double delta)
    {
        switch (CurrentMode)
        {
            case Mode.FollowingPath:
                ProcessFollowPath(delta);
                break;
            case Mode.Manual:
                ProcessManual(delta);
                break;
        }
    }

    private void ProcessFollowPath(double delta)
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
                CurrentMode = Mode.Idle;
                EmitSignal(SignalName.GoalReached);
            }
        }
        else
        {
            Position += toTarget.Normalized() * step;
        }
    }

    private void ProcessManual(double delta)
    {
        if (_isAnimatingCell)
        {
            _animElapsed += (float)delta;
            float t = Mathf.Clamp(_animElapsed / _animDuration, 0f, 1f);
            Position = _animFrom.Lerp(_animTo, t);

            if (t >= 1f)
            {
                _isAnimatingCell = false;
                Position = _animTo;
                // Sieg pruefen: Ist die aktuelle Zelle das Ziel?
                if (_manualCell == _manualGoal)
                    EmitSignal(SignalName.GoalReached);
            }
            return;
        }

        // Eingabe in eine Welt-Richtung aus Kamerasicht umrechnen und dann auf die
        // vier Maze-Richtungen quantisieren (North/South/West/East).
        Direction? dir = GetManualDirectionFromView();
        if (dir is null)
            return;

        // Wandkollision pruefen: HasWall == true bedeutet, die Wand ist noch vorhanden.
        if (_manualCell.HasWall(dir.Value))
            return;

        Cell next = _manualMaze.GetNeighbor(_manualCell, dir.Value);
        if (next == null) return;

        // Animation starten. Dauer = 1 / MoveSpeed (Sekunden pro Zelle).
        _animFrom = Position;
        _animTo = CellToWorld(next);
        _animElapsed = 0f;
        _animDuration = 1f / Mathf.Max(0.5f, MoveSpeed);
        _isAnimatingCell = true;
        _manualCell = next;
    }

    private Direction? GetManualDirectionFromView()
    {
        // Vorrang bleibt deterministisch wie zuvor: W > S > A > D.
        Camera3D camera = GetViewport().GetCamera3D();
        Vector3 forward = GetPlanarForward(camera);
        Vector3 right = GetPlanarRight(camera);

        Vector3 inputWorld;
        if (Input.IsPhysicalKeyPressed(Key.W)) inputWorld = forward;
        else if (Input.IsPhysicalKeyPressed(Key.S)) inputWorld = -forward;
        else if (Input.IsPhysicalKeyPressed(Key.A)) inputWorld = -right;
        else if (Input.IsPhysicalKeyPressed(Key.D)) inputWorld = right;
        else return null;

        return QuantizeWorldDirectionToMaze(inputWorld);
    }

    private static Vector3 GetPlanarForward(Camera3D camera)
    {
        if (camera == null)
            return Vector3.Forward;

        Vector3 forward = -camera.GlobalTransform.Basis.Z;
        forward.Y = 0f;
        return forward.LengthSquared() < 0.0001f ? Vector3.Forward : forward.Normalized();
    }

    private static Vector3 GetPlanarRight(Camera3D camera)
    {
        if (camera == null)
            return Vector3.Right;

        Vector3 right = camera.GlobalTransform.Basis.X;
        right.Y = 0f;
        return right.LengthSquared() < 0.0001f ? Vector3.Right : right.Normalized();
    }

    private static Direction QuantizeWorldDirectionToMaze(Vector3 direction)
    {
        // Godot-Weltachsen fuer das Grid:
        //   North = -Z, South = +Z, West = -X, East = +X.
        float northScore = direction.Dot(Vector3.Forward);
        float southScore = direction.Dot(Vector3.Back);
        float westScore = direction.Dot(Vector3.Left);
        float eastScore = direction.Dot(Vector3.Right);

        Direction best = Direction.North;
        float bestScore = northScore;

        if (southScore > bestScore)
        {
            best = Direction.South;
            bestScore = southScore;
        }
        if (westScore > bestScore)
        {
            best = Direction.West;
            bestScore = westScore;
        }
        if (eastScore > bestScore)
            best = Direction.East;

        return best;
    }

    /// <summary>Konvertiert Grid-Koordinaten in Welt-Koordinaten gemaess MazeView3D-Konvention.</summary>
    private Vector3 CellToWorld(Cell cell) =>
        new(cell.X * _cellSize + _cellSize / 2f, StandHeight, cell.Y * _cellSize + _cellSize / 2f);
}
