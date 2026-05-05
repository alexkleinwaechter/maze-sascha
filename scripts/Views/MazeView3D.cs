using Godot;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// 3D-Visualisierung des Labyrinths. Baut Boden und Waende aus Box-Meshes auf.
/// Fuer die Groessen im Schulprojekt reicht ein kompletter Neuaufbau bei Refresh.
/// </summary>
public partial class MazeView3D : Node3D
{
    [Export] public float CellSize = 1.0f;
    [Export] public float WallHeight = 1.4f;
    [Export] public float WallThickness = 0.1f;

    private Node3D _wallContainer = null!;
    private MeshInstance3D _floor = null!;
    private MultiMeshInstance3D _wallsHorizontal = null!;
    private MultiMeshInstance3D _wallsVertical = null!;
    private CameraController3D _camera = null!;
    private DirectionalLight3D _sun = null!;
    private OmniLight3D _playerLight = null!;
    private WorldEnvironment _worldEnv = null!;
    private Node3D _player = null!;
    private MultiMeshInstance3D _visitedPads = null!;
    private Model.Maze _maze = null!;
    private readonly HashSet<int> _visitedCellKeys = new();
    private int _visitedPadCount;

    private bool _exploreTarget;
    private float _exploreFactor;
    private const float ExploreLerpSpeed = 1.6f; // ~0.6s fuer 0->1

    private static readonly StandardMaterial3D WallMaterial = new()
    {
        AlbedoColor = new Color("#dcdcdc")
    };

    private static readonly StandardMaterial3D FloorMaterial = new()
    {
        AlbedoColor = new Color("#2c2c2c")
    };

    private static readonly StandardMaterial3D VisitedPadMaterial = new()
    {
        AlbedoColor = new Color(1.0f, 0.92f, 0.80f),
        EmissionEnabled = true,
        Emission = new Color(1.0f, 0.94f, 0.84f),
        EmissionEnergyMultiplier = 1.7f,
        Roughness = 0.2f,
        Metallic = 0.0f
    };

    // Start- und Ziel-Marker werden bei jedem Rebuild neu erstellt.
    private Node3D _startMarker;
    private Node3D _goalMarker;

    public override void _Ready()
    {
        _wallContainer = GetNode<Node3D>("WallContainer");
        _floor = GetNode<MeshInstance3D>("Floor");
        _wallsHorizontal = GetNode<MultiMeshInstance3D>("WallContainer/WallsHorizontal");
        _wallsVertical = GetNode<MultiMeshInstance3D>("WallContainer/WallsVertical");
        _camera = GetNode<CameraController3D>("Camera3D");
        _sun = GetNode<DirectionalLight3D>("Sun");
        _player = GetNode<Node3D>("Player");
        _playerLight = GetNode<OmniLight3D>("Player/PlayerLight");
        _worldEnv = GetNode<WorldEnvironment>("WorldEnvironment");

        BuildVisitedPadRenderer();

        // Material zuweisen - die in der .tscn voreingestellten BoxMeshes haben bewusst kein Material,
        // damit die Farbe zentral hier gesetzt werden kann.
        _wallsHorizontal.MaterialOverride = WallMaterial;
        _wallsVertical.MaterialOverride = WallMaterial;

        // BoxMesh-Groessen aus den [Export]-Werten neu setzen, damit die Wandgeometrie
        // den C#-Werten folgt - die in der .tscn voreingestellten Groessen sind nur
        // Editor-Platzhalter.
        ((BoxMesh)_wallsHorizontal.Multimesh.Mesh).Size = new Vector3(CellSize, WallHeight, WallThickness);
        ((BoxMesh)_wallsVertical.Multimesh.Mesh).Size = new Vector3(WallThickness, WallHeight, CellSize);

        ApplyExploreFactor(0f);
    }

    public override void _Process(double delta)
    {
        float target = _exploreTarget ? 1f : 0f;
        if (Mathf.IsEqualApprox(_exploreFactor, target))
        {
            UpdateVisitedTrail();
            return;
        }

        float lerpStep = ExploreLerpSpeed * (float)delta;
        _exploreFactor = Mathf.MoveToward(_exploreFactor, target, lerpStep);
        ApplyExploreFactor(_exploreFactor);
        UpdateVisitedTrail();
    }

    public void SetMaze(Model.Maze maze)
    {
        _maze = maze;
        Rebuild();
        _camera.FitToMaze(maze);
    }

    public void Refresh()
    {
        if (_maze != null)
            Rebuild();
    }

