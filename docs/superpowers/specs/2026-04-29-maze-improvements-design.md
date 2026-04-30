# Maze School Project — Improvements Design (Phasen 12–14)

> **Status:** Spec, freigegeben am 2026-04-29.
> **Vorläufer:** [`docs/superpowers/plans/2026-04-28-maze-school-project.md`](../plans/2026-04-28-maze-school-project.md) (Phasen 0–11 abgeschlossen).
> **Nächster Schritt:** Implementierungsplan via `superpowers:writing-plans`.

## Goal

Drei Implementierungslücken aus dem laufenden Projekt schließen, ohne den didaktischen Aufbau (Datenmodell-Schicht / Algorithmen-Schicht / View-Schicht / HUD) aufzubrechen:

1. **Größe bis 1000×1000** — Slider+SpinBox-Steuerung, render-seitig durch 2D-Throttling und 3D-MultiMesh-Refactor abgesichert.
2. **Tempo "Ohne Tempolimit"** — neue Checkbox, neuer `RunMode` im `AlgorithmRunner`, ungebremster Drain in einem Frame.
3. **3D-Kamera-Navigation** — eigene `CameraController3D`-Klasse mit WASD/QE, Shift-Sprint, Pfeiltasten- und RMB-Maus-Look, Mausrad-Dolly, Auto-Fit nach `SetMaze`.

## Architektur

Drei Phasen, die in fester Reihenfolge umgesetzt werden:

| Phase | Inhalt | Begründung der Reihenfolge |
|---|---|---|
| **12** | Große Mazes (UI, 2D-Throttle, 3D-MultiMesh) | Phase 13 und 14 wären ohne sie bei großen Mazes sinnlos. |
| **13** | "Ohne Tempolimit"-Modus | Setzt 12 voraus (sonst wird der ungebremste Drain bei 1000×1000 vom Render-Stack erschlagen). |
| **14** | Kamera-Navigation und Auto-Fit | Setzt 12 voraus (Auto-Fit-Faktor muss für jede Größe stimmen). |

Datei-Touchmatrix:

| Datei | 12 | 13 | 14 |
|---|---|---|---|
| `scenes/Hud.tscn` | Slider→SpinBox-Reihen | + Checkbox | – |
| `scripts/Hud/Hud.cs` | Slider-Range, SpinBox-Wiring | + `UnboundedModeChanged`-Signal | – |
| `scripts/Views/MazeView2D.cs` | Throttled Refresh ab >250×250 | – | – |
| `scripts/Views/MazeView3D.cs` | MultiMesh-Refactor | – | Aufruf `_camera.FitToMaze` |
| `scenes/MazeView3D.tscn` | + 2× MultiMeshInstance3D | – | Camera3D bekommt CameraController3D-Skript |
| `scripts/AlgorithmRunner.cs` | – | RunMode-Enum, Drain-Path | – |
| `scripts/Main.cs` | – | Suppression-Flag, Toggle-Handler | – |
| `scripts/Views/CameraController3D.cs` *(neu)* | – | – | komplett neu |

## Phase 12 — Große Mazes (bis 1000×1000)

### 12.1 HUD: Slider + SpinBox gekoppelt

**`scenes/Hud.tscn`:** In der `Sizes`-`HBoxContainer`-Zeile pro Achse drei Knoten — `Label`, `HSlider`, `SpinBox`.

- Beide neuen `SpinBox`-Knoten heißen `WidthSpinBox` und `HeightSpinBox`.
- `min_value = 5`, `max_value = 1000`, `step = 1`, `allow_greater = false`, `allow_lesser = false`.
- Beide `HSlider`-Knoten bekommen `max_value = 1000` (vorher 75).

**`scripts/Hud/Hud.cs`:**

- Neue Felder `_widthSpinBox`, `_heightSpinBox`.
- `_Ready()` löst die Knoten auf, setzt Min/Max/Step und initial denselben Wert wie der Slider.
- Bidirektionale Kopplung pro Achse:
  - `slider.ValueChanged += v => spin.SetValueNoSignal(v); UpdateLabels();`
  - `spin.ValueChanged += v => slider.SetValueNoSignal(v); UpdateLabels();`
- `UpdateLabels()` liest weiter aus dem Slider; das Label spiegelt den aktuellen Wert.
- `OnGeneratePressed` liest weiter `(int)_widthSlider.Value`.

