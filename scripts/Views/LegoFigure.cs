using Godot;

namespace Maze.Views;

/// <summary>
/// Sechs-Quader-Spielfigur fuer Phase 19.
/// Wurzel-Y = 0 entspricht Fuessen auf dem Boden; die Figur ist 32 Einheiten hoch in
/// "Pixel-Koordinaten" und wird ueber den Skalierungsknoten in MazeView3D.tscn verkleinert.
///
/// Pivot-Hierarchie (alle als <see cref="Node3D"/>):
///   LegoFigure (root, feet at Y=0)
///   └─ Hip (Y=12) — entspricht der hier definierten Hueft-Ankerposition
///      ├─ BodyMesh
///      ├─ HeadPivot (am Hals, fuer Kopfbob/Drehung)
///      │  └─ HeadMesh
///      ├─ LeftShoulder (Schulter-Pivot fuer Arm-Schwung)
///      │  └─ LeftArmMesh
///      ├─ RightShoulder
///      │  └─ RightArmMesh
///      ├─ LeftHip (Hueft-Pivot fuer Bein-Schwung)
///      │  └─ LeftLegMesh
///      └─ RightHip
///         └─ RightLegMesh
/// </summary>
public partial class LegoFigure : Node3D
{
    [Export] public Texture2D AtlasTexture;

    // ---- Animations-Parameter (Phase 19.4) ----
    [Export] public float WalkSpeedScale = 8f;   // wie schnell der Phasen-Akkumulator laeuft
    [Export] public float HeadTurn = 0f;         // optionaler Yaw, wird von aussen gesetzt

    // ---- Pivot-Knoten (werden in _Ready gebaut) ----
    public Node3D HeadPivot { get; private set; }
    public Node3D LeftShoulder { get; private set; }
    public Node3D RightShoulder { get; private set; }
    public Node3D LeftHip { get; private set; }
    public Node3D RightHip { get; private set; }

    private float _walkPhase;
    private bool _isWalking;

    public override void _Ready()
    {
        var material = new StandardMaterial3D
        {
            AlbedoTexture = AtlasTexture,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
        };

        var hip = new Node3D { Name = "Hip", Position = new Vector3(0, 12, 0) };
        AddChild(hip);

        // Body: zentriert in X (-4), vorne an Z-Mitte (-2).
        AddCuboid(hip, "BodyMesh", new Vector3(-4, 0, -2),
            8, 12, 4, BodyUvs(), material);

        // Kopf: Pivot bei Hals-Mitte (leicht in Z verschoben, damit der Kopf schoen sitzt).
        HeadPivot = new Node3D { Name = "HeadPivot", Position = new Vector3(0, 12, 2) };
        hip.AddChild(HeadPivot);
        AddCuboid(HeadPivot, "HeadMesh", new Vector3(-4, 0, -4),
            8, 8, 8, HeadUvs(), material);

        // Schultern: links und rechts neben dem Body, auf Hoehe 10 (knapp unter Body-Top).
        LeftShoulder = new Node3D { Name = "LeftShoulder", Position = new Vector3(-6, 10, 0) };
        hip.AddChild(LeftShoulder);
        AddCuboid(LeftShoulder, "LeftArmMesh", new Vector3(-2, -10, -2),
            4, 12, 4, ArmLeftUvs(), material);

        RightShoulder = new Node3D { Name = "RightShoulder", Position = new Vector3(6, 10, 0) };
        hip.AddChild(RightShoulder);
        AddCuboid(RightShoulder, "RightArmMesh", new Vector3(-2, -10, -2),
            4, 12, 4, ArmRightUvs(), material);

        // Hueftgelenke: links und rechts unter dem Body, Pivot oben am Bein.
        LeftHip = new Node3D { Name = "LeftHip", Position = new Vector3(-2, 0, 0) };
        hip.AddChild(LeftHip);
        AddCuboid(LeftHip, "LeftLegMesh", new Vector3(-2, -12, -2),
            4, 12, 4, LegLeftUvs(), material);

        RightHip = new Node3D { Name = "RightHip", Position = new Vector3(2, 0, 0) };
        hip.AddChild(RightHip);
        AddCuboid(RightHip, "RightLegMesh", new Vector3(-2, -12, -2),
            4, 12, 4, LegRightUvs(), material);
    }

