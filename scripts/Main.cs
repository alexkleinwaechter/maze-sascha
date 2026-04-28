using Godot;
using Maze.UI;

namespace Maze;

/// <summary>
/// Wurzelskript der Hauptszene. In dieser Phase nimmt Main HUD-Signale entgegen
/// und gibt sie als Console-Print aus, damit die Verkabelung sichtbar ist.
/// </summary>
public partial class Main : Node
{
    private Hud _hud = null!;

    public override void _Ready()
    {
        _hud = GetNode<Hud>("Hud");

        // Signale per C#-Eventsyntax abonnieren - typsicher und ohne Magic Strings.
        _hud.GenerateRequested += OnGenerateRequested;
        _hud.SolveRequested += OnSolveRequested;
        _hud.SpeedChanged += OnSpeedChanged;
        _hud.PauseToggle += OnPauseToggled;
        _hud.StepRequested += OnStepRequested;
        _hud.ResetRequested += OnResetRequested;
        _hud.ViewToggleRequested += OnViewToggled;

        GD.Print("[Main] HUD verbunden.");
    }

    public override void _Process(double delta) { }

    public override void _PhysicsProcess(double delta) { }

    public override void _ExitTree() => GD.Print("[Main] _ExitTree.");

    private void OnGenerateRequested(int width, int height, string generatorId) =>
        GD.Print($"[Main] Generate {generatorId} {width}x{height}");

    private void OnSolveRequested(string solverId) =>
        GD.Print($"[Main] Solve mit {solverId}");

    private void OnSpeedChanged(float stepsPerSecond) =>
        GD.Print($"[Main] Tempo: {stepsPerSecond} Schritte/s");

    private void OnPauseToggled(bool paused) =>
        GD.Print($"[Main] Pause = {paused}");

    private void OnStepRequested() =>
        GD.Print("[Main] Schritt angefordert");

    private void OnResetRequested() =>
        GD.Print("[Main] Reset");

    private void OnViewToggled(bool use3D) =>
        GD.Print($"[Main] 3D-Ansicht = {use3D}");
}
