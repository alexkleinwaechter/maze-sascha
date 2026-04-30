using Godot;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// Frei steuerbare 2D-Kamera fuer die Maze-Ansicht.
/// Bewegung: WASD/Pfeiltasten pannen die Kamera, Shift verdoppelt das Tempo.
/// Drag: rechte Maustaste gedrueckt halten und Maus bewegen.
/// Zoom: Mausrad mit Mausposition als Pivot - der Punkt unter dem Cursor bleibt stehen.
/// </summary>
public partial class CameraController2D : Camera2D
{
    [Export] public float PanSpeed = 800f;             // Welt-px pro Sekunde
    [Export] public float SprintMultiplier = 2f;
    [Export] public float ZoomStep = 1.1f;             // multiplikativ pro Mausrad-Tick
    [Export] public float ZoomSprintMultiplier = 1.3f; // Shift+Wheel macht groessere Spruenge
    [Export] public float MinZoom = 0.01f;
    [Export] public float MaxZoom = 5.0f;

    private bool _isPanning;

    public override void _Ready()
    {
        // Diese Kamera uebernimmt das Viewport-Rendering der 2D-Ansicht.
        MakeCurrent();
    }

    public override void _Process(double delta)
    {
        HandlePan(delta);
    }

    private void HandlePan(double delta)
    {
        // WASD und Pfeiltasten bauen denselben 2D-Richtungsvektor.
        // W/Pfeil-hoch = nach oben (-Y in Godots 2D-Welt), S/runter = +Y, A/links = -X, D/rechts = +X.
        Vector2 input = Vector2.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up))    input += Vector2.Up;
        if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down))  input += Vector2.Down;
        if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left))  input += Vector2.Left;
        if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right)) input += Vector2.Right;

        if (input == Vector2.Zero)
            return;

        float speed = PanSpeed;
        if (Input.IsPhysicalKeyPressed(Key.Shift))
            speed *= SprintMultiplier;

        // Geteilt durch Zoom: bei stark gezoomter Ansicht reicht eine kleinere Welt-Bewegung
        // fuer dieselbe sichtbare Strecke, sodass das Pan-Tempo subjektiv konstant bleibt.
        Position += input.Normalized() * speed * (float)delta / Zoom.X;
    }
}