**Begründung:** Slider als haptisches Werkzeug, SpinBox für exakte Werte. `SetValueNoSignal` verhindert Endlos-Schleifen zwischen den beiden.

### 12.2 2D-View: Throttled Refresh ab >250×250

`MazeView2D.cs` wird um folgende Felder/Methoden erweitert:

```csharp
private const int ThrottleThreshold = 250;
private const double ThrottledRefreshHz = 30.0;

private bool _refreshDirty;
private double _refreshAccumulator;

public void Refresh()
{
    if (_maze == null) return;
    if (_maze.Width <= ThrottleThreshold && _maze.Height <= ThrottleThreshold)
        QueueRedraw();
    else
        _refreshDirty = true;
}

public void ForceRefresh()
{
    _refreshDirty = false;
    _refreshAccumulator = 0;
    QueueRedraw();
}

public override void _Process(double delta)
{
    if (!_refreshDirty) return;
    _refreshAccumulator += delta;
    if (_refreshAccumulator >= 1.0 / ThrottledRefreshHz)
    {
        _refreshAccumulator = 0;
        _refreshDirty = false;
        QueueRedraw();
    }
}
```

`Main.OnGenerationFinished` und `Main.OnSolverFinished` rufen am Ende `_view2D.ForceRefresh()` (zusätzlich zum bestehenden `Refresh()`), damit der finale Zustand auch dann sichtbar wird, wenn der letzte Algorithmus-Schritt im Throttle-Fenster lag.

**Wirkung:** ≤250×250 → frame-genaue Animation wie bisher. >250×250 → maximal 30 Bildaktualisierungen/s, unabhängig von Schrittfrequenz oder Maze-Größe.

### 12.3 3D-View: MultiMeshInstance3D-Refactor

**`scenes/MazeView3D.tscn`:** `WallContainer` (`Node3D`) bleibt, bekommt zwei feste Kinder anstelle der dynamisch erzeugten `MeshInstance3D`-Reihe:

- `WallsHorizontal` (`MultiMeshInstance3D`) — Mesh: `BoxMesh` mit `Size = (CellSize, WallHeight, WallThickness)`, Material: `WallMaterial`.
- `WallsVertical` (`MultiMeshInstance3D`) — Mesh: `BoxMesh` mit `Size = (WallThickness, WallHeight, CellSize)`, Material: `WallMaterial`.

Beide MultiMeshes werden zur Laufzeit über C# konfiguriert: `TransformFormat = Transform3D`, `UseColors = false`, `UseCustomData = false`.

**`scripts/Views/MazeView3D.cs.Rebuild`:**

```csharp
int maxH = maze.Width * maze.Height + maze.Width;   // grobe Obergrenze N+S
int maxV = maze.Width * maze.Height + maze.Height;  // grobe Obergrenze E+W
_horizontal.Multimesh.InstanceCount = maxH;
_vertical.Multimesh.InstanceCount   = maxV;

int hi = 0, vi = 0;
for (int y = 0; y < maze.Height; y++)
for (int x = 0; x < maze.Width;  x++)
{
    var cell = maze.GetCell(x, y);
    if (cell.HasWall(Direction.North))
        _horizontal.Multimesh.SetInstanceTransform(hi++, NorthWallTransform(x, y));
    if (cell.HasWall(Direction.West))
        _vertical.Multimesh.SetInstanceTransform(vi++, WestWallTransform(x, y));
    if (y == maze.Height - 1 && cell.HasWall(Direction.South))
        _horizontal.Multimesh.SetInstanceTransform(hi++, SouthWallTransform(x, y));
    if (x == maze.Width - 1  && cell.HasWall(Direction.East))
        _vertical.Multimesh.SetInstanceTransform(vi++, EastWallTransform(x, y));
}

_horizontal.Multimesh.VisibleInstanceCount = hi;
_vertical.Multimesh.VisibleInstanceCount   = vi;
```

`ClearWalls()` entfällt — wir überschreiben die Transforms beim nächsten Rebuild.

**Wirkung:** Aus O(N²) Knoten + O(N²) Draw-Calls werden 2 Knoten + 2 Draw-Calls. 1000×1000 wird in 3D überhaupt erst rendierbar.

