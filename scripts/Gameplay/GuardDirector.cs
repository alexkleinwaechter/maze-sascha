using System;
using System.Collections.Generic;
using Godot;
using Maze.Model;
using Maze.Views;

namespace Maze.Gameplay;

/// <summary>
/// Orchestriert alle Guards: Tick-Loop, State-Machine, Spawn, Audio-Cues.
///
/// Tick: 8 Hz. Pfade werden mit RepathCooldown limitiert (0.4s).
/// Geschwindigkeitsverhaeltnisse pro Mode werden ueber MoveCooldown abgebildet:
///   Patrol  0.7x Spieler  (langsamer)
///   Chase   1.15x Spieler (schneller, Reaktion erzwingend)
///   Search  0.8x Spieler
///   Return  1.0x Spieler
///
/// Phase 23: Patrol -> Alert -> Chase -> (ChaseLossTimer 3s) -> Patrol.
/// Phase 24: Chase -> Search (Sichtverlust 0.5s) -> Return -> Patrol.
/// </summary>
public partial class GuardDirector : Node
{
    [Signal] public delegate void StatusChangedEventHandler(string text);
    [Signal] public delegate void PlayerCaughtEventHandler();

    // --- Tick-Konfiguration ---
    private const float TickInterval = 1f / 8f;     // 8 Hz
    private const float StartProtectionSeconds = 2.0f;
    private const int MinSpawnDistanceManhattan = 6;

    // --- Speed-Multiplikatoren (Faktor auf Spieler-MoveSpeed in Cells/sec) ---
    private const float PatrolSpeedFactor = 0.7f;
    private const float ChaseSpeedFactor = 1.15f;
    private const float SearchSpeedFactor = 0.8f;
    private const float ReturnSpeedFactor = 1.0f;

    // --- Timing-Konstanten ---
    private const float AlertSeconds = 0.3f;
    private const float SightLossGraceSeconds = 0.5f;
    private const float ChaseLossSecondsPhase23 = 3.0f;     // Fallback wenn Search/Return aus
    private const float SearchBudgetSeconds = 5.0f;
    private const int SearchBudgetSteps = 8;
    private const float RepathCooldown = 0.4f;
    private const int SearchRadius = 3;

    // --- Zustand ---
    private Model.Maze _maze;
    private float _tickAccumulator;
    private float _runTimer;
    private float _playerMoveSpeed = 4f;
    private bool _useSearchPipeline = true;     // Phase 24+ default an
    private readonly Random _random = new();

    private readonly List<GuardState> _guards = new();
    private readonly List<GuardCharacter3D> _views = new();
    private Node3D _guardsContainer;
    private float _cellSize = 1f;
    private AudioStreamPlayer _detectAudio;
    private AudioStreamPlayer _escapeAudio;
    private List<Cell> _solverPathList;          // BFS Start->Goal, zur Spawn-Distanz-Sortierung
    private HashSet<Cell> _solverPathSet;        // O(1) Contains fuer Off-Path-Score im Builder

    // Spieler-Zugriff per Delegate, damit der Director nicht direkt PlayerCharacter3D referenziert.
    private Func<Cell> _getPlayerSightCell;
    private Func<Cell> _getPlayerCurrentCell;
    private Func<bool> _getPlayerSneaking;

    private GuardTelemetry _telemetry;

    public bool IsActive { get; private set; }

    /// <summary>Telemetrie-Snapshot nach Run-Ende (oder live, falls noetig).</summary>
    public GuardTelemetry Telemetry => _telemetry;

    public override void _Process(double delta)
    {
        if (!IsActive || _maze is null) return;
        _runTimer += (float)delta;
        _tickAccumulator += (float)delta;
        if (_tickAccumulator < TickInterval) return;

        float dt = _tickAccumulator;
        _tickAccumulator = 0f;
        Tick(dt);
    }

    // ----------------------------------------------------------------
    // Public API
    // ----------------------------------------------------------------