    private static void AddCuboid(Node3D parent, string name, Vector3 meshOffset,
        float w, float h, float d, TexturedCuboid.FaceUvs uvs, Material material)
    {
        var mesh = TexturedCuboid.Build(w, h, d, uvs);
        var instance = new MeshInstance3D
        {
            Name     = name,
            Mesh     = mesh,
            Position = meshOffset,
            MaterialOverride = material,
        };
        parent.AddChild(instance);
    }

    // ---- UV-Rechtecke fuer den 64x32-Atlas assets/devedse.png ----
    private static TexturedCuboid.FaceUvs HeadUvs() => new(
        Front:  new(8,  8, 8, 8),
        Right:  new(16, 8, 8, 8),
        Rear:   new(24, 8, 8, 8),
        Left:   new(0,  8, 8, 8),
        Top:    new(8,  0, 8, 8),
        Bottom: new(16, 0, 8, 8));

    private static TexturedCuboid.FaceUvs BodyUvs() => new(
        Front:  new(20, 20, 8, 12),
        Right:  new(28, 20, 4, 12),
        Rear:   new(32, 20, 8, 12),
        Left:   new(16, 20, 4, 12),
        Top:    new(20, 16, 8,  4),
        Bottom: new(28, 16, 8,  4));

    private static TexturedCuboid.FaceUvs ArmLeftUvs() => new(
        Front:  new(44, 20, 4, 12),
        Right:  new(48, 20, 4, 12),
        Rear:   new(52, 20, 4, 12),
        Left:   new(40, 20, 4, 12),
        Top:    new(44, 16, 4,  4),
        Bottom: new(48, 16, 4,  4));

    // Rechter Arm: gespiegelte UVs ueber negative Width im Rectangle.
    private static TexturedCuboid.FaceUvs ArmRightUvs() => new(
        Front:  new(48, 20, -4, 12),
        Left:   new(52, 20, -4, 12),
        Rear:   new(56, 20, -4, 12),
        Right:  new(44, 20, -4, 12),
        Top:    new(48, 16, -4,  4),
        Bottom: new(52, 16, -4,  4));

    private static TexturedCuboid.FaceUvs LegLeftUvs() => new(
        Front:  new(4,  20, 4, 12),
        Right:  new(8,  20, 4, 12),
        Rear:   new(12, 20, 4, 12),
        Left:   new(0,  20, 4, 12),
        Top:    new(4,  16, 4,  4),
        Bottom: new(8,  16, 4,  4));

    private static TexturedCuboid.FaceUvs LegRightUvs() => new(
        Front:  new(8,  20, -4, 12),
        Left:   new(12, 20, -4, 12),
        Rear:   new(16, 20, -4, 12),
        Right:  new(4,  20, -4, 12),
        Top:    new(8,  16, -4,  4),
        Bottom: new(12, 16, -4,  4));

    // ---- Walk-Animation (Phase 19.4) ----

    /// <summary>
    /// Schaltet die Lauf-Animation an oder aus. Im Idle-Modus stehen Arme/Beine still.
    /// </summary>
    public void SetWalking(bool walking) => _isWalking = walking;

    public override void _Process(double delta)
    {
        if (_isWalking)
            _walkPhase += (float)delta * WalkSpeedScale;

        float v = _walkPhase;

        HeadPivot.Rotation = new Vector3(
            Mathf.Sin(v) / 10f,
            HeadTurn,
            0f);

        LeftShoulder.Rotation = new Vector3(
             Mathf.Sin(v * 5f / 8f) / 2f,
             0f,
             Mathf.Sin(v * 9f / 8f) / 8f - 1f / 8f);

        RightShoulder.Rotation = new Vector3(
             Mathf.Sin(v * 5f / 8f - Mathf.Pi) / 2f,
             0f,
             Mathf.Sin(v * 9f / 8f - Mathf.Pi) / 8f + 1f / 8f);

        LeftHip.Rotation = new Vector3(
             Mathf.Sin(v * 7f / 8f),
             0f,
             0f);

        RightHip.Rotation = new Vector3(
             Mathf.Sin(v * 7f / 8f - Mathf.Pi),
             0f,
             0f);
    }
}
