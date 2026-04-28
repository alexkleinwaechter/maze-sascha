using Godot;

namespace Maze.UI;

/// <summary>
/// Bedienoberflaeche oben am Bildschirm.
/// Sendet typisierte Signale an Main, statt direkt Felder zu aendern.
/// </summary>
public partial class Hud : CanvasLayer
{
    // Signale werden in C# als delegate deklariert; das Suffix EventHandler ist Pflicht.
    [Signal] public delegate void GenerateRequestedEventHandler(int width, int height, string generatorId);
    [Signal] public delegate void SolveRequestedEventHandler(string solverId);
    [Signal] public delegate void SpeedChangedEventHandler(float stepsPerSecond);
    [Signal] public delegate void ViewToggleRequestedEventHandler(bool use3D);
    [Signal] public delegate void PauseToggleEventHandler(bool paused);
    [Signal] public delegate void StepRequestedEventHandler();
    [Signal] public delegate void ResetRequestedEventHandler();

    // ---- Knotenreferenzen (in _Ready aufgeloest) ----
    private HSlider _widthSlider = null!;
    private HSlider _heightSlider = null!;
    private HSlider _speedSlider = null!;
    private OptionButton _generatorChooser = null!;
    private OptionButton _solverChooser = null!;
    private Button _generateButton = null!;
    private Button _solveButton = null!;
    private Button _pauseButton = null!;
    private Button _stepButton = null!;
    private Button _resetButton = null!;
    private CheckBox _viewToggle = null!;
    private Label _widthLabel = null!;
    private Label _heightLabel = null!;
    private Label _speedLabel = null!;

    public override void _Ready()
    {
        _widthSlider = GetNode<HSlider>("Root/Margin/VBox/Sizes/WidthSlider");
        _heightSlider = GetNode<HSlider>("Root/Margin/VBox/Sizes/HeightSlider");
        _speedSlider = GetNode<HSlider>("Root/Margin/VBox/SpeedRow/SpeedSlider");
        _generatorChooser = GetNode<OptionButton>("Root/Margin/VBox/Algos/GeneratorChooser");
        _solverChooser = GetNode<OptionButton>("Root/Margin/VBox/Algos/SolverChooser");
        _generateButton = GetNode<Button>("Root/Margin/VBox/Buttons/GenerateButton");
        _solveButton = GetNode<Button>("Root/Margin/VBox/Buttons/SolveButton");
        _pauseButton = GetNode<Button>("Root/Margin/VBox/Buttons/PauseButton");
        _stepButton = GetNode<Button>("Root/Margin/VBox/Buttons/StepButton");
        _resetButton = GetNode<Button>("Root/Margin/VBox/Buttons/ResetButton");
        _viewToggle = GetNode<CheckBox>("Root/Margin/VBox/Algos/View3DToggle");
        _widthLabel = GetNode<Label>("Root/Margin/VBox/Sizes/WidthLabel");
        _heightLabel = GetNode<Label>("Root/Margin/VBox/Sizes/HeightLabel");
        _speedLabel = GetNode<Label>("Root/Margin/VBox/SpeedRow/SpeedLabel");

        // ---- Slider-Werte initial sicherstellen ----
        _widthSlider.MinValue = 5;
        _widthSlider.MaxValue = 75;
        _widthSlider.Step = 1;
        _widthSlider.Value = 25;

        _heightSlider.MinValue = 5;
        _heightSlider.MaxValue = 75;
        _heightSlider.Step = 1;
        _heightSlider.Value = 25;

        _speedSlider.MinValue = 1;
        _speedSlider.MaxValue = 240;
        _speedSlider.Step = 1;
        _speedSlider.Value = 30;

        UpdateLabels();

        // ---- Signal-Wiring (C#-Eventsyntax) ----
        _widthSlider.ValueChanged += _ => UpdateLabels();
        _heightSlider.ValueChanged += _ => UpdateLabels();
        _speedSlider.ValueChanged += OnSpeedChanged;
        _generateButton.Pressed += OnGeneratePressed;
        _solveButton.Pressed += OnSolvePressed;
        _pauseButton.Toggled += OnPauseToggled;
        _stepButton.Pressed += OnStepPressed;
        _resetButton.Pressed += OnResetPressed;
        _viewToggle.Toggled += OnViewToggled;

        FillGeneratorChooser();
        FillSolverChooser();
    }

    private void UpdateLabels()
    {
        _widthLabel.Text = $"Breite:  {(int)_widthSlider.Value}";
        _heightLabel.Text = $"Hoehe:    {(int)_heightSlider.Value}";
        _speedLabel.Text = $"Tempo:  {(int)_speedSlider.Value} Schritte/s";
    }

    private void OnSpeedChanged(double value)
    {
        UpdateLabels();
        EmitSignal(SignalName.SpeedChanged, (float)value);
    }

    private void OnGeneratePressed()
    {
        int w = (int)_widthSlider.Value;
        int h = (int)_heightSlider.Value;
        var id = (string)_generatorChooser.GetItemMetadata(_generatorChooser.Selected);
        EmitSignal(SignalName.GenerateRequested, w, h, id);
    }

    private void OnSolvePressed()
    {
        var id = (string)_solverChooser.GetItemMetadata(_solverChooser.Selected);
        EmitSignal(SignalName.SolveRequested, id);
    }

    private void OnPauseToggled(bool pressed)
    {
        _pauseButton.Text = pressed ? "Fortsetzen" : "Pause";
        EmitSignal(SignalName.PauseToggle, pressed);
    }

    private void OnStepPressed() => EmitSignal(SignalName.StepRequested);

    private void OnResetPressed() => EmitSignal(SignalName.ResetRequested);

    private void OnViewToggled(bool pressed)
    {
        EmitSignal(SignalName.ViewToggleRequested, pressed);
    }

    private void FillGeneratorChooser()
    {
        // Metadaten = Algorithmus-ID, die der Runner versteht.
        // Reihenfolge bewusst didaktisch: einfach -> komplex.
        _generatorChooser.Clear();
        AddGenerator("Recursive Backtracker", "recursive-backtracker");
        AddGenerator("Growing Tree (75% newest, 25% random)", "growing-tree");
        AddGenerator("Recursive Division", "recursive-division");
        AddGenerator("Cellular Automata (4-5)", "cellular-automata");
        _generatorChooser.Selected = 0;
    }

    private void FillSolverChooser()
    {
        _solverChooser.Clear();
        AddSolver("Breadth-First Search (BFS)", "bfs");
        AddSolver("Depth-First Search (DFS)", "dfs");
        AddSolver("A*", "a-star");
        AddSolver("Greedy Best-First", "greedy");
        AddSolver("Wall Follower (links)", "wall-follower");
        AddSolver("Dead-End Filling", "dead-end-filling");
        _solverChooser.Selected = 0;
    }

    private void AddGenerator(string label, string id)
    {
        int index = _generatorChooser.ItemCount;
        _generatorChooser.AddItem(label);
        _generatorChooser.SetItemMetadata(index, id);
    }

    private void AddSolver(string label, string id)
    {
        int index = _solverChooser.ItemCount;
        _solverChooser.AddItem(label);
        _solverChooser.SetItemMetadata(index, id);
    }
}
