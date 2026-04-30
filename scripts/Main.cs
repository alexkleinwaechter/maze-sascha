using System;
using System.Collections.Generic;
using Godot;
using Maze.Generators;
using Maze.Model;
using Maze.Solvers;
using Maze.UI;
using Maze.Views;

namespace Maze;

/// <summary>
/// Wurzelskript der Hauptszene. Verbindet HUD, Runner und aktive View.
/// </summary>
public partial class Main : Node
{
    private Hud _hud = null!;
    private StatsPanel _stats = null!;
    private MazeView2D _view2D = null!;
    private MazeView3D _view3D = null!;
    private AlgorithmRunner _runner = null!;

    private Model.Maze _currentMaze = null!;

    private readonly Dictionary<string, IMazeGenerator> _generators = new()
    {
        ["recursive-backtracker"] = new RecursiveBacktrackerGenerator(),
        ["growing-tree"] = new GrowingTreeGenerator(),
        ["recursive-division"] = new RecursiveDivisionGenerator(),
        ["cellular-automata"] = new CellularAutomataGenerator()
    };

    private readonly Dictionary<string, IMazeSolver> _solvers = new()
    {
        ["bfs"] = new BreadthFirstSolver(),
        ["dfs"] = new DepthFirstSolver(),
        ["a-star"] = new AStarSolver(),
        ["greedy"] = new GreedyBestFirstSolver(),
        ["wall-follower"] = new WallFollowerSolver(),
        ["dead-end-filling"] = new DeadEndFillingSolver()
    };
    private Cell _solverStart = null!;
    private Cell _solverGoal = null!;

    private PlayerCharacter3D _player = null!;
    private readonly List<Cell> _solverPath = new();

    private readonly Random _random = new();
    private readonly PerformanceTracker _tracker = new();
    private bool _suppressViewRefresh;
    private const float DefaultSolverStepsPerSecond = 30f;
    private const float DefaultBotCellsPerSecond = 4f;

    public override void _Ready()
    {
        _hud = GetNode<Hud>("Hud");
        _stats = GetNode<StatsPanel>("Hud/StatsPanel");
        _view2D = GetNode<MazeView2D>("MazeView2D");
        _view3D = GetNode<MazeView3D>("MazeView3D");
        _runner = GetNode<AlgorithmRunner>("Runner");

        // Signale per C#-Eventsyntax abonnieren - typsicher und ohne Magic Strings.
        _hud.GenerateRequested += OnGenerateRequested;
        _hud.SolveRequested += OnSolveRequested;
        _hud.SpeedChanged += OnSpeedChanged;
        _hud.PauseToggle += OnPauseToggled;
        _hud.StepRequested += OnStepRequested;
        _hud.ResetRequested += OnResetRequested;
        _hud.ViewToggleRequested += OnViewToggled;
        _hud.HeatmapToggle += OnHeatmapToggled;
        _hud.UnboundedModeChanged += OnUnboundedModeChanged;
        _hud.FollowCamToggle += OnFollowCamToggled;

        _player = GetNode<PlayerCharacter3D>("MazeView3D/Player");
        _player.GoalReached += OnBotGoalReached;

        _runner.GenerationStepProduced += OnGenerationStepProduced;
        _runner.GenerationFinished += OnGenerationFinished;
        _runner.SolverStepProduced += OnSolverStepProduced;
        _runner.SolverFinished += OnSolverFinished;
        _runner.StepsPerSecond = DefaultSolverStepsPerSecond;
        _player.MoveSpeed = DefaultBotCellsPerSecond;

        _view2D.Visible = true;
        _view3D.Visible = false;

        GD.Print("[Main] HUD, Runner, 2D-View und 3D-View verbunden.");
    }

    public override void _Process(double delta) { }