    /// <summary>
    /// Startet den Director. Spawnt einen Guard (oder mehr) im Maze unter Beachtung von
    /// Mindestabstand und Startschutz. Rendert ueber <paramref name="container"/>.
    /// </summary>
    public void Start(
        Model.Maze maze,
        Cell playerStart,
        float cellSize,
        float playerMoveSpeed,
        Node3D container,
        AudioStreamPlayer detectAudio,
        AudioStreamPlayer escapeAudio,
        Func<Cell> getPlayerSightCell,
        Func<Cell> getPlayerCurrentCell,
        Func<bool> getPlayerSneaking,
        int guardCount = 1)
    {
        _maze = maze;
        _cellSize = cellSize;
        _playerMoveSpeed = Mathf.Max(0.5f, playerMoveSpeed);
        _guardsContainer = container;
        _detectAudio = detectAudio;
        _escapeAudio = escapeAudio;
        _getPlayerSightCell = getPlayerSightCell;
        _getPlayerCurrentCell = getPlayerCurrentCell;
        _getPlayerSneaking = getPlayerSneaking;
        _runTimer = 0f;
        _tickAccumulator = 0f;
        _telemetry = new GuardTelemetry();

        ClearGuards();

        // Loesungspfad einmalig berechnen - wird sowohl fuer Spawn-Sortierung (nahe Pfad)
        // als auch fuer Off-Path-Bonus im Routenbuilder gebraucht.
        Cell goal = _maze.GetCell(_maze.Width - 1, _maze.Height - 1);
        _solverPathList = ShortestPathCells(playerStart, goal);
        _solverPathSet = _solverPathList != null ? new HashSet<Cell>(_solverPathList) : null;

        // Spawn: bevorzugt Zellen auf BFS-Pfad Start->Goal (Phase 25), sonst frei.
        var spawnCandidates = SpawnCandidates(playerStart, guardCount);
        for (int i = 0; i < spawnCandidates.Count; i++)
        {
            SpawnGuardAt(spawnCandidates[i], i);
        }

        IsActive = true;
        EmitSignal(SignalName.StatusChanged, _guards.Count > 0 ? "Waechter aktiv" : "");
        GD.Print($"[GuardDirector] gestartet, {_guards.Count} Guards.");
    }

    /// <summary>Stoppt den Director, raeumt Views ab.</summary>
    public void Stop()
    {
        if (!IsActive) return;
        IsActive = false;
        ClearGuards();
        _solverPathList = null;
        _solverPathSet = null;
        _maze = null;
        _telemetry?.OnRunEnd();
        if (_telemetry != null)
            GD.Print($"[GuardDirector] Telemetrie: {_telemetry.Summarize()}");
        EmitSignal(SignalName.StatusChanged, "");
        GD.Print("[GuardDirector] gestoppt.");
    }

    /// <summary>Schaltet Search/Return-Pipeline ein/aus. Default an (Phase 24+).</summary>
    public void SetSearchPipelineEnabled(bool enabled) => _useSearchPipeline = enabled;

    // ----------------------------------------------------------------
    // Tick / State Machine
    // ----------------------------------------------------------------

    private void Tick(float dt)
    {
        Cell sightCell = _getPlayerSightCell?.Invoke();
        Cell catchCell = _getPlayerCurrentCell?.Invoke();
        bool sneaking = _getPlayerSneaking?.Invoke() ?? false;
        bool startProtected = _runTimer < StartProtectionSeconds;

        for (int i = 0; i < _guards.Count; i++)
        {
            var guard = _guards[i];
            var view = _views[i];

            // Update Cooldowns
            guard.MoveCooldown = Mathf.Max(0f, guard.MoveCooldown - dt);
            guard.RepathCooldown = Mathf.Max(0f, guard.RepathCooldown - dt);
            guard.StateTimer += dt;

            // Sicht-Test (nur wenn Spieler-Position bekannt und Startschutz vorbei)
            bool seesPlayer = !startProtected && sightCell != null && GuardPerception.CanSee(
                _maze, guard.CurrentCell, guard.FacingDirection, sightCell, sneaking);

            switch (guard.Mode)
            {
                case GuardMode.Patrol:  TickPatrol(guard, view, seesPlayer, sightCell); break;
                case GuardMode.Alert:   TickAlert(guard, view, seesPlayer, sightCell); break;
                case GuardMode.Chase:   TickChase(guard, view, seesPlayer, sightCell, dt); break;
                case GuardMode.Search:  TickSearch(guard, view, seesPlayer, sightCell, dt); break;
                case GuardMode.Return:  TickReturn(guard, view, seesPlayer, sightCell); break;
            }

            // Kollision pruefen
            if (catchCell != null && guard.CurrentCell == catchCell)
            {
                _telemetry?.OnCaught(catchCell);
                EmitSignal(SignalName.PlayerCaught);
                return;
            }
        }
    }