    private void Rebuild()
    {
        if (_maze == null)
            return;

        BuildFloor(_maze);
        BuildWalls(_maze);
        ClearVisitedTrail();
        PlaceMarkers(_maze);
    }

    private void BuildVisitedPadRenderer()
    {
        _visitedPads = new MultiMeshInstance3D
        {
            Name = "VisitedPads",
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = VisitedPadMaterial
        };

        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new CylinderMesh
            {
                TopRadius = CellSize * 0.22f,
                BottomRadius = CellSize * 0.22f,
                Height = 0.018f,
                RadialSegments = 18
            },
            InstanceCount = 0,
            VisibleInstanceCount = 0
        };

        _visitedPads.Multimesh = multimesh;
        AddChild(_visitedPads);
    }

    private void ClearVisitedTrail()
    {
        _visitedCellKeys.Clear();
        _visitedPadCount = 0;

        if (_visitedPads?.Multimesh == null)
            return;

        var mm = _visitedPads.Multimesh;
        int capacity = _maze == null ? 0 : _maze.Width * _maze.Height;
        if (mm.InstanceCount != capacity)
            mm.InstanceCount = capacity;

        mm.VisibleInstanceCount = 0;
    }

    private void UpdateVisitedTrail()
    {
        if (!Visible || _maze == null || _player == null || !_player.Visible)
            return;

        int cellX = Mathf.FloorToInt(_player.GlobalPosition.X / CellSize);
        int cellY = Mathf.FloorToInt(_player.GlobalPosition.Z / CellSize);
        if (!_maze.IsInside(cellX, cellY))
            return;

        int key = cellY * _maze.Width + cellX;
        if (_visitedCellKeys.Contains(key))
            return;

        var mm = _visitedPads.Multimesh;
        if (_visitedPadCount >= mm.InstanceCount)
            return;

        _visitedCellKeys.Add(key);
        mm.SetInstanceTransform(_visitedPadCount, new Transform3D(
            Basis.Identity,
            new Vector3(
                cellX * CellSize + CellSize * 0.5f,
                0.009f,
                cellY * CellSize + CellSize * 0.5f)));
        _visitedPadCount++;
        mm.VisibleInstanceCount = _visitedPadCount;
    }

    private void BuildFloor(Model.Maze maze)
    {
        Vector3 size = new(maze.Width * CellSize, 0.05f, maze.Height * CellSize);
        _floor.Mesh = new BoxMesh { Size = size };
        _floor.MaterialOverride = FloorMaterial;
        _floor.Position = new Vector3(maze.Width * CellSize / 2f, -0.025f, maze.Height * CellSize / 2f);
    }

    /// <summary>
    /// Schreibt fuer jede Wand des Mazes eine Transformations-Matrix in eines der zwei
    /// MultiMesh-Buckets (horizontal = Nord/Sued, vertikal = Ost/West). Beide MultiMeshes
    /// teilen sich jeweils ein BoxMesh; die GPU rendert alle Instanzen in einem Draw-Call.
    /// </summary>
    private void BuildWalls(Model.Maze maze)
    {
        // Maximalkapazitaet exakt dimensionieren: horizontale Waende = Width * (Height+1)
        // (Nord-Kanten aller Zellen plus die Sued-Randreihe), vertikale entsprechend
        // (Width+1) * Height.
        int maxHorizontal = maze.Width * (maze.Height + 1);
        int maxVertical = (maze.Width + 1) * maze.Height;

        var horizontal = _wallsHorizontal.Multimesh;
        var vertical = _wallsVertical.Multimesh;

        horizontal.InstanceCount = maxHorizontal;
        vertical.InstanceCount = maxVertical;

        int hi = 0;
        int vi = 0;

        for (int y = 0; y < maze.Height; y++)
        for (int x = 0; x < maze.Width; x++)
        {
            Cell cell = maze.GetCell(x, y);

            if (cell.HasWall(Direction.North))
                horizontal.SetInstanceTransform(hi++, HorizontalWallTransform(x * CellSize + CellSize / 2f, y * CellSize));

            if (cell.HasWall(Direction.West))
                vertical.SetInstanceTransform(vi++, VerticalWallTransform(x * CellSize, y * CellSize + CellSize / 2f));

            if (y == maze.Height - 1 && cell.HasWall(Direction.South))
                horizontal.SetInstanceTransform(hi++, HorizontalWallTransform(x * CellSize + CellSize / 2f, (y + 1) * CellSize));

            if (x == maze.Width - 1 && cell.HasWall(Direction.East))
                vertical.SetInstanceTransform(vi++, VerticalWallTransform((x + 1) * CellSize, y * CellSize + CellSize / 2f));
        }

        // VisibleInstanceCount sorgt dafuer, dass nur die tatsaechlich befuellten Slots
        // gerendert werden - nicht das InstanceCount-Maximum.
        horizontal.VisibleInstanceCount = hi;
        vertical.VisibleInstanceCount = vi;
    }

    // Die Wand-Orientierung steckt im BoxMesh.Size, das im _Ready aus den
    // [Export]-Werten gesetzt wird - NICHT in dieser Transform-Methode.
    // Hier wird nur die Position gesetzt. Beide Helper bleiben trotz identischem
    // Body getrennt, damit der Aufrufer am Methodennamen erkennt, in welchen
    // Bucket geschrieben wird.
    private Transform3D HorizontalWallTransform(float centerX, float centerZ) =>
        new(Basis.Identity, new Vector3(centerX, WallHeight / 2f, centerZ));

    private Transform3D VerticalWallTransform(float centerX, float centerZ) =>
        new(Basis.Identity, new Vector3(centerX, WallHeight / 2f, centerZ));

    /// <summary>
    /// Setzt den Zielzustand fuer den Entdeckungs-Modus.
    /// Die visuelle Uebergangsanimation laeuft in _Process.
    /// </summary>
    public void SetExploreMode(bool enabled) => _exploreTarget = enabled;

    // -------------------------------------------------------------------------
    // Start- / Ziel-Marker
    // -------------------------------------------------------------------------

    /// <summary>
    /// Platziert leuchtende Marker fuer Start (gruen) und Ziel (gold) auf den
    /// Eckzellen des Labyrinths. Alte Marker werden vorher entfernt.
    /// </summary>
    private void PlaceMarkers(Model.Maze maze)
    {
        _startMarker?.QueueFree();
        _goalMarker?.QueueFree();

        _startMarker = CreateMarker(
            CellCenter(0, 0),
            new Color(0.0f, 0.90f, 0.46f),   // gruen
            new Color(0.41f, 1.0f, 0.68f),    // hell-gruen Licht
            "Start"
        );
        _goalMarker = CreateMarker(
            CellCenter(maze.Width - 1, maze.Height - 1),
            new Color(1.0f, 0.84f, 0.0f),     // gold
            new Color(1.0f, 0.95f, 0.4f),     // gelb Licht
            "Goal"
        );

        AddChild(_startMarker);
        AddChild(_goalMarker);
    }

    private Vector3 CellCenter(int x, int y) =>
        new Vector3(x * CellSize + CellSize * 0.5f, 0f, y * CellSize + CellSize * 0.5f);

    /// <summary>
    /// Erzeugt einen Marker-Node bestehend aus einem leuchtenden Zylinder-Pad
    /// und einem farbigen OmniLight darueber.
    /// </summary>
    private Node3D CreateMarker(Vector3 worldPos, Color color, Color lightColor, string markerName)
    {
        var root = new Node3D { Name = markerName };
        root.Position = worldPos;

        // --- Leuchtende Scheibe ---
        var pad = new MeshInstance3D();
        var cylinder = new CylinderMesh
        {
            TopRadius    = CellSize * 0.38f,
            BottomRadius = CellSize * 0.38f,
            Height       = 0.07f,
            RadialSegments = 20
        };
        pad.Mesh = cylinder;
        pad.Position = new Vector3(0f, 0.035f, 0f);

        var mat = new StandardMaterial3D
        {
            AlbedoColor              = color,
            EmissionEnabled          = true,
            Emission                 = color,
            EmissionEnergyMultiplier = 2.0f
        };
        pad.MaterialOverride = mat;
        root.AddChild(pad);

        // --- Farbiges Punktlicht ---
        var light = new OmniLight3D
        {
            LightColor  = lightColor,
            LightEnergy = 2.0f,
            OmniRange   = CellSize * 4.0f,
            Position    = new Vector3(0f, WallHeight * 0.6f, 0f)
        };
        root.AddChild(light);

        return root;
    }

    private void ApplyExploreFactor(float factor)
    {
        var env = _worldEnv.Environment;
        _sun.LightEnergy = Mathf.Lerp(1.0f, 0.05f, factor);
        env.AmbientLightEnergy = Mathf.Lerp(0.4f, 0.05f, factor);
        _playerLight.LightEnergy = Mathf.Lerp(0f, 1.6f, factor);
        _playerLight.Visible = factor > 0.01f;

        // Fog wird ueber Density eingeblendet, damit der Uebergang weich bleibt.
        env.FogEnabled = factor > 0.01f;
        env.FogDensity = Mathf.Lerp(0f, 0.06f, factor);
    }
}
