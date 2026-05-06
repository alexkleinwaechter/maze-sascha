using System.Collections.Generic;
using Maze.Model;

namespace Maze.Gameplay;

/// <summary>
/// KI-Zustand eines einzelnen Waechters. Reine Logikklasse, kennt weder Godot noch Rendering.
/// Vom <see cref="GuardDirector"/> orchestriert, von <see cref="GuardPerception"/> und
/// <see cref="GuardNavigator"/> als Datenpaket benutzt.
/// </summary>
public enum GuardMode
{
    Patrol,
    Alert,
    Chase,
    Search,
    Return
}

/// <summary>
/// Laufzeitdaten eines Guards. Pro Tick mutiert der Director Felder, daher mutable.
/// </summary>
public sealed class GuardState
{
    public int GuardId { get; }

    /// <summary>Aktuelle Zelle (logische Position, auch waehrend Cell-Animation der View).</summary>
    public Cell CurrentCell { get; set; }

    /// <summary>Cardinal-Blickrichtung des Guards. FOV-Test ist +/- 70 Grad um diese Richtung.</summary>
    public Direction FacingDirection { get; set; } = Direction.North;

    /// <summary>Letzte bestaetigte Spielerzelle (fuer Chase-/Search-Pipeline).</summary>
    public Cell LastKnownPlayerCell { get; set; }

    public GuardMode Mode { get; set; } = GuardMode.Patrol;

    /// <summary>Vom <see cref="GuardPatrolRouteBuilder"/> erzeugt, wird zyklisch begangen.</summary>
    public List<Cell> PatrolRoute { get; set; } = new();

    public int PatrolIndex { get; set; }

    /// <summary>Sekunden im aktuellen Mode (Reset bei Mode-Wechsel).</summary>
    public float StateTimer { get; set; }

    /// <summary>Restliche Cooldown-Sekunden bis ein Repath erlaubt ist (Phase 24).</summary>
    public float RepathCooldown { get; set; }

    /// <summary>Restliche Sekunden bis zur naechsten Cell-Bewegung (mode-spezifisches Tempo).</summary>
    public float MoveCooldown { get; set; }

    /// <summary>
    /// Sekunden ohne Sicht im Chase. Phase-23-Vereinfachung fuer Chase -> Patrol.
    /// In Phase 24 ersetzt durch Search/Return-Pipeline.
    /// </summary>
    public float ChaseLossTimer { get; set; }

    /// <summary>Zaehlt Schritte im Search-Mode (Suchbudget = Zeit ODER Schritte).</summary>
    public int SearchStepsTaken { get; set; }

    public GuardState(int guardId, Cell startCell)
    {
        GuardId = guardId;
        CurrentCell = startCell;
    }

    /// <summary>Wechselt den Mode und resettet den StateTimer.</summary>
    public void EnterMode(GuardMode mode)
    {
        Mode = mode;
        StateTimer = 0f;
        ChaseLossTimer = 0f;
        SearchStepsTaken = 0;
    }
}