    private void TickPatrol(GuardState guard, GuardCharacter3D view, bool seesPlayer, Cell sightCell)
    {
        if (seesPlayer)
        {
            EnterAlert(guard, view, sightCell);
            return;
        }
        if (guard.PatrolRoute.Count < 2) return;
        if (guard.MoveCooldown > 0f) return;

        Cell next = NextPatrolCell(guard);
        StepGuard(guard, view, next, PatrolSpeedFactor);
    }

    private void TickAlert(GuardState guard, GuardCharacter3D view, bool seesPlayer, Cell sightCell)
    {
        if (seesPlayer && sightCell != null)
            guard.LastKnownPlayerCell = sightCell;

        if (guard.StateTimer >= AlertSeconds)
        {
            guard.EnterMode(GuardMode.Chase);
            view.SetModeColor(GuardMode.Chase);
            _telemetry?.OnDetection(_runTimer);
            // Detection-Audio bereits beim Patrol->Alert getriggert; nichts hier.
        }
    }

    private void TickChase(GuardState guard, GuardCharacter3D view, bool seesPlayer, Cell sightCell, float dt)
    {
        if (seesPlayer && sightCell != null)
        {
            guard.LastKnownPlayerCell = sightCell;
            guard.ChaseLossTimer = 0f;
        }
        else
        {
            guard.ChaseLossTimer += dt;
        }
        _telemetry?.OnChaseTick(dt);

        // Phase-23-Pfad oder Phase-24-Pfad
        if (_useSearchPipeline)
        {
            if (guard.ChaseLossTimer >= SightLossGraceSeconds && !seesPlayer)
            {
                guard.EnterMode(GuardMode.Search);
                view.SetModeColor(GuardMode.Search);
                return;
            }
        }
        else
        {
            if (guard.ChaseLossTimer >= ChaseLossSecondsPhase23)
            {
                ReturnToPatrol(guard, view);
                PlayAudio(_escapeAudio);
                _telemetry?.OnEscape();
                return;
            }
        }

        if (guard.MoveCooldown > 0f) return;
        if (guard.LastKnownPlayerCell == null) return;

        Cell next = NextStepWithRepathBudget(guard, guard.LastKnownPlayerCell);
        if (next == null) return;
        StepGuard(guard, view, next, ChaseSpeedFactor);
    }

    private void TickSearch(GuardState guard, GuardCharacter3D view, bool seesPlayer, Cell sightCell, float dt)
    {
        if (seesPlayer && sightCell != null)
        {
            // Spieler wieder gesichtet -> direkt in Chase.
            guard.LastKnownPlayerCell = sightCell;
            guard.EnterMode(GuardMode.Chase);
            view.SetModeColor(GuardMode.Chase);
            return;
        }

        // Suchbudget abgelaufen? Zeit ODER Schritte.
        if (guard.StateTimer >= SearchBudgetSeconds || guard.SearchStepsTaken >= SearchBudgetSteps)
        {
            guard.EnterMode(GuardMode.Return);
            view.SetModeColor(GuardMode.Return);
            PlayAudio(_escapeAudio);
            _telemetry?.OnEscape();
            return;
        }

        if (guard.MoveCooldown > 0f) return;

        // Random-Walk im Radius um LastKnownPlayerCell, mit BFS-Hilfe wenn weit weg.
        Cell target = guard.LastKnownPlayerCell ?? guard.CurrentCell;
        Cell next;
        if (GuardNavigator.Manhattan(guard.CurrentCell, target) > SearchRadius)
        {
            next = NextStepWithRepathBudget(guard, target);
        }
        else
        {
            next = GuardNavigator.RandomOpenNeighbor(_maze, guard.CurrentCell, _random);
        }
        if (next == null) return;
        StepGuard(guard, view, next, SearchSpeedFactor);
        guard.SearchStepsTaken++;
    }

