# Maze School Project — Verbesserungen (Phasen 12–14) — Implementierungsplan

> **Für agentische Worker:** REQUIRED SUB-SKILL: Verwende `superpowers:subagent-driven-development` (empfohlen) oder `superpowers:executing-plans` zur task-by-task-Umsetzung. Schritte verwenden Checkbox-Syntax (`- [ ]`).
>
> **Für Schüler:** Du kannst die Phasen und Tasks einzeln nacheinander durcharbeiten. Jeder Task ist so aufgebaut, dass er für sich genommen lauffähigen Code produziert (`dotnet build` bricht nicht).
>
> **Vorläufer-Spec:** [`docs/superpowers/specs/2026-04-29-maze-improvements-design.md`](../specs/2026-04-29-maze-improvements-design.md)
>
> **Vorläufer-Plan:** [`docs/superpowers/plans/2026-04-28-maze-school-project.md`](2026-04-28-maze-school-project.md) (Phasen 0–11).

**Goal:** Drei Implementierungsverbesserungen am laufenden Maze-Projekt umsetzen — Maze-Größe bis 1000×1000 (mit Slider+SpinBox-Steuerung und Render-Performance-Refactor), "Ohne Tempolimit"-Modus für ungebremste Erzeugung/Lösung, frei steuerbare 3D-Kamera mit WASD/QE/Shift/Maus/Mausrad und Auto-Fit pro Maze.

**Architecture:** Drei nacheinander umgesetzte Phasen. Phase 12 erweitert die HUD-Steuerung (Slider+SpinBox), führt Throttled-Refresh in `MazeView2D` und MultiMeshInstance3D-Rendering in `MazeView3D` ein. Phase 13 ergänzt im `AlgorithmRunner` einen `RunMode`-Enum mit Drain-in-einem-Frame-Pfad und im HUD eine Checkbox; `Main` unterdrückt View-Updates während des Drains. Phase 14 fügt eine eigene `CameraController3D`-Klasse hinzu, die den `Camera3D`-Knoten in `MazeView3D.tscn` ersetzt; `MazeView3D.SetMaze` ruft `FitToMaze` für korrekte Anfangsposition.

**Tech Stack:**
- Godot 4.6.2 .NET (mono), C# 12 (`<TargetFramework>net8.0</TargetFramework>`)
- Forward+ Renderer mit D3D12
- Windows + PowerShell Workflow gemäß `.github/skills/godot-csharp-windows`
- Godot Executable: `$env:GODOT4` (Fallback `C:\temp\_godot\Godot_v4.6.2-stable_mono_win64.exe`)

