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

    public override void _Process(double delta)
    {
        HandleMovement(delta);
        HandleKeyboardLook(delta);
        ApplyRotation();
    }

    // Hinweis: Vector3.Forward ist in Godot (0, 0, -1), deshalb erhoeht
    // W -> Vector3.Forward die lokale -Z-Position via Translate korrekt = vorwaerts.
    private void HandleMovement(double delta)
    {
        // Eingabe als 3D-Richtungsvektor aufbauen:
        //   Vorwaerts/zurueck (W/S)  -> lokale -Z/+Z (forward/back)
        //   Seitwaerts (A/D)         -> lokale -X/+X (left/right)
        //   Vertikal (Q/E)            -> Welt-Y (E hoch, Q runter)
        Vector3 input = Vector3.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W)) input += Vector3.Forward;
        if (Input.IsPhysicalKeyPressed(Key.S)) input += Vector3.Back;
        if (Input.IsPhysicalKeyPressed(Key.A)) input += Vector3.Left;
        if (Input.IsPhysicalKeyPressed(Key.D)) input += Vector3.Right;

        // QE bewegen sich entlang Welt-Y, unabhaengig vom Pitch der Kamera.
        Vector3 worldVertical = Vector3.Zero;
        if (Input.IsPhysicalKeyPressed(Key.E)) worldVertical += Vector3.Up;
        if (Input.IsPhysicalKeyPressed(Key.Q)) worldVertical += Vector3.Down;

        if (input == Vector3.Zero && worldVertical == Vector3.Zero)
            return;

        float speed = MoveSpeed;
        if (Input.IsPhysicalKeyPressed(Key.Shift))
            speed *= SprintMultiplier;

        // Horizontaler Anteil: in Kamera-Lokalkoordinaten transformieren.
        if (input != Vector3.Zero)
        {
            input = input.Normalized();
            Translate(input * speed * (float)delta);
        }

        // Vertikaler Anteil: direkt in Weltkoordinaten addieren.
        if (worldVertical != Vector3.Zero)
            Position += worldVertical.Normalized() * speed * (float)delta;
    }

    private void HandleKeyboardLook(double delta)
    {
        // Pfeiltasten als Maus-Backup. Funktioniert immer, auch ohne RMB.
        float yawDelta = 0f;
        float pitchDelta = 0f;
        if (Input.IsPhysicalKeyPressed(Key.Left))  yawDelta   += KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Right)) yawDelta   -= KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Up))    pitchDelta += KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Down))  pitchDelta -= KeyTurnSpeed * (float)delta;
        _yaw += yawDelta;
        _pitch = Mathf.Clamp(_pitch + pitchDelta, -1.4f, 1.4f);
    }

    private void ApplyRotation()
    {
        // Rotation immer komplett aus Yaw/Pitch aufbauen, nicht inkrementell.
        Basis = Basis.FromEuler(new Vector3(_pitch, _yaw, 0));
    }
}
