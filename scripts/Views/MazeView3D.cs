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
    private Model.Maze _maze = null!;

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
    }

    public void SetMaze(Model.Maze maze)
    {
        _maze = maze;
        Rebuild();
    }

    public void Refresh()
    {
        if (_maze != null)
            Rebuild();
    }

    private void Rebuild()
    {
        ClearWalls();
        if (_maze == null)
            return;

        BuildFloor(_maze);
        BuildWalls(_maze);
    }

    private void ClearWalls()
    {
        foreach (Node child in _wallContainer.GetChildren())
            child.QueueFree();
    }

    private void BuildFloor(Model.Maze maze)
    {
        Vector3 size = new(maze.Width * CellSize, 0.05f, maze.Height * CellSize);
        _floor.Mesh = new BoxMesh { Size = size };
        _floor.MaterialOverride = FloorMaterial;
        _floor.Position = new Vector3(maze.Width * CellSize / 2f, -0.025f, maze.Height * CellSize / 2f);
    }

    private void BuildWalls(Model.Maze maze)
    {
        for (int y = 0; y < maze.Height; y++)
        for (int x = 0; x < maze.Width; x++)
        {
            Cell cell = maze.GetCell(x, y);
            if (cell.HasWall(Direction.North))
                AddWall(x * CellSize + CellSize / 2f, y * CellSize, CellSize, WallThickness);
            if (cell.HasWall(Direction.West))
                AddWall(x * CellSize, y * CellSize + CellSize / 2f, WallThickness, CellSize);
            if (y == maze.Height - 1 && cell.HasWall(Direction.South))
                AddWall(x * CellSize + CellSize / 2f, (y + 1) * CellSize, CellSize, WallThickness);
            if (x == maze.Width - 1 && cell.HasWall(Direction.East))
                AddWall((x + 1) * CellSize, y * CellSize + CellSize / 2f, WallThickness, CellSize);
        }
    }

    private void AddWall(float centerX, float centerZ, float lengthX, float lengthZ)
    {
        var instance = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(lengthX, WallHeight, lengthZ) },
            MaterialOverride = WallMaterial,
            Position = new Vector3(centerX, WallHeight / 2f, centerZ)
        };

        _wallContainer.AddChild(instance);
    }
}