**Konventionen (aus dem Vorläuferplan übernommen):**
- Klassendateiname == Klassenname (Godot-C#-Pflicht).
- `public partial class` und `using Godot;` für jedes Godot-Skript.
- Englische Identifier, deutsche Kommentare bei didaktisch wertvollen Stellen.
- `null!` Backing-Field-Pattern für Knoten, die in `_Ready()` aufgelöst werden.
- Nach jedem Hinzufügen oder Umbenennen eines C#-Skripts mit `[Export]` oder `[Signal]`: `& $env:GODOT4 --path $PWD --build-solutions` ausführen.
- Reine Codeänderungen: `dotnet build` reicht.

---

## Phase 12 — Große Mazes (bis 1000×1000)

Ziel: Größenwahl bis 1000×1000 wird im HUD bedienbar; 2D-View bleibt bei großen Mazes flüssig durch Refresh-Throttle ab >250×250; 3D-View rendert auch 1000×1000 korrekt durch Umstieg auf `MultiMeshInstance3D`.

### Task 12.1: HUD — Slider und SpinBox koppeln (Width/Height bis 1000)

**Files:**
- Modify: `scenes/Hud.tscn` (Sizes-Zeile, beide Slider-Bereiche)
- Modify: `scripts/Hud/Hud.cs` (Knotenreferenzen, Range, Sync-Wiring)

- [ ] **Step 1: `scenes/Hud.tscn` — `WidthSpinBox` und `HeightSpinBox` einfügen, Slider-Maxima auf 1000**

Öffne `scenes/Hud.tscn` und ersetze die zwei `Sizes`-Knoten-Blöcke (Slider+Label pro Achse) so, dass nach jedem Slider eine `SpinBox` folgt. Außerdem `max_value` der beiden Slider auf `1000`.

Ersetze den Block für die Width-Reihe (Zeilen mit `WidthLabel` und `WidthSlider`) durch:

```text
[node name="WidthLabel" type="Label" parent="Root/Margin/VBox/Sizes"]
text = "Breite: 25"

[node name="WidthSlider" type="HSlider" parent="Root/Margin/VBox/Sizes"]
min_value = 5.0
max_value = 1000.0
step = 1.0
value = 25.0
size_flags_horizontal = 3

[node name="WidthSpinBox" type="SpinBox" parent="Root/Margin/VBox/Sizes"]
min_value = 5.0
max_value = 1000.0
step = 1.0
value = 25.0
allow_greater = false
allow_lesser = false
custom_minimum_size = Vector2(96, 0)
```

Analog für Height (gleicher Aufbau, alle Werte = 25, alle Bezeichner ersetzen `Width` → `Height`):

```text
[node name="HeightLabel" type="Label" parent="Root/Margin/VBox/Sizes"]
text = "Hoehe: 25"

[node name="HeightSlider" type="HSlider" parent="Root/Margin/VBox/Sizes"]
min_value = 5.0
max_value = 1000.0
step = 1.0
value = 25.0
size_flags_horizontal = 3

[node name="HeightSpinBox" type="SpinBox" parent="Root/Margin/VBox/Sizes"]
min_value = 5.0
max_value = 1000.0
step = 1.0
value = 25.0
allow_greater = false
allow_lesser = false
custom_minimum_size = Vector2(96, 0)
```

> **Hinweis:** Die `SpinBox`-Knoten werden Geschwister der Slider in derselben `Sizes`-`HBoxContainer`-Reihe — kein neuer Container. Reihenfolge im Container nach diesem Edit: `WidthLabel, WidthSlider, WidthSpinBox, HeightLabel, HeightSlider, HeightSpinBox`.

- [ ] **Step 2: `scripts/Hud/Hud.cs` — Felder, `_Ready`-Auflösung, Sync ergänzen**

Im Block der Knotenreferenzen (oberhalb von `public override void _Ready()`):

```csharp
private SpinBox _widthSpinBox = null!;
private SpinBox _heightSpinBox = null!;
```

In `_Ready()` direkt nach den bisherigen Slider-Auflösungen ergänzen:

```csharp
_widthSpinBox = GetNode<SpinBox>("Root/Margin/VBox/Sizes/WidthSpinBox");
_heightSpinBox = GetNode<SpinBox>("Root/Margin/VBox/Sizes/HeightSpinBox");
```

Die beiden Slider-Range-Initialisierungen in `_Ready()` müssen den neuen Maximalwert spiegeln. Ersetze die bestehenden Blöcke:

```csharp
_widthSlider.MinValue = 5;
_widthSlider.MaxValue = 1000;
_widthSlider.Step = 1;
_widthSlider.Value = 25;

_widthSpinBox.MinValue = 5;
_widthSpinBox.MaxValue = 1000;
_widthSpinBox.Step = 1;
_widthSpinBox.Value = 25;

_heightSlider.MinValue = 5;
_heightSlider.MaxValue = 1000;
_heightSlider.Step = 1;
_heightSlider.Value = 25;

_heightSpinBox.MinValue = 5;
_heightSpinBox.MaxValue = 1000;
_heightSpinBox.Step = 1;
_heightSpinBox.Value = 25;
```

Im Signal-Wiring-Block die alten Lambdas für `_widthSlider.ValueChanged` / `_heightSlider.ValueChanged` ersetzen durch bidirektionale Sync-Routinen:

```csharp
_widthSlider.ValueChanged += v =>
{
    // SetValueNoSignal verhindert eine Endlosschleife zurueck zum Slider.
    _widthSpinBox.SetValueNoSignal(v);
    UpdateLabels();
};
_widthSpinBox.ValueChanged += v =>
{
    _widthSlider.SetValueNoSignal(v);
    UpdateLabels();
};

_heightSlider.ValueChanged += v =>
{
    _heightSpinBox.SetValueNoSignal(v);
    UpdateLabels();
};
_heightSpinBox.ValueChanged += v =>
{
    _heightSlider.SetValueNoSignal(v);
    UpdateLabels();
};
```

> **Wichtig:** `OnGeneratePressed` und `UpdateLabels` lesen weiterhin aus `_widthSlider.Value` / `_heightSlider.Value` — keine Änderung dort nötig, weil Slider und SpinBox immer synchron sind.

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Manuell testen**

```powershell
& $env:GODOT4 --path $PWD
```

Prüfe im laufenden Editor (Play-Button):
1. Slider und SpinBox stehen in einer Reihe.
2. Slider ziehen → SpinBox folgt synchron.
3. SpinBox-Pfeile drücken → Slider folgt synchron.
4. Beim Klick auf SpinBox-Textfeld lässt sich `1000` per Tastatur eintippen.
5. `Erstellen` mit 1000×1000 → das Ergebnis ist beim aktuellen Render-Stand absichtlich noch unbrauchbar (Throttle und MultiMesh kommen in 12.2 / 12.3). Wichtig: keine Exception, der Generator läuft an.

- [ ] **Step 5: Commit**

```bash
git add scenes/Hud.tscn scripts/Hud/Hud.cs
git commit -m "Task 12.1: HUD-Groessenwahl mit gekoppelter SpinBox bis 1000"
```

### Task 12.2: 2D-View — Throttled Refresh ab >250×250

**Files:**
- Modify: `scripts/Views/MazeView2D.cs`
- Modify: `scripts/Main.cs`

- [ ] **Step 1: `MazeView2D.cs` — Throttle-Felder, `Refresh`/`ForceRefresh`/`_Process`**

Ersetze die bestehende `Refresh`-Methode und ergänze direkt darunter `ForceRefresh` und `_Process`. Direkt unter den existierenden statischen Farben (`HeatmapMax`) zwei Konstanten und zwei Felder hinzufügen:

```csharp
// Ab dieser Maze-Groesse pro Achse schaltet die View vom Pro-Schritt-Refresh
// auf zeitbasiertes Throttling um. Animation bleibt fluessig, aber die Anzahl
// der Neuzeichnungen ist von der Schrittfrequenz entkoppelt.
private const int ThrottleThreshold = 250;
private const double ThrottledRefreshHz = 30.0;

private bool _refreshDirty;
private double _refreshAccumulator;
```

Ersetze `public void Refresh() => QueueRedraw();` durch:

```csharp
/// <summary>
/// Wird nach jedem Algorithmus-Schritt aufgerufen. Bei kleinen Mazes loest die
/// Methode sofort eine Neuzeichnung aus, bei grossen Mazes nur ein Dirty-Flag,
/// das im naechsten _Process zeitbasiert eingeloest wird.
/// </summary>
public void Refresh()
{
    if (_maze == null) return;
    if (_maze.Width <= ThrottleThreshold && _maze.Height <= ThrottleThreshold)
        QueueRedraw();
    else
        _refreshDirty = true;
}

/// <summary>
/// Erzwingt eine sofortige Neuzeichnung, unabhaengig vom Throttling.
/// Wird am Ende von Generierung/Loesung verwendet, damit der Endzustand
/// sicher sichtbar ist.
/// </summary>
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

> **Wichtig:** Falls `_Process` schon existiert, die obigen Zeilen in den vorhandenen Body einsetzen. Aktuell hat `MazeView2D` kein `_Process` — der Block oben ist neu.

- [ ] **Step 2: `scripts/Main.cs` — `ForceRefresh` am Ende von Generierung/Loesung**

In `OnGenerationFinished` ist bereits ein `_view2D.Refresh();` vorhanden. Ersetze diese Zeile durch:

```csharp
_view2D.ForceRefresh();
```

In `OnSolverFinished` ist ebenfalls `_view2D.Refresh();` vorhanden. Ersetze ebenfalls durch:

```csharp
_view2D.ForceRefresh();
```

> Begründung: Bei großen Mazes lag der letzte Schritt eventuell innerhalb des Throttle-Fensters und löst nur das Dirty-Flag aus. `ForceRefresh` zeichnet garantiert sofort.

- [ ] **Step 3: Build prüfen**

```powershell
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Manuell testen — kleines Maze (animiert)**

```powershell
& $env:GODOT4 --path $PWD
```

Im Editor mit `25×25` und `Recursive Backtracker` generieren. Erwartet: Animation pro Schritt sichtbar wie bisher (kein Throttling unter dem Schwellwert).

- [ ] **Step 5: Manuell testen — großes Maze (throttled)**

In derselben laufenden Instanz `300×300` einstellen, `Recursive Backtracker` starten. Erwartet:
1. Frame-Rate bleibt stabil (kein Einbruch).
2. Animation ist sichtbar, aber maximal 30 Hz (sichtbar weniger zappelig als bei 25×25).
3. Endzustand zeigt das vollständig generierte Maze.

- [ ] **Step 6: Commit**

```bash
git add scripts/Views/MazeView2D.cs scripts/Main.cs
git commit -m "Task 12.2: Throttled Refresh in MazeView2D ab >250x250"
```

### Task 12.3: 3D-View — MultiMeshInstance3D-Refactor

**Files:**
- Modify: `scenes/MazeView3D.tscn` (zwei `MultiMeshInstance3D` als Kinder von `WallContainer`)
- Modify: `scripts/Views/MazeView3D.cs` (Rebuild auf MultiMesh umstellen)

- [ ] **Step 1: `scenes/MazeView3D.tscn` — `WallsHorizontal` und `WallsVertical` einfügen**

Öffne `scenes/MazeView3D.tscn`. Aktuell endet die Datei mit dem `WallContainer`-Knoten. Ergänze danach **innerhalb** desselben `WallContainer` zwei MultiMeshInstance3D-Kinder. Erweitere zuerst den Header, damit drei Sub-Resources Platz haben:

Ersetze die erste Zeile

```text
[gd_scene load_steps=2 format=3 uid="uid://b0maze0view3d"]
```

durch

```text
[gd_scene load_steps=5 format=3 uid="uid://b0maze0view3d"]
```

Füge **vor** dem `[node name="MazeView3D" ...]`-Block (nach dem `[ext_resource ...]`) folgende vier Sub-Resource-Blöcke ein:

```text
[sub_resource type="BoxMesh" id="BoxMesh_horizontal"]
size = Vector3(1, 1.4, 0.1)

[sub_resource type="BoxMesh" id="BoxMesh_vertical"]
size = Vector3(0.1, 1.4, 1)

[sub_resource type="MultiMesh" id="MultiMesh_horizontal"]
transform_format = 1
mesh = SubResource("BoxMesh_horizontal")

[sub_resource type="MultiMesh" id="MultiMesh_vertical"]
transform_format = 1
mesh = SubResource("BoxMesh_vertical")
```

> **Wichtig:** `transform_format = 1` entspricht `MultiMesh.TransformFormat.Transform3D` (= 3D-Transformations-Matrix pro Instanz). Ohne diesen Wert defaultet das auf 2D-Transforms und der Aufbau funktioniert nicht.

Ergänze danach am Ende der Datei (unter dem bisherigen `WallContainer`-Knoten) zwei neue Knoten als Kinder von `WallContainer`:

```text
[node name="WallsHorizontal" type="MultiMeshInstance3D" parent="WallContainer"]
multimesh = SubResource("MultiMesh_horizontal")

[node name="WallsVertical" type="MultiMeshInstance3D" parent="WallContainer"]
multimesh = SubResource("MultiMesh_vertical")
```

- [ ] **Step 2: `scripts/Views/MazeView3D.cs` — Komplettes Rewrite des `Rebuild`-Pfades**

Ersetze den **kompletten Klassenkörper** von `MazeView3D` (also alles innerhalb der `class`-Klammern) durch:

```csharp
[Export] public float CellSize = 1.0f;
[Export] public float WallHeight = 1.4f;
[Export] public float WallThickness = 0.1f;

private Node3D _wallContainer = null!;
private MeshInstance3D _floor = null!;
private MultiMeshInstance3D _wallsHorizontal = null!;
private MultiMeshInstance3D _wallsVertical = null!;
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
    _wallsHorizontal = GetNode<MultiMeshInstance3D>("WallContainer/WallsHorizontal");
    _wallsVertical = GetNode<MultiMeshInstance3D>("WallContainer/WallsVertical");

    // Material zuweisen - die in der .tscn voreingestellten BoxMeshes haben bewusst kein Material,
    // damit die Farbe zentral hier gesetzt werden kann.
    _wallsHorizontal.MaterialOverride = WallMaterial;
    _wallsVertical.MaterialOverride = WallMaterial;
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
    // Maximalkapazitaet grosszuegig dimensionieren: jede Zelle kann ihre Nord/West-Wand
    // beitragen, plus jeweils eine Reihe Sued- und Ost-Wand am Rand.
    int maxHorizontal = maze.Width * maze.Height + maze.Width;
    int maxVertical = maze.Width * maze.Height + maze.Height;

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

private Transform3D HorizontalWallTransform(float centerX, float centerZ) =>
    new(Basis.Identity, new Vector3(centerX, WallHeight / 2f, centerZ));

private Transform3D VerticalWallTransform(float centerX, float centerZ) =>
    new(Basis.Identity, new Vector3(centerX, WallHeight / 2f, centerZ));
```

> **Hinweis:** Das `using` im Datei-Kopf bleibt unverändert (`using Godot;` und `using Maze.Model;`). Die alte `AddWall`-Methode und `ClearWalls` entfallen ersatzlos.

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Manuell testen — kleines 3D-Maze**

```powershell
& $env:GODOT4 --path $PWD
```

Mit `25×25 Recursive Backtracker` generieren, dann `3D-Ansicht` aktivieren. Erwartet:
1. Wände sichtbar mit korrekter Farbe (`#dcdcdc`).
2. Boden sichtbar in dunkelgrau.
3. Anzahl der Wände visuell plausibel (Maze ist begrenzt, nicht offen).

- [ ] **Step 5: Manuell testen — großes 3D-Maze**

Im selben Run `500×500 Recursive Backtracker` (im Unbounded-Modus aus Phase 13 würde das schneller gehen — hier reicht aber, mit hohem Tempo zu starten und kurz zu warten). Sobald das 2D fertig ist, `3D-Ansicht` aktivieren.

Erwartet:
1. Editor friert nicht ein.
2. 3D-Ansicht baut die Wände in unter ~1 s auf.
3. FPS bleibt im Editor flüssig (≥30).

> Falls die Anzeige der `Camera3D` unten aus dem Bild ragt, ist das hier zu erwarten — der Auto-Fit kommt erst in Phase 14.

- [ ] **Step 6: Commit**

```bash
git add scenes/MazeView3D.tscn scripts/Views/MazeView3D.cs
git commit -m "Task 12.3: MazeView3D auf MultiMeshInstance3D umgestellt"
```

---

## Phase 13 — Tempo "Ohne Tempolimit"

Ziel: Eine Checkbox im HUD aktiviert einen ungebremsten Modus. `AlgorithmRunner` drained den Iterator dann komplett in einem Frame; `Main` unterdrückt während des Drains die View- und Stats-Updates und macht am Ende eine einzige Aktualisierung.

### Task 13.1: `AlgorithmRunner.RunMode` mit Drain-in-einem-Frame

**Files:**
- Modify: `scripts/AlgorithmRunner.cs`

- [ ] **Step 1: `RunMode`-Enum, `Mode`-Property, neuer `_Process`-Pfad**

Ergänze direkt unter den vier `[Signal]`-Zeilen (vor `public float StepsPerSecond ...`):

```csharp
/// <summary>
/// Throttled = Schritte werden ueber StepsPerSecond getaktet.
/// Unbounded = Schritte werden in einem Frame komplett abgearbeitet.
/// </summary>
public enum RunMode
{
    Throttled,
    Unbounded
}

public RunMode Mode { get; set; } = RunMode.Throttled;
```

Ersetze den vollständigen `_Process`-Body durch:

```csharp
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

/// <summary>
/// Im Unbounded-Modus aufgerufen: drained den aktuellen Iterator komplett in einem Frame.
/// AdvanceGenerator/AdvanceSolver setzen den Iterator beim Ende selbst auf null und
/// emittieren das jeweilige Finished-Signal - dadurch terminieren die while-Schleifen.
/// </summary>
private void DrainAllInOneFrame()
{
    while (_genIterator != null)
        AdvanceGenerator();
    while (_solverIterator != null)
        AdvanceSolver();
}
```

- [ ] **Step 2: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add scripts/AlgorithmRunner.cs
git commit -m "Task 13.1: AlgorithmRunner.RunMode mit Drain-in-einem-Frame"
```

### Task 13.2: HUD-Checkbox "Ohne Tempolimit"

**Files:**
- Modify: `scenes/Hud.tscn` (CheckBox in der `SpeedRow`)
- Modify: `scripts/Hud/Hud.cs` (Knotenreferenz, Signal, Toggle-Handler)

- [ ] **Step 1: `scenes/Hud.tscn` — `UnboundedToggle` in `SpeedRow` ergänzen**

Direkt nach dem `[node name="SpeedSlider" ...]`-Block in `SpeedRow` einfügen:

```text
[node name="UnboundedToggle" type="CheckBox" parent="Root/Margin/VBox/SpeedRow"]
text = "Ohne Tempolimit"
```

> **Reihenfolge in `SpeedRow`** nach diesem Edit: `SpeedLabel, SpeedSlider, UnboundedToggle`.

- [ ] **Step 2: `scripts/Hud/Hud.cs` — Signal, Feld, Wiring, Label-Anpassung**

Ergänze unter den anderen `[Signal]`-Zeilen:

```csharp
[Signal] public delegate void UnboundedModeChangedEventHandler(bool unbounded);
```

Ergänze unter den anderen privaten Knotenreferenzen:

```csharp
private CheckBox _unboundedToggle = null!;
```

In `_Ready()` direkt nach `_speedLabel = ...`:

```csharp
_unboundedToggle = GetNode<CheckBox>("Root/Margin/VBox/SpeedRow/UnboundedToggle");
```

Im Signal-Wiring-Block ergänzen (z. B. nach `_speedSlider.ValueChanged += OnSpeedChanged;`):

```csharp
_unboundedToggle.Toggled += OnUnboundedToggled;
```

Ersetze den bestehenden `UpdateLabels`-Body durch:

```csharp
private void UpdateLabels()
{
    _widthLabel.Text = $"Breite:  {(int)_widthSlider.Value}";
    _heightLabel.Text = $"Hoehe:    {(int)_heightSlider.Value}";

    if (_unboundedToggle != null && _unboundedToggle.ButtonPressed)
        _speedLabel.Text = "Tempo:  ungebremst";
    else
        _speedLabel.Text = $"Tempo:  {(int)_speedSlider.Value} Schritte/s";
}
```

> Begründung Null-Check: `UpdateLabels` wird in `_Ready` aufgerufen, bevor `_unboundedToggle` zugewiesen ist — der Null-Check verhindert die NRE im allerersten Frame.

Ergänze einen neuen Handler unter `OnSpeedChanged`:

```csharp
private void OnUnboundedToggled(bool pressed)
{
    // Slider visuell ausgrauen, damit klar ist, dass StepsPerSecond ignoriert wird.
    _speedSlider.Editable = !pressed;
    UpdateLabels();
    EmitSignal(SignalName.UnboundedModeChanged, pressed);
}
```

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add scenes/Hud.tscn scripts/Hud/Hud.cs
git commit -m "Task 13.2: HUD-Checkbox 'Ohne Tempolimit'"
```

### Task 13.3: `Main` — Suppression-Flag und Toggle-Handler verkabeln

**Files:**
- Modify: `scripts/Main.cs`

- [ ] **Step 1: Feld, Subscription, Handler ergänzen**

Ergänze direkt unter dem letzten `private`-Feld in `Main` (nach `private readonly PerformanceTracker _tracker = new();`):

```csharp
private bool _suppressViewRefresh;
```

In `_Ready()` direkt nach `_hud.HeatmapToggle += OnHeatmapToggled;`:

```csharp
_hud.UnboundedModeChanged += OnUnboundedModeChanged;
```

Ergänze einen neuen Handler unten in der Klasse (z. B. direkt nach `OnHeatmapToggled`):

```csharp
private void OnUnboundedModeChanged(bool unbounded)
{
    _suppressViewRefresh = unbounded;
    _runner.Mode = unbounded ? AlgorithmRunner.RunMode.Unbounded : AlgorithmRunner.RunMode.Throttled;
}
```

- [ ] **Step 2: `OnGenerationStepProduced` umbauen**

Ersetze den vollständigen Body von `OnGenerationStepProduced` durch:

```csharp
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
```

- [ ] **Step 3: `OnSolverStepProduced` umbauen**

Ersetze den vollständigen Body von `OnSolverStepProduced` durch:

```csharp
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
```

> **Hinweis:** `OnGenerationFinished` und `OnSolverFinished` rufen unverändert `Refresh()` / `ForceRefresh()` und `UpdateStats(...)` auf — sie laufen immer, auch im Unbounded-Modus. Dort wird also das Endbild und die finale Stoppuhr garantiert aktualisiert.

- [ ] **Step 4: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 5: Manuell testen — Toggle aus (bisheriges Verhalten)**

```powershell
& $env:GODOT4 --path $PWD
```

Im Editor: `25×25 Recursive Backtracker` mit Standard-Tempo. Erwartet: Animation pro Schritt sichtbar, Stoppuhr läuft mit, alles wie vorher.

- [ ] **Step 6: Manuell testen — Toggle an (Drain in einem Frame)**

In derselben Instanz `Ohne Tempolimit` aktivieren, dann `1000×1000 Recursive Backtracker` `Erstellen`. Erwartet:
1. UI friert kurz ein (sichtbar als Stillstand der HUD-Animation).
2. Nach wenigen Sekunden zeigt das 2D-Bild das fertige Maze.
3. `Stoppuhr` zeigt eine plausible Gesamtzeit (z. B. mehrere hundert ms bis wenige Sekunden, je nach CPU).
4. `Schritte` und `Besucht` enthalten realistische Werte (nahe 1.000.000).

`Loesen` mit `BFS` oder `A*` analog testen.

- [ ] **Step 7: Commit**

```bash
git add scripts/Main.cs
git commit -m "Task 13.3: Main verkabelt Unbounded-Toggle und Suppression"
```

---

## Phase 14 — 3D-Kamera-Navigation

Ziel: Eigene `CameraController3D`-Klasse ersetzt die statische `Camera3D` in `MazeView3D.tscn`. WASD bewegt horizontal, QE vertikal, Shift verdoppelt die Geschwindigkeit, RMB+Maus dreht (FPS-Stil), Pfeiltasten dienen als Maus-Backup, Mausrad zoomt per Dolly. `MazeView3D.SetMaze` ruft `FitToMaze`, damit das gesamte Maze zu Beginn im Bild ist.

### Task 14.1: `CameraController3D` — Skelett und `[Export]`-Felder

**Files:**
- Create: `scripts/Views/CameraController3D.cs`
- Modify: `scenes/MazeView3D.tscn` (Camera3D bekommt das Skript zugewiesen)

- [ ] **Step 1: `scripts/Views/CameraController3D.cs` anlegen**

```csharp
using Godot;

namespace Maze.Views;

/// <summary>
/// Frei steuerbare 3D-Kamera fuer die Maze-Ansicht.
/// Bewegung: WASD horizontal in Blickrichtung, QE vertikal in Welt-Y,
/// Shift verdoppelt die Geschwindigkeit. Drehung: RMB + Maus oder Pfeiltasten.
/// Zoom: Mausrad als Dolly entlang der Blickrichtung.
/// </summary>
public partial class CameraController3D : Camera3D
{
    [Export] public float MoveSpeed = 8f;
    [Export] public float SprintMultiplier = 2f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float KeyTurnSpeed = 1.5f;        // rad/s fuer Pfeiltasten
    [Export] public float ZoomStep = 1.5f;
    [Export] public float ZoomSprintMultiplier = 3f;

    // Yaw/Pitch werden separat gefuehrt, damit die Rotation immer aus
    // Basis.FromEuler aufgebaut wird (kein Gimbal-Drift).
    private float _yaw;
    private float _pitch;
    private bool _mouseLook;

    public override void _Ready()
    {
        // Initialwerte aus dem aktuellen Transform ziehen, damit auch ohne FitToMaze
        // ein konsistenter Startzustand existiert.
        Vector3 euler = Basis.GetEuler();
        _pitch = euler.X;
        _yaw = euler.Y;
    }
}
```

- [ ] **Step 2: `scenes/MazeView3D.tscn` — Skript an Camera3D hängen**

Öffne `scenes/MazeView3D.tscn`. Aktuell:

```text
[ext_resource type="Script" path="res://scripts/Views/MazeView3D.cs" id="1_view3d"]
```

Ergänze direkt darunter:

```text
[ext_resource type="Script" path="res://scripts/Views/CameraController3D.cs" id="2_camctl"]
```

Erhöhe `load_steps` im Header um 1 (aktuell `5` nach Task 12.3 — also `6`):

```text
[gd_scene load_steps=6 format=3 uid="uid://b0maze0view3d"]
```

Ändere den Camera3D-Knoten so, dass er das Skript bekommt. Aktuell:

```text
[node name="Camera3D" type="Camera3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 0.7071, 0.7071, 0, -0.7071, 0.7071, 12, 25, 12)
fov = 60.0
```

Ersetze durch:

```text
[node name="Camera3D" type="Camera3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 0.7071, 0.7071, 0, -0.7071, 0.7071, 12, 25, 12)
fov = 60.0
script = ExtResource("2_camctl")
```

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add scripts/Views/CameraController3D.cs scenes/MazeView3D.tscn
git commit -m "Task 14.1: CameraController3D-Skelett, an Camera3D gehaengt"
```

### Task 14.2: Bewegung — WASD horizontal, QE vertikal, Shift-Sprint

**Files:**
- Modify: `scripts/Views/CameraController3D.cs`

- [ ] **Step 1: `_Process` mit Bewegung ergänzen**

Ergänze in `CameraController3D` direkt unter `_Ready`:

```csharp
public override void _Process(double delta)
{
    HandleMovement(delta);
    HandleKeyboardLook(delta);
    ApplyRotation();
}

private void HandleMovement(double delta)
{
    // Eingabe als 3D-Richtungsvektor aufbauen:
    //   Vorwaerts/zurueck (W/S)  -> lokale -Z/+Z (forward/back)
    //   Seitwaerts (A/D)         -> lokale -X/+X (left/right)
    //   Vertikal (Q/E)            -> Welt-Y (E hoch, Q runter)
    Vector3 input = Vector3.Zero;
    if (Input.IsPhysicalKeyPressed(Key.W)) input += Vector3.Forward;
    if (Input.IsPhysicalKeyPressed(Key.S)) input += Vector3.Back;
    if (Input.IsPhysicalKeyPressed(Key.A)) input += Vector3.Left;
    if (Input.IsPhysicalKeyPressed(Key.D)) input += Vector3.Right;

    // QE bewegen sich entlang Welt-Y, unabhaengig vom Pitch der Kamera.
    Vector3 worldVertical = Vector3.Zero;
    if (Input.IsPhysicalKeyPressed(Key.E)) worldVertical += Vector3.Up;
    if (Input.IsPhysicalKeyPressed(Key.Q)) worldVertical += Vector3.Down;

    if (input == Vector3.Zero && worldVertical == Vector3.Zero)
        return;

    float speed = MoveSpeed;
    if (Input.IsPhysicalKeyPressed(Key.Shift))
        speed *= SprintMultiplier;

    // Horizontaler Anteil: in Kamera-Lokalkoordinaten transformieren.
    if (input != Vector3.Zero)
    {
        input = input.Normalized();
        Translate(input * speed * (float)delta);
    }

    // Vertikaler Anteil: direkt in Weltkoordinaten addieren.
    if (worldVertical != Vector3.Zero)
        Position += worldVertical.Normalized() * speed * (float)delta;
}

private void HandleKeyboardLook(double delta)
{
    // Pfeiltasten als Maus-Backup. Funktioniert immer, auch ohne RMB.
    float yawDelta = 0f;
    float pitchDelta = 0f;
    if (Input.IsPhysicalKeyPressed(Key.Left))  yawDelta   += KeyTurnSpeed * (float)delta;
    if (Input.IsPhysicalKeyPressed(Key.Right)) yawDelta   -= KeyTurnSpeed * (float)delta;
    if (Input.IsPhysicalKeyPressed(Key.Up))    pitchDelta += KeyTurnSpeed * (float)delta;
    if (Input.IsPhysicalKeyPressed(Key.Down))  pitchDelta -= KeyTurnSpeed * (float)delta;
    _yaw += yawDelta;
    _pitch = Mathf.Clamp(_pitch + pitchDelta, -1.4f, 1.4f);
}

private void ApplyRotation()
{
    // Rotation immer komplett aus Yaw/Pitch aufbauen, nicht inkrementell.
    Basis = Basis.FromEuler(new Vector3(_pitch, _yaw, 0));
}
```

> **Hinweis:** `Vector3.Forward` ist in Godot `(0, 0, -1)`, deshalb erhoeht `W → Vector3.Forward` die lokale -Z-Position via `Translate` korrekt = vorwaerts.

- [ ] **Step 2: Build prüfen**

```powershell
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 3: Manuell testen**

```powershell
& $env:GODOT4 --path $PWD
```

Maze `25×25 Recursive Backtracker` generieren, `3D-Ansicht` aktivieren, ins 3D-Viewport klicken (Fokus). Erwartet:
1. `W/S` bewegen die Kamera vor/zurück.
2. `A/D` bewegen sie seitlich.
3. `E` hebt sie an, `Q` senkt sie.
4. `Shift` gehalten verdoppelt die Geschwindigkeit.
5. Pfeiltasten drehen die Kamera (links/rechts = Yaw, hoch/runter = Pitch).
6. Pitch ist nach oben/unten begrenzt — kein Überschlag.

> Falls beim Pfeiltasten-Drücken die Kamera "wegspringt": liegt am Yaw/Pitch-Init in `_Ready` (siehe Task 14.1). Falls nötig, einmal Editor neu starten.

- [ ] **Step 4: Commit**

```bash
git add scripts/Views/CameraController3D.cs
git commit -m "Task 14.2: Kamera-Bewegung WASD/QE/Shift mit Pfeiltasten-Look"
```

### Task 14.3: Maus-Look (RMB) und Mausrad-Zoom

**Files:**
- Modify: `scripts/Views/CameraController3D.cs`

- [ ] **Step 1: `_UnhandledInput` ergänzen**

Ergänze direkt unter `ApplyRotation`:

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    // RMB druecken/loslassen schaltet Mouse-Look an/aus.
    if (@event is InputEventMouseButton mb)
    {
        if (mb.ButtonIndex == MouseButton.Right)
        {
            _mouseLook = mb.Pressed;
            Input.MouseMode = mb.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
            return;
        }

        // Mausrad als Dolly: bewegt die Kamera entlang der lokalen Forward-Achse.
        if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
        {
            float step = ZoomStep;
            if (Input.IsPhysicalKeyPressed(Key.Shift))
                step *= ZoomSprintMultiplier;

            // WheelUp = naeher heran (vorwaerts), WheelDown = weiter weg (rueckwaerts).
            Vector3 direction = mb.ButtonIndex == MouseButton.WheelUp ? Vector3.Forward : Vector3.Back;
            Translate(direction * step);
            return;
        }
    }

    // Maus-Bewegung im Look-Modus aendert Yaw/Pitch.
    if (@event is InputEventMouseMotion motion && _mouseLook)
    {
        _yaw -= motion.Relative.X * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.4f, 1.4f);
    }
}
```

- [ ] **Step 2: Mouse-Capture beim Fenster-Defokus zuruecknehmen**

Ergänze direkt unter `_UnhandledInput`:

```csharp
public override void _Notification(int what)
{
    // Wenn das Fenster den Fokus verliert (Alt-Tab), den Cursor freigeben,
    // sonst klemmt die Maus unsichtbar im Spielbereich.
    if (what == NotificationApplicationFocusOut && _mouseLook)
    {
        _mouseLook = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}
```

- [ ] **Step 3: Build prüfen**

```powershell
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Manuell testen**

```powershell
& $env:GODOT4 --path $PWD
```

Maze `25×25 Recursive Backtracker` generieren, `3D-Ansicht` aktivieren. Erwartet:
1. Rechte Maustaste gedrückt halten → Cursor verschwindet, Maus-Bewegung dreht die Kamera (rechts → Kamera schaut rechts; runter → Kamera schaut runter).
2. RMB loslassen → Cursor wird wieder sichtbar.
3. Pitch begrenzt nach oben/unten (kein Überschlag).
4. Mausrad nach oben → Kamera bewegt sich vorwärts.
5. Mausrad nach unten → Kamera bewegt sich rückwärts.
6. `Shift` + Mausrad → größere Sprünge pro Tick.
7. Während RMB gehalten: Mausrad funktioniert weiterhin.
8. Alt-Tab raus aus Godot → bei Rückkehr ist die Maus sichtbar (kein eingefangener Cursor).

- [ ] **Step 5: Commit**

```bash
git add scripts/Views/CameraController3D.cs
git commit -m "Task 14.3: Maus-Look (RMB) und Mausrad-Zoom (Dolly)"
```

### Task 14.4: `FitToMaze` — Auto-Fit nach `SetMaze`

**Files:**
- Modify: `scripts/Views/CameraController3D.cs` (FitToMaze-Methode)
- Modify: `scripts/Views/MazeView3D.cs` (SetMaze ruft FitToMaze)

- [ ] **Step 1: `CameraController3D.FitToMaze` ergänzen**

Ergänze in `CameraController3D` direkt unter `_Notification`:

```csharp
/// <summary>
/// Setzt die Kamera so, dass das gesamte Maze von schraeg oben sichtbar ist.
/// Hoehe skaliert mit der groesseren Achse, Z-Versatz sorgt fuer einen sanften
/// Schraegblick auf die Mitte.
/// </summary>
public void FitToMaze(Model.Maze maze)
{
    float w = maze.Width;
    float h = maze.Height;
    float centerX = w / 2f;
    float centerZ = h / 2f;
    float height = Mathf.Max(w, h) * 0.8f;

    Position = new Vector3(centerX, height, centerZ + height * 0.7f);
    LookAt(new Vector3(centerX, 0, centerZ), Vector3.Up);

    // Yaw/Pitch aus dem fertigen Look-At zurueckrechnen, damit die WASD-Steuerung
    // direkt vom Auto-Fit-Zustand uebernimmt - sonst wuerde der erste Tastendruck
    // die Kamera zurueck in den alten Winkel reissen.
    Vector3 euler = Basis.GetEuler();
    _pitch = euler.X;
    _yaw = euler.Y;
}
```

> **Wichtig:** Direkt am Datei-Anfang muss `using Maze.Model;` ergänzt werden, falls noch nicht vorhanden — die Methode referenziert den Maze-Typ aus diesem Namespace.

Aktualisiere den `using`-Block am Datei-Anfang. Aktuell nur `using Godot;`. Ergänze:

```csharp
using Godot;
using Maze.Model;
```

- [ ] **Step 2: `MazeView3D.SetMaze` ruft `FitToMaze`**

In `scripts/Views/MazeView3D.cs` ergänze in `_Ready` (zusätzlich zu den Knotenreferenzen aus Task 12.3) eine Camera-Referenz:

```csharp
private CameraController3D _camera = null!;
```

Direkt nach den anderen Knotenauflösungen in `_Ready`:

```csharp
_camera = GetNode<CameraController3D>("Camera3D");
```

Aktualisiere `SetMaze` so, dass die Kamera nach dem Rebuild auf das neue Maze ausgerichtet wird:

```csharp
public void SetMaze(Model.Maze maze)
{
    _maze = maze;
    Rebuild();
    _camera.FitToMaze(maze);
}
```

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Manuell testen — Auto-Fit bei verschiedenen Größen**

```powershell
& $env:GODOT4 --path $PWD
```

Im Editor:
1. `25×25 Recursive Backtracker` generieren, `3D-Ansicht` aktivieren. Erwartet: Maze ist vollständig im Bild, leicht von oben (Vogelperspektive ~35°).
2. Auf `100×100` umstellen, neu generieren, `3D-Ansicht` aktivieren. Erwartet: Kamera ist jetzt höher, Maze ist immer noch vollständig im Bild.
3. Auf `500×500` umstellen, ggf. mit "Ohne Tempolimit" generieren, `3D-Ansicht` aktivieren. Erwartet: Maze ragt nicht aus dem Bild, weder unten noch seitlich.
4. WASD nach dem Auto-Fit funktioniert ohne Sprung — die Kamera bewegt sich aus der Auto-Fit-Position weg, nicht zurück in einen alten Zustand.

- [ ] **Step 5: Commit**

```bash
git add scripts/Views/CameraController3D.cs scripts/Views/MazeView3D.cs
git commit -m "Task 14.4: Kamera-Auto-Fit nach SetMaze"
```

---

## Phase 15 — 2D-Pan/Zoom-Steuerung

> **Hinzugefügt am 2026-04-30 als Folge-Anforderung.** Bei Mazes >250×250 wird `MazeView2D` ohne Kamera unbenutzbar — bei `CellSizePx = 24` und 1000×1000 ist die Welt 24000×24000 px. Diese Phase fügt eine `CameraController2D` analog zur 3D-Kamera hinzu.

Ziel: Eigene `CameraController2D`-Klasse hängt am neuen `Camera2D`-Knoten in `MazeView2D.tscn`. WASD/Pfeiltasten pannen, Shift verdoppelt das Tempo, Mausrad zoomt mit Mausposition als Pivot, RMB-Drag pannt mit der Maus. `MazeView2D.SetMaze` ruft `FitToMaze` für korrekte Anfangsposition.

### Task 15.1: `CameraController2D` — Skelett und `[Export]`-Felder

**Files:**
- Create: `scripts/Views/CameraController2D.cs`
- Modify: `scenes/MazeView2D.tscn` (neuer `Camera2D`-Knoten mit Skript)

- [ ] **Step 1: `scripts/Views/CameraController2D.cs` anlegen**

```csharp
using Godot;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// Frei steuerbare 2D-Kamera fuer die Maze-Ansicht.
/// Bewegung: WASD/Pfeiltasten pannen die Kamera, Shift verdoppelt das Tempo.
/// Drag: rechte Maustaste gedrueckt halten und Maus bewegen.
/// Zoom: Mausrad mit Mausposition als Pivot - der Punkt unter dem Cursor bleibt stehen.
/// </summary>
public partial class CameraController2D : Camera2D
{
    [Export] public float PanSpeed = 800f;             // Welt-px pro Sekunde
    [Export] public float SprintMultiplier = 2f;
    [Export] public float ZoomStep = 1.1f;             // multiplikativ pro Mausrad-Tick
    [Export] public float ZoomSprintMultiplier = 1.3f; // Shift+Wheel macht groessere Spruenge
    [Export] public float MinZoom = 0.01f;
    [Export] public float MaxZoom = 5.0f;

    private bool _isPanning;

    public override void _Ready()
    {
        // Diese Kamera uebernimmt das Viewport-Rendering der 2D-Ansicht.
        MakeCurrent();
    }
}
```

- [ ] **Step 2: `scenes/MazeView2D.tscn` — `Camera2D` als Kind hinzufügen**

Aktuelle Datei:

```text
[gd_scene format=3 uid="uid://b0ma0e0view2d"]

[ext_resource type="Script" path="res://scripts/Views/MazeView2D.cs" id="1_view2d"]

[node name="MazeView2D" type="Node2D"]
script = ExtResource("1_view2d")
position = Vector2(64, 256)
```

Ersetze durch:

```text
[gd_scene load_steps=3 format=3 uid="uid://b0ma0e0view2d"]

[ext_resource type="Script" path="res://scripts/Views/MazeView2D.cs" id="1_view2d"]
[ext_resource type="Script" path="res://scripts/Views/CameraController2D.cs" id="2_camctl2d"]

[node name="MazeView2D" type="Node2D"]
script = ExtResource("1_view2d")
position = Vector2(64, 256)

[node name="Camera2D" type="Camera2D" parent="."]
script = ExtResource("2_camctl2d")
```

> Die `position = Vector2(64, 256)` auf dem `MazeView2D`-Knoten bleibt erhalten — sobald die `Camera2D` aktiv wird, übernimmt sie ohnehin die Viewport-Steuerung; die alte Position bleibt nur als "Fallback ohne Kamera" relevant und schadet nicht.

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add scripts/Views/CameraController2D.cs scenes/MazeView2D.tscn
git commit -m "Task 15.1: CameraController2D-Skelett, an Camera2D gehaengt"
```

### Task 15.2: Pan-Bewegung — WASD/Pfeiltasten + Shift-Sprint

**Files:**
- Modify: `scripts/Views/CameraController2D.cs`

- [ ] **Step 1: `_Process` mit Pan-Logik ergänzen**

Ergänze in `CameraController2D` direkt unter `_Ready`:

```csharp
public override void _Process(double delta)
{
    HandlePan(delta);
}

private void HandlePan(double delta)
{
    // WASD und Pfeiltasten bauen denselben 2D-Richtungsvektor.
    // W/Pfeil-hoch = nach oben (-Y in Godots 2D-Welt), S/runter = +Y, A/links = -X, D/rechts = +X.
    Vector2 input = Vector2.Zero;
    if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up))    input += Vector2.Up;
    if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down))  input += Vector2.Down;
    if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left))  input += Vector2.Left;
    if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right)) input += Vector2.Right;

    if (input == Vector2.Zero)
        return;

    float speed = PanSpeed;
    if (Input.IsPhysicalKeyPressed(Key.Shift))
        speed *= SprintMultiplier;

    // Geteilt durch Zoom: bei stark gezoomter Ansicht reicht eine kleinere Welt-Bewegung
    // fuer dieselbe sichtbare Strecke, sodass das Pan-Tempo subjektiv konstant bleibt.
    Position += input.Normalized() * speed * (float)delta / Zoom.X;
}
```

- [ ] **Step 2: Build prüfen**

```powershell
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add scripts/Views/CameraController2D.cs
git commit -m "Task 15.2: 2D-Kamera-Pan mit WASD/Pfeilen und Shift-Sprint"
```

### Task 15.3: Mausrad-Zoom mit Pivot + RMB-Drag-Pan

**Files:**
- Modify: `scripts/Views/CameraController2D.cs`

- [ ] **Step 1: `_UnhandledInput` ergänzen**

Ergänze direkt unter `HandlePan`:

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    if (@event is InputEventMouseButton mb)
    {
        // RMB druecken/loslassen schaltet den Drag-Modus.
        if (mb.ButtonIndex == MouseButton.Right)
        {
            _isPanning = mb.Pressed;
            return;
        }

        // Mausrad zoomt mit Mausposition als Pivot, sodass der Punkt unter dem Cursor stationaer bleibt.
        if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
        {
            float step = ZoomStep;
            if (Input.IsPhysicalKeyPressed(Key.Shift))
                step = ZoomSprintMultiplier;

            float factor = mb.ButtonIndex == MouseButton.WheelUp ? step : 1f / step;

            // Pivot-Math: Welt-Mausposition vor dem Zoom merken, Zoom anwenden,
            // dann Position so verschieben, dass die Welt-Mausposition gleich bleibt.
            Vector2 mouseWorldBefore = GetGlobalMousePosition();
            Vector2 newZoom = Zoom * factor;
            newZoom.X = Mathf.Clamp(newZoom.X, MinZoom, MaxZoom);
            newZoom.Y = Mathf.Clamp(newZoom.Y, MinZoom, MaxZoom);
            Zoom = newZoom;
            Vector2 mouseWorldAfter = GetGlobalMousePosition();
            Position += mouseWorldBefore - mouseWorldAfter;
            return;
        }
    }

    // Mausbewegung im Drag-Modus pannt entgegengesetzt zur Mausbewegung.
    // Geteilt durch Zoom: 1 Pixel Mausbewegung == 1 Pixel sichtbare Verschiebung.
    if (@event is InputEventMouseMotion motion && _isPanning)
    {
        Position -= motion.Relative / Zoom;
    }
}
```

