using Godot;
using Maze.Gameplay;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// 3D-Visualisierung eines Guards: Mesh + Spotlight + Mode-Farbcode.
/// Reine View-Klasse; AI-Logik liegt im <see cref="GuardDirector"/>.
/// </summary>
public partial class GuardCharacter3D : Node3D
{
    [Export] public float StandHeight = 0.0f;

    private MeshInstance3D _bodyMesh = null!;
    private SpotLight3D _sightCone = null!;
    private StandardMaterial3D _bodyMaterial = null!;
    private float _cellSize = 1f;

    // Anim-Lerp pro Cell-Schritt.
    private bool _isAnimating;
    private Vector3 _animFrom;
    private Vector3 _animTo;
    private float _animElapsed;
    private float _animDuration;

    public override void _Ready()
    {
        BuildVisuals();
        Visible = false;
    }

    private void BuildVisuals()
    {
        _bodyMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.3f, 0.9f, 0.4f),
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.9f, 0.4f),
            EmissionEnergyMultiplier = 0.6f,
            Roughness = 0.4f
        };

        _bodyMesh = new MeshInstance3D
        {
            Name = "Body",
            Mesh = new CapsuleMesh
            {
                Radius = 0.22f,
                Height = 0.7f
            },
            Position = new Vector3(0f, 0.35f, 0f),
            MaterialOverride = _bodyMaterial
        };
        AddChild(_bodyMesh);

        _sightCone = new SpotLight3D
        {
            Name = "SightCone",
            LightColor = new Color(0.3f, 0.9f, 0.4f),
            LightEnergy = 1.4f,
            SpotRange = GuardPerception.DefaultRangeCells,
            SpotAngle = GuardPerception.HalfAngleDeg,
            SpotAttenuation = 0.6f,
            SpotAngleAttenuation = 0.5f,
            ShadowEnabled = false,
            // Default in Godot zeigt Spotlight entlang -Z (forward).
            // Wir richten unten ueber SetFacing aus.
            // Leicht nach unten geneigt (-15 Grad um X), damit der Kegel den Boden im
            // Nahbereich trifft - macht den Sichtkegel im First-Person als Lichtform sichtbar.
            Position = new Vector3(0f, 0.65f, 0f),
            RotationDegrees = new Vector3(-15f, 0f, 0f)
        };
        AddChild(_sightCone);
    }

    /// <summary>Setzt die Figur auf eine Zelle (ohne Animation).</summary>
    public void PlaceAtCell(Cell cell, float cellSize)
    {
        _cellSize = cellSize;
        Position = CellToWorld(cell);
        Visible = true;
        _isAnimating = false;
    }

    /// <summary>Startet eine Cell-Animation. Dauer in Sekunden.</summary>
    public void AnimateToCell(Cell target, float duration)
    {
        _animFrom = Position;
        _animTo = CellToWorld(target);
        _animElapsed = 0f;
        _animDuration = Mathf.Max(0.05f, duration);
        _isAnimating = true;
    }

    /// <summary>Setzt die Cardinal-Blickrichtung (animiert die Yaw-Rotation des Spotlights).</summary>
    public void SetFacing(Direction dir)
    {
        // Godot: -Z = forward. Mapping unserer Direction-Enum:
        //   North = -Z (yaw 0)
        //   East  = +X (yaw -90 grad bzw. +pi/2 um Y? -> getestet: yaw = -PI/2)
        //   South = +Z (yaw PI)
        //   West  = -X (yaw +PI/2)
        float yaw = dir switch
        {
            Direction.North => 0f,
            Direction.East => -Mathf.Pi / 2f,
            Direction.South => Mathf.Pi,
            Direction.West => Mathf.Pi / 2f,
            _ => 0f
        };
        // SpotLight3D blickt entlang -Z; wir rotieren den ganzen Guard (Body + Cone) um Y.
        Rotation = new Vector3(0f, yaw, 0f);
    }

    /// <summary>Setzt die Mode-Farbe fuer Body und Spotlight.</summary>
    public void SetModeColor(GuardMode mode)
    {
        Color color = mode switch
        {
            GuardMode.Patrol => new Color(0.30f, 0.90f, 0.40f), // gruen
            GuardMode.Alert => new Color(1.00f, 0.85f, 0.10f),  // gelb
            GuardMode.Chase => new Color(1.00f, 0.20f, 0.20f),  // rot
            GuardMode.Search => new Color(1.00f, 0.55f, 0.10f), // orange
            GuardMode.Return => new Color(0.30f, 0.55f, 1.00f), // blau
            _ => new Color(1f, 1f, 1f)
        };
        _bodyMaterial.AlbedoColor = color;
        _bodyMaterial.Emission = color;
        _sightCone.LightColor = color;
    }

    /// <summary>Hide + Animation stoppen.</summary>
    public new void Hide()
    {
        Visible = false;
        _isAnimating = false;
    }

    public override void _Process(double delta)
    {
        if (!_isAnimating) return;
        _animElapsed += (float)delta;
        float t = Mathf.Clamp(_animElapsed / _animDuration, 0f, 1f);
        Position = _animFrom.Lerp(_animTo, t);
        if (t >= 1f)
        {
            _isAnimating = false;
            Position = _animTo;
        }
    }

    private Vector3 CellToWorld(Cell cell) =>
        new(cell.X * _cellSize + _cellSize / 2f, StandHeight, cell.Y * _cellSize + _cellSize / 2f);
}
