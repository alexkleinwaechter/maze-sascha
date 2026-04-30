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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            // RMB druecken/loslassen schaltet den Drag-Modus.
            if (mb.ButtonIndex == MouseButton.Right)
            {
                _isPanning = mb.Pressed;
                return;
            }

            // Mausrad zoomt mit Mausposition als Pivot, sodass der Punkt unter dem Cursor stationaer bleibt.
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
            {
                float step = ZoomStep;
                if (Input.IsPhysicalKeyPressed(Key.Shift))
                    step = ZoomSprintMultiplier;

                float factor = mb.ButtonIndex == MouseButton.WheelUp ? step : 1f / step;

                // Pivot-Math: Welt-Mausposition vor dem Zoom merken, Zoom anwenden,
                // dann Position so verschieben, dass die Welt-Mausposition gleich bleibt.
                Vector2 mouseWorldBefore = GetGlobalMousePosition();
                Vector2 newZoom = Zoom * factor;
                newZoom.X = Mathf.Clamp(newZoom.X, MinZoom, MaxZoom);
                newZoom.Y = Mathf.Clamp(newZoom.Y, MinZoom, MaxZoom);
                Zoom = newZoom;
                Vector2 mouseWorldAfter = GetGlobalMousePosition();
                Position += mouseWorldBefore - mouseWorldAfter;
                return;
            }
        }

        // Mausbewegung im Drag-Modus pannt entgegengesetzt zur Mausbewegung.
        // Geteilt durch Zoom: 1 Pixel Mausbewegung == 1 Pixel sichtbare Verschiebung.
        if (@event is InputEventMouseMotion motion && _isPanning)
        {
            Position -= motion.Relative / Zoom;
        }
    }

    public override void _Notification(int what)
    {
        // Wenn das Fenster den Fokus verliert, den Drag-Modus zuruecksetzen,
        // damit der Cursor sich nicht "festhaelt".
        if (what == NotificationApplicationFocusOut)
            _isPanning = false;
    }
}