    public override void _PhysicsProcess(double delta) { }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Space } && _runner.IsPaused)
            _runner.ForceSingleStep();
    }

    public override void _ExitTree() => GD.Print("[Main] _ExitTree.");

    private void OnGenerateRequested(int width, int height, string generatorId)
    {
        if (!_generators.TryGetValue(generatorId, out var generator))
        {
            GD.PrintErr($"[Main] Unbekannter Generator: {generatorId}");
            return;
        }

        _tracker.Start();
        _stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, 0);

        // Vor einer Neugenerierung alte Bot-Animation und Pfadreste sicher verwerfen.
        _solverPath.Clear();
        _player.Hide();
        _runner.StopAll();
        _currentMaze = new Model.Maze(width, height);
        _view2D.SetMaze(_currentMaze);
        // 3D-View wird NICHT hier gebaut – erst nach Abschluss der Generierung (OnGenerationFinished).

        _runner.StartGeneration(generator.Generate(_currentMaze, _random));
        _runner.IsPaused = false;
        GD.Print($"[Main] Generator gestartet: {generator.Name}");
    }

    private void OnGenerationStepProduced()
    {
        if (_currentMaze == null) return;

        var step = _runner.LastGenerationStep;
        step.Cell.State = step.NewState;

        _tracker.TickStep();
        _tracker.IncrementVisited();

        if (_suppressViewRefresh)
            return;

        _view2D.Refresh();
        _stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, 0);
    }

    private void OnGenerationFinished()
    {
        if (_currentMaze == null) return;

        foreach (var cell in _currentMaze.AllCells())
            cell.State = CellState.Open;

        _view2D.ForceRefresh();
        // 3D nur aufbauen, wenn der User die 3D-Ansicht gerade sieht. Wird die
        // Ansicht spaeter umgeschaltet, baut OnViewToggled lazy nach.
        if (_view3D.Visible)
            _view3D.SetMaze(_currentMaze);
        _tracker.Stop();
        _stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, _tracker.ManagedMemoryDeltaBytes);
        GD.Print("[Main] Generator fertig.");
    }

    private void OnSolveRequested(string solverId)
    {
        if (_currentMaze == null)
        {
            GD.PrintErr("[Main] Kein Maze zum Loesen vorhanden.");
            return;
        }

        if (!_solvers.TryGetValue(solverId, out var solver))
        {
            GD.PrintErr($"[Main] Unbekannter Solver: {solverId}");
            return;
        }

        _tracker.Start();
        _stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, 0);

        // Solver-Lauf immer mit sauberem Bot- und Pfad-Zustand starten.
        _solverPath.Clear();
        _player.Hide();
        _runner.StopAll();

        _currentMaze.ResetSolverState();

        _solverStart = _currentMaze.GetCell(0, 0);
        _solverGoal = _currentMaze.GetCell(_currentMaze.Width - 1, _currentMaze.Height - 1);
        _solverStart.State = CellState.Start;
        _solverGoal.State = CellState.Goal;

        _view2D.Refresh();

        _runner.StartSolver(solver.Solve(_currentMaze, _solverStart, _solverGoal));
        _runner.IsPaused = false;
        GD.Print($"[Main] Solver gestartet: {solver.Name}");
    }

    private void OnSolverStepProduced()
    {
        var step = _runner.LastSolverStep;
        if (step == null)
            return;

        if (step.Cell == _solverStart)
            step.Cell.State = CellState.Start;
        else if (step.Cell == _solverGoal)
            step.Cell.State = CellState.Goal;
        else
            step.Cell.State = step.NewState;

        step.Cell.Distance = step.Distance;

        // Den finalen Pfad zellweise sammeln; die Reihenfolge entspricht der vom Solver
        // emittierten Index-Reihenfolge (Distance == Pfad-Index).
        if (step.NewState == CellState.Path)
            _solverPath.Add(step.Cell);

        _tracker.TickStep();
        if (step.NewState == CellState.Visited)
            _tracker.IncrementVisited();
        if (step.NewState == CellState.Path)
            _tracker.SetPathLength(step.Distance + 1);

        if (_suppressViewRefresh)
            return;

        _view2D.Refresh();
        _stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, 0);
    }

    private void OnSolverFinished()
    {
        _view2D.ForceRefresh();
        _tracker.Stop();
        _stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, _tracker.ManagedMemoryDeltaBytes);
        GD.Print("[Main] Solver fertig.");

        // Pfad defensiv nach Distance sortieren - falls ein Solver Path-Schritte
        // nicht in Index-Reihenfolge yieldet, ist die Animation trotzdem korrekt.
        _solverPath.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        // Vollstaendige Wegpunktliste aufbauen: Start, alle Path-Zellen, Goal.
        var fullPath = new List<Cell>(_solverPath.Count + 2) { _solverStart };
        fullPath.AddRange(_solverPath);
        fullPath.Add(_solverGoal);

        GD.Print($"[Main] Solver-Pfaddiagnose: pathCells={_solverPath.Count}, fullWaypoints={fullPath.Count}, start=({_solverStart.X},{_solverStart.Y}), goal=({_solverGoal.X},{_solverGoal.Y})");

        // Wenn keine Loesung gefunden wurde (Pfad leer und Start nicht direkt am Goal),
        // den Bot gar nicht erst starten - sonst wuerde er quer durchs Maze teleportieren.
        if (_solverPath.Count == 0 && !AreNeighbors(_solverStart, _solverGoal))
        {
            GD.Print("[Main] Kein Pfad zum Loesen vorhanden - Bot bleibt versteckt.");
            return;
        }

        _player.StartFollowingPath(fullPath, _view3D.CellSize);
    }

    private static bool AreNeighbors(Cell a, Cell b) =>
        System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) == 1;

    private void OnSpeedChanged(float stepsPerSecond)
    {
        _runner.StepsPerSecond = stepsPerSecond;

        // Der HUD-Regler soll nicht nur den Solver-Takt, sondern auch die sichtbare
        // Bot-Bewegung steuern. Deshalb mappen wir ihn relativ zum Default 30 -> 4.
        float speedFactor = stepsPerSecond / DefaultSolverStepsPerSecond;
        _player.MoveSpeed = DefaultBotCellsPerSecond * speedFactor;
    }

    private void OnPauseToggled(bool paused) =>
        _runner.IsPaused = paused;

    private void OnStepRequested() =>
        _runner.ForceSingleStep();

    private void OnResetRequested()
    {
        _runner.StopAll();
        _solverPath.Clear();
        _player.Hide();
        _currentMaze = null;
        _solverStart = null!;
        _solverGoal = null!;
        var resetMaze = new Model.Maze(2, 2);
        _view2D.SetMaze(resetMaze);
        _view3D.SetMaze(resetMaze);
        GD.Print("[Main] Reset.");
    }

    private void OnViewToggled(bool use3D)
    {
        _view2D.Visible = !use3D;
        _view3D.Visible = use3D;
        // Falls 3D eingeschaltet wird, aber noch kein SetMaze lief (z. B. Reset), sicherstellen.
        if (use3D && _currentMaze != null)
            _view3D.SetMaze(_currentMaze);
        GD.Print($"[Main] 3D-Ansicht = {use3D}");
    }

    private void OnHeatmapToggled(bool enabled)
    {
        _view2D.ShowDistances = enabled;
        _view2D.Refresh();
    }

    private void OnUnboundedModeChanged(bool unbounded)
    {
        _suppressViewRefresh = unbounded;
        _runner.Mode = unbounded ? AlgorithmRunner.RunMode.Unbounded : AlgorithmRunner.RunMode.Throttled;
    }

    private void OnBotGoalReached()
    {
        GD.Print("[Main] Bot ist am Ziel angekommen.");
    }

    private void OnFollowCamToggled(bool enabled)
    {
        var camera = _view3D.GetNode<CameraController3D>("Camera3D");
        if (enabled)
            camera.EnableFollow(_player);
        else
            camera.DisableFollow();
    }
}