## Phase 13 — Tempo "Ohne Tempolimit"

### 13.1 HUD-Erweiterung

**`scenes/Hud.tscn`:** In `SpeedRow` rechts neben `SpeedSlider` eine `CheckBox "Ohne Tempolimit"` als `UnboundedToggle`.

**`scripts/Hud/Hud.cs`:**

```csharp
[Signal] public delegate void UnboundedModeChangedEventHandler(bool unbounded);

private CheckBox _unboundedToggle = null!;

// in _Ready():
_unboundedToggle = GetNode<CheckBox>("Root/Margin/VBox/SpeedRow/UnboundedToggle");
_unboundedToggle.Toggled += OnUnboundedToggled;

private void OnUnboundedToggled(bool pressed)
{
    _speedSlider.Editable = !pressed;
    UpdateLabels();
    EmitSignal(SignalName.UnboundedModeChanged, pressed);
}
```

`UpdateLabels()` schreibt bei aktivem Toggle `"Tempo: ungebremst"`, sonst wie bisher.

### 13.2 `AlgorithmRunner.RunMode`

```csharp
public enum RunMode { Throttled, Unbounded }
public RunMode Mode { get; set; } = RunMode.Throttled;

public override void _Process(double delta)
{
    if (IsPaused || !IsRunning) return;

    if (Mode == RunMode.Unbounded)
    {
        DrainAllInOneFrame();
        return;
    }

    _accumulator += delta;
    double secondsPerStep = 1.0 / Mathf.Max(1f, StepsPerSecond);
    while (_accumulator >= secondsPerStep && IsRunning)
    {
        _accumulator -= secondsPerStep;
        if (_genIterator != null) { AdvanceGenerator(); continue; }
        if (_solverIterator != null) { AdvanceSolver(); continue; }
    }
}

private void DrainAllInOneFrame()
{
    while (_genIterator    != null) AdvanceGenerator();
    while (_solverIterator != null) AdvanceSolver();
}
```

`AdvanceGenerator/AdvanceSolver` setzen den jeweiligen Iterator beim Ende auf `null` und emittieren das `…Finished`-Signal — die `while`-Schleifen terminieren dadurch automatisch.

### 13.3 Suppressed-Refresh in `Main`

```csharp
private bool _suppressViewRefresh;

private void OnUnboundedModeChanged(bool unbounded)
{
    _suppressViewRefresh = unbounded;
    _runner.Mode = unbounded ? RunMode.Unbounded : RunMode.Throttled;
}

private void OnGenerationStepProduced()
{
    var step = _runner.LastGenerationStep;
    step.Cell.State = step.NewState;
    _tracker.TickStep();
    _tracker.IncrementVisited();
    if (_suppressViewRefresh) return;
    _view2D.Refresh();
    _stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, 0);
}
```

(`OnSolverStepProduced` analog.)

`OnGenerationFinished` und `OnSolverFinished` rufen **immer** das Endrendering und Stats-Update — egal ob Unbounded oder nicht. So sieht der User im Unbounded-Modus zumindest das fertige Maze und die Gesamt-Stoppuhr.

### 13.4 Pause-Verhalten

`Pause` wirkt auf `IsPaused`; im Unbounded-Modus wird der Iterator ohnehin in einem Frame leergedraint, danach ist `IsRunning == false`. Keine Sonderlogik nötig. Die Checkbox bleibt nach einem Run aktiv, damit der User mehrere ungebremste Generierungen hintereinander starten kann, ohne neu zu klicken.

## Phase 14 — 3D-Kamera-Navigation

### 14.1 Neuer `CameraController3D`

`scripts/Views/CameraController3D.cs` erweitert `Camera3D` und ersetzt die statische Camera in `MazeView3D.tscn`. Die Klasse trennt Bewegungslogik vom Maze-Rendering — analog zum bestehenden `AlgorithmRunner`.

```csharp
[Export] public float MoveSpeed             = 8f;
[Export] public float SprintMultiplier      = 2f;
[Export] public float MouseSensitivity      = 0.003f;
[Export] public float KeyTurnSpeed          = 1.5f;   // rad/s
[Export] public float ZoomStep              = 1.5f;
[Export] public float ZoomSprintMultiplier  = 3f;

private float _yaw;
private float _pitch;
private bool  _mouseLook;
```