    private void TickReturn(GuardState guard, GuardCharacter3D view, bool seesPlayer, Cell sightCell)
    {
        if (seesPlayer && sightCell != null)
        {
            guard.LastKnownPlayerCell = sightCell;
            guard.EnterMode(GuardMode.Chase);
            view.SetModeColor(GuardMode.Chase);
            return;
        }

        if (guard.PatrolRoute.Count < 2)
        {
            guard.EnterMode(GuardMode.Patrol);
            view.SetModeColor(GuardMode.Patrol);
            return;
        }

        Cell target = guard.PatrolRoute[guard.PatrolIndex];
        if (guard.CurrentCell == target)
        {
            guard.EnterMode(GuardMode.Patrol);
            view.SetModeColor(GuardMode.Patrol);
            return;
        }

        if (guard.MoveCooldown > 0f) return;

        Cell next = NextStepWithRepathBudget(guard, target);
        if (next == null) return;
        StepGuard(guard, view, next, ReturnSpeedFactor);
    }

    // ----------------------------------------------------------------
    // Mode-Wechsel-Helpers
    // ----------------------------------------------------------------

    private void EnterAlert(GuardState guard, GuardCharacter3D view, Cell sightCell)
    {
        guard.LastKnownPlayerCell = sightCell;
        guard.EnterMode(GuardMode.Alert);
        view.SetModeColor(GuardMode.Alert);
        PlayAudio(_detectAudio);
        EmitSignal(SignalName.StatusChanged, "Entdeckt!");
    }

    /// <summary>Spielt einen AudioStreamPlayer nur, wenn ein Stream gesetzt ist (verhindert Godot-Warnings).</summary>
    private static void PlayAudio(AudioStreamPlayer player)
    {
        if (player == null || player.Stream == null) return;
        player.Play();
    }

    private void ReturnToPatrol(GuardState guard, GuardCharacter3D view)
    {
        guard.EnterMode(GuardMode.Patrol);
        view.SetModeColor(GuardMode.Patrol);
        EmitSignal(SignalName.StatusChanged, "Entkommen");
    }

    // ----------------------------------------------------------------
    // Bewegung
    // ----------------------------------------------------------------

    private void StepGuard(GuardState guard, GuardCharacter3D view, Cell next, float speedFactor)
    {
        var dir = GuardNavigator.DirectionTo(guard.CurrentCell, next);
        if (dir.HasValue)
        {
            guard.FacingDirection = dir.Value;
            view.SetFacing(dir.Value);
        }
        guard.CurrentCell = next;

        float duration = 1f / Mathf.Max(0.5f, _playerMoveSpeed * speedFactor);
        view.AnimateToCell(next, duration);
        guard.MoveCooldown = duration;
    }

    private Cell NextPatrolCell(GuardState guard)
    {
        // Auf der zyklischen Route den naechsten Index ansteuern. Wenn die aktuelle Zelle
        // mit dem Index-Eintrag uebereinstimmt, einen Schritt weiter.
        if (guard.CurrentCell == guard.PatrolRoute[guard.PatrolIndex])
            guard.PatrolIndex = (guard.PatrolIndex + 1) % guard.PatrolRoute.Count;
        Cell target = guard.PatrolRoute[guard.PatrolIndex];

        return NextStepWithRepathBudget(guard, target);
    }

    /// <summary>
    /// BFS gegen RepathCooldown abwaegen: wenn Cooldown laeuft, Random-Step als Fallback.
    /// Spart in grossen Mazes signifikant Pathfinding-Kosten bei vielen Guards.
    /// </summary>
    private Cell NextStepWithRepathBudget(GuardState guard, Cell target)
    {
        if (target == null) return null;
        Cell next;
        if (guard.RepathCooldown > 0f)
        {
            // Cooldown aktiv: kein BFS, einfach random offener Nachbar (Heuristik: wir sind eh
            // im naechsten Tick wieder dran).
            next = GuardNavigator.RandomOpenNeighbor(_maze, guard.CurrentCell, _random);
        }
        else
        {
            next = GuardNavigator.NextStepTowards(_maze, guard.CurrentCell, target);
            if (next != null)
                guard.RepathCooldown = RepathCooldown;
            else
                next = GuardNavigator.RandomOpenNeighbor(_maze, guard.CurrentCell, _random);
        }
        return next;
    }

