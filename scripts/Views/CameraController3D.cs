using Godot;

namespace Maze.Views;

/// <summary>
/// Frei steuerbare 3D-Kamera fuer die Maze-Ansicht.
/// Bewegung: WASD horizontal in Blickrichtung, QE vertikal in Welt-Y,
/// Shift verdoppelt die Geschwindigkeit. Drehung: RMB + Maus oder Pfeiltasten.
/// Zoom: Mausrad als Dolly entlang der Blickrichtung.
/// </summary>
public partial class CameraController3D : Camera3D
{
    [Export] public float MoveSpeed = 8f;
    [Export] public float SprintMultiplier = 2f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float KeyTurnSpeed = 1.5f;        // rad/s fuer Pfeiltasten
    [Export] public float ZoomStep = 1.5f;
    [Export] public float ZoomSprintMultiplier = 3f;

    // Yaw/Pitch werden separat gefuehrt, damit die Rotation immer aus
    // Basis.FromEuler aufgebaut wird (kein Gimbal-Drift).
    private float _yaw;
    private float _pitch;
    private bool _mouseLook;

    public override void _Ready()
    {
        // Initialwerte aus dem aktuellen Transform ziehen, damit auch ohne FitToMaze
        // ein konsistenter Startzustand existiert.
        Vector3 euler = Basis.GetEuler();
        _pitch = euler.X;
        _yaw = euler.Y;
    }
}
