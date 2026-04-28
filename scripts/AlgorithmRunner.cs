using System.Collections.Generic;
using Godot;
using Maze.Generators;
using Maze.Solvers;

namespace Maze;

/// <summary>
/// Treibt einen Iterator aus Generator/Solver tickweise voran.
/// Geschwindigkeit (Schritte/s) wird per Property gesteuert; Pause/Step werden unterstuetzt.
/// </summary>
public partial class AlgorithmRunner : Node
{
    [Signal] public delegate void GenerationStepProducedEventHandler();
    [Signal] public delegate void SolverStepProducedEventHandler();
    [Signal] public delegate void GenerationFinishedEventHandler();
    [Signal] public delegate void SolverFinishedEventHandler();

    public float StepsPerSecond { get; set; } = 30f;
    public bool IsPaused { get; set; }
    public bool IsRunning => _genIterator != null || _solverIterator != null;

    public GenerationStep LastGenerationStep { get; private set; } = null!;
    public SolverStep LastSolverStep { get; private set; } = null!;

    private IEnumerator<GenerationStep> _genIterator = null!;
    private IEnumerator<SolverStep> _solverIterator = null!;
    private double _accumulator;

    public void StartGeneration(IEnumerable<GenerationStep> steps)
    {
        _genIterator?.Dispose();
        _genIterator = steps.GetEnumerator();
        _solverIterator = null;
        _accumulator = 0;
    }

    public void StartSolver(IEnumerable<SolverStep> steps)
    {
        _solverIterator?.Dispose();
        _solverIterator = steps.GetEnumerator();
        _genIterator = null;
        _accumulator = 0;
    }

    public void StopAll()
    {
        _genIterator?.Dispose();
        _genIterator = null;
        _solverIterator?.Dispose();
        _solverIterator = null;
    }

    /// <summary>Manuelles Vorruecken um genau einen Schritt (Step-Modus).</summary>
    public void ForceSingleStep()
    {
        if (_genIterator != null)
        {
            AdvanceGenerator();
            return;
        }

        if (_solverIterator != null)
            AdvanceSolver();
    }

    public override void _Process(double delta)
    {
        if (IsPaused || !IsRunning) return;

        _accumulator += delta;
        double secondsPerStep = 1.0 / Mathf.Max(1f, StepsPerSecond);

        // Mehrere Schritte pro Frame, falls die Tempovorgabe es erfordert.
        while (_accumulator >= secondsPerStep && IsRunning)
        {
            _accumulator -= secondsPerStep;

            if (_genIterator != null)
            {
                AdvanceGenerator();
                continue;
            }

            if (_solverIterator != null)
            {
                AdvanceSolver();
                continue;
            }
        }
    }

    private void AdvanceGenerator()
    {
        if (_genIterator.MoveNext())
        {
            LastGenerationStep = _genIterator.Current;
            EmitSignal(SignalName.GenerationStepProduced);
        }
        else
        {
            _genIterator.Dispose();
            _genIterator = null;
            EmitSignal(SignalName.GenerationFinished);
        }
    }

    private void AdvanceSolver()
    {
        if (_solverIterator.MoveNext())
        {
            LastSolverStep = _solverIterator.Current;
            EmitSignal(SignalName.SolverStepProduced);
        }
        else
        {
            _solverIterator.Dispose();
            _solverIterator = null;
            EmitSignal(SignalName.SolverFinished);
        }
    }
}
