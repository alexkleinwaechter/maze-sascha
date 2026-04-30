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
}