### 14.2 Bindings

`_Process(double delta)` (kontinuierliche Eingaben):

| Aktion | Taste(n) | Verhalten |
|---|---|---|
| Vorwärts/zurück | W / S | Translation entlang lokaler -Z/+Z, projiziert auf XZ |
| Seitwärts | A / D | Translation entlang lokaler -X/+X, projiziert auf XZ |
| Vertikal | E / Q | Translation in Welt-Y (E hoch, Q runter) |
| Sprint | Shift | `MoveSpeed * SprintMultiplier` während gehalten |
| Look (Pfeile) | ←→↑↓ | Yaw/Pitch über `KeyTurnSpeed * delta` |

Rotation wird zentral als `Basis = Basis.FromEuler(new Vector3(_pitch, _yaw, 0))` gesetzt — nie direkt `RotateY`/`RotateX`, um Gimbal-Drift zu vermeiden.

`_UnhandledInput(InputEvent @event)` (diskrete Eingaben):

| Aktion | Eingabe | Verhalten |
|---|---|---|
| Look-Mode an | RMB drücken | `_mouseLook = true`, `Input.MouseMode = Captured` |
| Look-Mode aus | RMB loslassen | `_mouseLook = false`, `Input.MouseMode = Visible` |
| Maus-Look | `InputEventMouseMotion` während `_mouseLook` | `_yaw -= relative.X * Sensitivity; _pitch -= relative.Y * Sensitivity;` |
| Zoom in | Mausrad hoch | `Translate(Vector3.Forward * step)` |
| Zoom out | Mausrad runter | `Translate(Vector3.Back * step)` |

`_pitch` ist auf `[-1.4, 1.4]` rad geclamped (≈±80°). Zoom-`step` ist `ZoomStep`, multipliziert mit `ZoomSprintMultiplier` falls Shift gehalten — dann macht das Mausrad große Sprünge, was bei 1000×1000 nötig ist, um aus der Vogelperspektive ins Detail zu kommen.

### 14.3 Auto-Fit nach `SetMaze`

`MazeView3D.SetMaze` ruft am Ende `_camera.FitToMaze(maze)`:

```csharp
public void FitToMaze(Model.Maze maze)
{
    float w = maze.Width;
    float h = maze.Height;
    float center_x = w / 2f;
    float center_z = h / 2f;
    float height   = Mathf.Max(w, h) * 0.8f;

    Position = new Vector3(center_x, height, center_z + height * 0.7f);
    LookAt(new Vector3(center_x, 0, center_z), Vector3.Up);

    // Yaw/Pitch aus dem fertigen Look-At zurückrechnen, damit WASD nahtlos übernimmt.
    Vector3 euler = Basis.GetEuler();
    _pitch = euler.X;
    _yaw   = euler.Y;
}
```

**Wirkung:** Egal ob 10×10 oder 1000×1000 — das Maze ist beim Start vollständig im Bild. Schräge Vogelperspektive (~35°) hält den didaktischen Überblick.

### 14.4 Input-Stil

`Input.IsPhysicalKeyPressed(Key.W)` und `InputEventMouseButton`/`InputEventMouseMotion` werden direkt abgefragt — **keine** Input-Map-Aktionen in `project.godot`. Die Bindings sind hartkodiert und für Schüler im Skript sichtbar.

## Edge Cases & Tests

- **Maze 1×1 / 5×5:** Auto-Fit darf nicht durch null teilen; `Mathf.Max(w,h) * 0.8` wird minimum `4` (5×0.8) — okay.
- **Slider/SpinBox-Sync:** `SetValueNoSignal` verhindert Endlosrekursion; manuell mit Tastatur in der SpinBox testen.
- **Throttle-Threshold-Übergang:** Ein Run mit `Width=250, Height=251` schaltet auf Throttle; Endbild muss durch `ForceRefresh` korrekt ankommen.
- **Unbounded während laufendem Run:** Das Toggle wirkt erst beim nächsten Tick; ein laufender throttled Run wird nicht zu Unbounded umgeleitet (`Mode` wird zwar gesetzt, der Run war aber schon im Iterator). Bewusste Vereinfachung.
- **Look-Mode + Alt-Tab:** `MouseMode = Captured` muss in `_Notification(NotificationApplicationFocusOut)` zurück auf `Visible` gesetzt werden, sonst klemmt der Cursor.
- **Maus-Sensitivity bei verschiedenen Auflösungen:** `0.003` ist auf 1080p kalibriert; bei 4K ist die Drehung dadurch schneller. Akzeptabel für Schulprojekt; ggf. später `[Export]`-konfigurierbar belassen.