- [ ] **Step 2: `_Notification` zum Beenden des Drag-Modus bei Fokus-Verlust**

Ergänze direkt unter `_UnhandledInput`:

```csharp
public override void _Notification(int what)
{
    // Wenn das Fenster den Fokus verliert, den Drag-Modus zuruecksetzen,
    // damit der Cursor sich nicht "festhaelt".
    if (what == NotificationApplicationFocusOut)
        _isPanning = false;
}
```

- [ ] **Step 3: Build prüfen**

```powershell
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add scripts/Views/CameraController2D.cs
git commit -m "Task 15.3: 2D-Mausrad-Zoom mit Pivot und RMB-Drag-Pan"
```

### Task 15.4: `FitToMaze` — Auto-Fit nach `SetMaze`

**Files:**
- Modify: `scripts/Views/CameraController2D.cs` (FitToMaze-Methode)
- Modify: `scripts/Views/MazeView2D.cs` (SetMaze ruft FitToMaze)

- [ ] **Step 1: `CameraController2D.FitToMaze` ergänzen**

Ergänze in `CameraController2D` direkt unter `_Notification`:

```csharp
/// <summary>
/// Setzt Zoom und Position so, dass das gesamte Maze ins Viewport passt mit ~10% Rand.
/// </summary>
public void FitToMaze(Model.Maze maze)
{
    var view = GetParent<MazeView2D>();
    float worldW = maze.Width  * view.CellSizePx;
    float worldH = maze.Height * view.CellSizePx;

    Vector2 viewport = GetViewportRect().Size;
    float zoomX = viewport.X / worldW;
    float zoomY = viewport.Y / worldH;
    float zoomFit = Mathf.Min(zoomX, zoomY) * 0.9f;
    zoomFit = Mathf.Clamp(zoomFit, MinZoom, MaxZoom);

    Zoom = new Vector2(zoomFit, zoomFit);
    Position = new Vector2(worldW / 2f, worldH / 2f);
}
```