    // ----------------------------------------------------------------
    // Spawn
    // ----------------------------------------------------------------

    private List<Cell> SpawnCandidates(Cell playerStart, int count)
    {
        var result = new List<Cell>();
        if (_maze == null) return result;

        // Sammle Zellen nach Manhattan-Distanz und Eignung. Filter: Mindestabstand.
        var pool = new List<Cell>();
        foreach (var cell in _maze.AllCells())
        {
            if (GuardNavigator.Manhattan(cell, playerStart) < MinSpawnDistanceManhattan) continue;
            // mindestens eine offene Wand muss vorhanden sein, damit Patrol moeglich ist
            int open = 0;
            foreach (var dir in DirectionHelper.All)
                if (!cell.HasWall(dir)) open++;
            if (open == 0) continue;
            pool.Add(cell);
        }

        if (pool.Count == 0) return result;

        // Phase 25: bevorzugt auf BFS-Pfad Start->Goal. Pfad wurde in Start() bereits berechnet.
        if (_solverPathList != null && _solverPathList.Count > 0)
        {
            // Sortiere Pool: Zellen auf/nahe Pfad zuerst.
            pool.Sort((a, b) =>
            {
                int da = MinDistanceTo(a, _solverPathList);
                int db = MinDistanceTo(b, _solverPathList);
                return da.CompareTo(db);
            });
        }
        else
        {
            // Fallback: Shuffle.
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
        }

        // Cluster vermeiden: Mindestabstand zwischen Spawns.
        const int InterSpawnDistance = 5;
        foreach (var cand in pool)
        {
            bool tooClose = false;
            foreach (var taken in result)
            {
                if (GuardNavigator.Manhattan(cand, taken) < InterSpawnDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose) result.Add(cand);
            if (result.Count >= count) break;
        }

        // Falls noch nicht genug (sehr enges Maze): Mindestabstand lockern.
        if (result.Count < count)
        {
            foreach (var cand in pool)
            {
                if (result.Contains(cand)) continue;
                result.Add(cand);
                if (result.Count >= count) break;
            }
        }

        return result;
    }

    private List<Cell> ShortestPathCells(Cell from, Cell to)
    {
        if (from == null || to == null) return null;
        var queue = new Queue<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var seen = new HashSet<Cell> { from };
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c == to) break;
            foreach (var dir in DirectionHelper.All)
            {
                if (c.HasWall(dir)) continue;
                var n = _maze.GetNeighbor(c, dir);
                if (n == null || seen.Contains(n)) continue;
                seen.Add(n);
                cameFrom[n] = c;
                queue.Enqueue(n);
            }
        }
        if (!cameFrom.ContainsKey(to)) return null;
        var path = new List<Cell> { to };
        var step = to;
        while (step != from)
        {
            step = cameFrom[step];
            path.Add(step);
        }
        return path;
    }

    private static int MinDistanceTo(Cell c, List<Cell> path)
    {
        int min = int.MaxValue;
        foreach (var p in path)
        {
            int d = GuardNavigator.Manhattan(c, p);
            if (d < min) min = d;
        }
        return min;
    }

    private void SpawnGuardAt(Cell spawn, int guardId)
    {
        // Off-Path-Bonus an den Routenbuilder durchreichen, damit Patrouillen den
        // Solver-Pfad regelmaessig verlassen und der Spieler Zeitfenster bekommt.
        var route = GuardPatrolRouteBuilder.Build(_maze, spawn, _random, _solverPathSet);
        var state = new GuardState(guardId, spawn) { PatrolRoute = route };
        // Initiale Facing: in Richtung des naechsten Routenpunkts.
        if (route.Count >= 2)
        {
            var d = GuardNavigator.DirectionTo(route[0], route[1]);
            if (d.HasValue) state.FacingDirection = d.Value;
        }

        var view = new GuardCharacter3D { Name = $"Guard{guardId}" };
        _guardsContainer.AddChild(view);
        view.PlaceAtCell(spawn, _cellSize);
        view.SetFacing(state.FacingDirection);
        view.SetModeColor(GuardMode.Patrol);

        _guards.Add(state);
        _views.Add(view);
    }

    private void ClearGuards()
    {
        foreach (var view in _views) view.QueueFree();
        _views.Clear();
        _guards.Clear();
    }
}