## Phase 15 — 2D-Pan/Zoom-Steuerung

**Hinzugefügt am 2026-04-30 als Folge-Anforderung.** Bei Mazes >250×250 ist `MazeView2D` ohne Kamerasteuerung praktisch unbrauchbar — die Welt ist 6000×6000 px (bei 24 px/Zelle) bzw. 24000×24000 px bei 1000×1000, größer als jeder Bildschirm. `Camera2D` mit Pan/Zoom-Controller analog zur 3D-Kamera macht das Maze begehbar.

### 15.1 Neuer `CameraController2D`

`scripts/Views/CameraController2D.cs` erweitert `Camera2D` und ersetzt das bisher fehlende Kamera-Setup im 2D-View. Die Klasse trennt Pan-/Zoom-Logik vom Maze-Rendering — analog zum bestehenden `CameraController3D`.

```csharp
[Export] public float PanSpeed             = 800f;   // Welt-px pro Sekunde
[Export] public float SprintMultiplier     = 2f;
[Export] public float ZoomStep             = 1.1f;   // multiplikativ pro Mausrad-Tick
[Export] public float ZoomSprintMultiplier = 1.3f;   // Shift+Wheel macht groessere Spruenge
[Export] public float MinZoom              = 0.01f;
[Export] public float MaxZoom              = 5.0f;

private bool _isPanning;
```

Die Camera2D wird in `MazeView2D.tscn` als Kind ergänzt mit `Current = true`, damit sie das Viewport-Rendering übernimmt.

### 15.2 Bindings

`_Process(double delta)` (kontinuierliche Eingaben):

| Aktion | Taste(n) | Verhalten |
|---|---|---|
| Pan vorwärts/rückwärts | W/S | `Position.Y -= / +=` mit `PanSpeed * delta` |
| Pan seitwärts | A/D | `Position.X -= / +=` mit `PanSpeed * delta` |
| Pan-Backup | ←→↑↓ | Identisch zu A/D/W/S |
| Sprint | Shift | `PanSpeed *= SprintMultiplier` während gehalten |

`_UnhandledInput(InputEvent @event)`:

| Aktion | Eingabe | Verhalten |
|---|---|---|
| Drag-Pan an | RMB drücken | `_isPanning = true` |
| Drag-Pan aus | RMB loslassen | `_isPanning = false` |
| Maus-Pan | `InputEventMouseMotion` während `_isPanning` | `Position -= motion.Relative / Zoom` (Skalierung mit Zoom hält Mausgeschwindigkeit konstant relativ zum sichtbaren Inhalt) |
| Zoom in (Pivot Maus) | Mausrad hoch | `factor = ZoomStep` (oder `*ZoomSprintMultiplier` mit Shift), Pivot-Math siehe unten |
| Zoom out (Pivot Maus) | Mausrad runter | `factor = 1 / ZoomStep` (oder `1 / ZoomSprintMultiplier` mit Shift) |

Pivot-Math für Zoom mit Mausposition als Fixpunkt:

```csharp
Vector2 mouseWorldBefore = GetGlobalMousePosition();
Zoom = (Zoom * factor).Clamp(MinZoom * Vector2.One, MaxZoom * Vector2.One);
Vector2 mouseWorldAfter = GetGlobalMousePosition();
Position += mouseWorldBefore - mouseWorldAfter;
```

Da `GetGlobalMousePosition()` die aktuelle Kameraeinstellung verwendet, liefert der zweite Aufruf nach der Zoom-Änderung den neuen Wert; die Differenz korrigiert `Position` so, dass der Punkt unter dem Cursor stationär bleibt.

`_Notification(NotificationApplicationFocusOut)` setzt `_isPanning = false`, damit der Drag-Modus nicht "klebt", wenn der User Alt-Tab macht.

### 15.3 Auto-Fit nach `SetMaze`

`MazeView2D.SetMaze` ruft am Ende `_camera.FitToMaze(maze)`:

```csharp
public void FitToMaze(Model.Maze maze)
{
    var view = GetParent<MazeView2D>();
    float worldW = maze.Width  * view.CellSizePx;
    float worldH = maze.Height * view.CellSizePx;

    Vector2 viewport = GetViewportRect().Size;
    float zoomFit = Mathf.Min(viewport.X / worldW, viewport.Y / worldH) * 0.9f;
    zoomFit = Mathf.Clamp(zoomFit, MinZoom, MaxZoom);

    Zoom = new Vector2(zoomFit, zoomFit);
    Position = new Vector2(worldW / 2f, worldH / 2f);
}
```

`0.9` lässt 10% Rand. Egal ob 5×5 (worldW=120, zoom~5.4 → clamped auf 5.0) oder 1000×1000 (worldW=24000, zoom~0.027) — das gesamte Maze passt ins Bild.

### 15.4 Edge Cases & Tests (Phase 15)

- **Min-Maze 5×5** → world 120×120, zoom-fit 5.4 wird auf `MaxZoom=5.0` geclampt. Maze bleibt ~21% kleiner als das Viewport — akzeptabel.
- **Max-Maze 1000×1000** → world 24000×24000, zoom-fit ~0.027. Größer als `MinZoom=0.01`, also kein Clamp.
- **Pan-Kollision mit HUD:** Auto-Fit zentriert das Maze im FullView; HUD (CanvasLayer, oberste 220 px) verdeckt einen Teil. Bewusste Akzeptanz — User kann pannen oder Zoom anpassen.
- **Drag-Pan + Alt-Tab:** `_isPanning` wird durch `_Notification(NotificationApplicationFocusOut)` zurückgesetzt.
- **Pre-Init-Zoom/Pan:** Falls der User vor dem ersten `SetMaze` mit dem Mausrad zoomt, hat das nur kosmetischen Effekt — der nächste `FitToMaze` überschreibt es.

### 15.5 Out of Scope (Phase 15)

- Kein HUD-Aware-Auto-Fit (Maze unterhalb der HUD-Höhe zentrieren). User pannt manuell.
- Keine Touch-/Pinch-Gesten.
- Keine Persistenz der Pan/Zoom-Position über `SetMaze`-Aufrufe — jeder neue Run resettet auf Auto-Fit.
- Keine Konfigurierbarkeit der Bindings über `project.godot` Input-Map.

## Edge Cases & Tests (Gesamt — Phasen 12–14)

- **Maze 1×1 / 5×5:** 3D-Auto-Fit darf nicht durch null teilen; `Mathf.Max(w,h) * 0.8` wird minimum `4` (5×0.8) — okay.
- **Slider/SpinBox-Sync:** `SetValueNoSignal` verhindert Endlosrekursion; manuell mit Tastatur in der SpinBox testen.
- **Throttle-Threshold-Übergang:** Ein Run mit `Width=250, Height=251` schaltet auf Throttle; Endbild muss durch `ForceRefresh` korrekt ankommen.
- **Unbounded während laufendem Run:** Das Toggle wirkt erst beim nächsten Tick; ein laufender throttled Run wird nicht zu Unbounded umgeleitet (`Mode` wird zwar gesetzt, der Run war aber schon im Iterator). Bewusste Vereinfachung.
- **Look-Mode + Alt-Tab:** `MouseMode = Captured` muss in `_Notification(NotificationApplicationFocusOut)` zurück auf `Visible` gesetzt werden, sonst klemmt der Cursor.
- **Maus-Sensitivity bei verschiedenen Auflösungen:** `0.003` ist auf 1080p kalibriert; bei 4K ist die Drehung dadurch schneller. Akzeptabel für Schulprojekt; ggf. später `[Export]`-konfigurierbar belassen.

## Out of Scope (Gesamt — Phasen 12–14)

- Keine GPU-basierten Algorithmen — die Generatoren/Solver bleiben C#-Iteratoren.
- Keine MultiMesh-Color-Animation während des Solvens — die 3D-View zeigt nur das fertige Maze, kein animierter Solver in 3D (war nie Teil des Projekts).
- Keine Konfigurierbarkeit der Bindings über `project.godot` Input-Map.
- Keine Persistenz der Kamera-Position über Runs hinweg — jeder neue `SetMaze` resettet auf Auto-Fit.