- [ ] **Step 2: `MazeView2D.SetMaze` ruft `FitToMaze`**

In `scripts/Views/MazeView2D.cs` ergänze ein Feld für die Kamera-Referenz neben dem `_maze`-Feld:

```csharp
private CameraController2D _camera = null!;
```

Ergänze ein `_Ready()` (das die Klasse aktuell nicht hat) ODER, falls bereits vorhanden, eine Zeile darin:

```csharp
public override void _Ready()
{
    _camera = GetNode<CameraController2D>("Camera2D");
}
```

> Hinweis: `MazeView2D` hat aktuell kein `_Ready` (die Klasse löste bisher keine Knoten auf). Diese neue `_Ready`-Methode wird hinzugefügt, eine eventuell vorhandene `_Process`-Override aus Task 12.2 bleibt unberührt.

Aktualisiere `SetMaze` so, dass die Kamera nach dem `QueueRedraw` auf das neue Maze ausgerichtet wird:

```csharp
public void SetMaze(Maze.Model.Maze maze)
{
    _maze = maze;
    QueueRedraw();
    _camera.FitToMaze(maze);
}
```

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`.

- [ ] **Step 4: Manuell testen — Auto-Fit + Pan + Zoom**

```powershell
& $env:GODOT4 --path $PWD
```

Im Editor:
1. `25×25 Recursive Backtracker` generieren. Erwartet: Maze füllt das 2D-Viewport (mit ~10% Rand).
2. WASD pannen die Kamera, Shift verdoppelt das Tempo.
3. Pfeiltasten pannen identisch.
4. Mausrad zoomt; der Punkt unter dem Cursor bleibt stationär.
5. RMB-Drag pannt mit der Maus.
6. Auf `1000×1000` umstellen, mit "Ohne Tempolimit" generieren. Erwartet: Maze ist als gepixelte Fläche sichtbar (Zellen sehr klein, aber komplett im Bild). Hineinzoomen mit Mausrad funktioniert flüssig.

- [ ] **Step 5: Commit**

```bash
git add scripts/Views/CameraController2D.cs scripts/Views/MazeView2D.cs
git commit -m "Task 15.4: 2D-FitToMaze - Auto-Fit nach SetMaze"
```

---

## Abnahme-Smoke-Test (alle vier Phasen zusammen)

Nach Abschluss aller Phasen sollten folgende Szenarien einwandfrei laufen:

- [ ] **Smoke 1: Klein, animiert** — `25×25 Recursive Backtracker`, Tempo 30, 3D-Ansicht beim Start. Erwartet: Animation flüssig in 2D, Auto-Fit zeigt das ganze Maze in 3D.
- [ ] **Smoke 2: Mittelgroß, throttled** — `300×300 BFS-Solver`. Erwartet: 2D-Animation läuft mit ~30 Hz Refresh, kein FPS-Einbruch.
- [ ] **Smoke 3: Groß, ungebremst** — `1000×1000 Recursive Backtracker` mit "Ohne Tempolimit". Erwartet: UI friert ein paar Sekunden ein, danach komplettes Maze sichtbar in 2D, Stoppuhr zeigt plausible Zeit.
- [ ] **Smoke 4: Groß in 3D** — Nach Smoke 3 `3D-Ansicht` aktivieren. Erwartet: 3D-Aufbau in <2 s, Kamera auf Auto-Fit-Position, Maze vollständig im Bild.
- [ ] **Smoke 5: Kamera-Steuerung in 3D** — Im 1000×1000-3D mit WASD frei bewegen, RMB-Maus drehen, Mausrad zoomen. Erwartet: alle Eingaben reagieren, Performance stabil.
- [ ] **Smoke 6: Kamera-Steuerung in 2D** — Im 1000×1000-2D mit WASD pannen, Shift+WASD beschleunigt, RMB-Drag mit Maus pannt, Mausrad zoomt mit Maus-Pivot. Erwartet: Maze nach Auto-Fit komplett sichtbar, Hineinzoomen zeigt einzelne Zellen, Punkt unter Cursor bleibt beim Zoomen stationär.

Wenn alle fünf Smoke-Tests bestehen, sind Phasen 12–14 fertig.
