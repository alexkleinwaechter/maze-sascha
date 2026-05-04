using Godot;
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
    private Model.Maze _maze = null!;

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

    public override void _Ready()
    {
        _wallContainer = GetNode<Node3D>("WallContainer");
        _floor = GetNode<MeshInstance3D>("Floor");
        _wallsHorizontal = GetNode<MultiMeshInstance3D>("WallContainer/WallsHorizontal");
        _wallsVertical = GetNode<MultiMeshInstance3D>("WallContainer/WallsVertical");
        _camera = GetNode<CameraController3D>("Camera3D");
        _sun = GetNode<DirectionalLight3D>("Sun");
        _playerLight = GetNode<OmniLight3D>("Player/PlayerLight");
        _worldEnv = GetNode<WorldEnvironment>("WorldEnvironment");

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
            return;

        float lerpStep = ExploreLerpSpeed * (float)delta;
        _exploreFactor = Mathf.MoveToward(_exploreFactor, target, lerpStep);
        ApplyExploreFactor(_exploreFactor);
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
