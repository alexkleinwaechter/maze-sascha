# Maze School Project — Implementierungsplan

> **Für agentische Worker:** REQUIRED SUB-SKILL: Verwende `superpowers:subagent-driven-development` (empfohlen) oder `superpowers:executing-plans` zur task-by-task-Umsetzung. Schritte verwenden Checkbox-Syntax (`- [ ]`).
>
> **Für Schüler:** Du kannst die Phasen und Tasks einzeln nacheinander durcharbeiten. Jeder Task ist so aufgebaut, dass er für sich genommen lauffähigen Code produziert (oder zumindest das `dotnet build` nicht bricht). Kommentare im Code sind bewusst ausführlich.

**Goal:** Ein didaktisches Godot-4.6.2-Projekt in C#, das verschiedene Labyrinth-Erstellungs- und Lösungsalgorithmen sowohl in 2D (Cheat-Sheet mit Zustands-Färbung, Heatmap, Statistiken) als auch in 3D animiert visualisiert. Größe und Geschwindigkeit sind zur Laufzeit veränderbar; Performance- und Speicherverbrauch werden gemessen und angezeigt.

**Architecture:**
- Datenmodell-Schicht (`Maze`, `Cell`, `Direction`) ist render-unabhängig.
- Algorithmen implementieren `IMazeGenerator` bzw. `IMazeSolver` und liefern Schrittobjekte (`GenerationStep`/`SolverStep`) als `IEnumerable`. Ein zentraler `AlgorithmRunner` zieht pro Tick einen Schritt aus dem Iterator und gibt ihn an die aktive View weiter.
- Zwei Views (`MazeView2D` mit `_Draw()`, `MazeView3D` mit `MeshInstance3D`-Bausteinen) konsumieren dieselben Schrittobjekte.
- HUD (`Hud.tscn`) liefert Sliders/Buttons/DropDowns für Größe, Geschwindigkeit, Algorithmus-Auswahl, View-Toggle, Run/Pause/Step. `StatsPanel` zeigt Stoppuhr, besuchte Zellen, Pfadlänge, Speicher.
- `Main.tscn` ist orchestrierender Wurzelknoten und vermittelt zwischen HUD, Views und Runner.

**Tech Stack:**
- Godot 4.6.2 .NET (mono), C# 12 (`<TargetFramework>net8.0</TargetFramework>` setzt Godot automatisch)
- Forward+ Renderer mit D3D12 (bereits in `project.godot` konfiguriert)
- Jolt Physics aktiv (für 3D Polish, optional)
- Windows + PowerShell Workflow gemäß `.github/skills/godot-csharp-windows`
- Godot Executable: `$env:GODOT4` (Fallback `C:\temp\_godot\Godot_v4.6.2-stable_mono_win64.exe`)

**Verzeichnisstruktur (Endzustand):**

```
res://
├── icon.svg
├── project.godot
├── maze-sascha.csproj          (von Godot beim ersten C#-Skript erzeugt)
├── maze-sascha.sln
├── scenes/
│   ├── Main.tscn               Root: Node, Script: Main.cs
│   ├── Hud.tscn                Root: CanvasLayer, Script: Hud.cs
│   ├── StatsPanel.tscn         Root: PanelContainer, Script: StatsPanel.cs
│   ├── MazeView2D.tscn         Root: Node2D, Script: MazeView2D.cs
│   └── MazeView3D.tscn         Root: Node3D, Script: MazeView3D.cs
├── scripts/
│   ├── Main.cs
│   ├── AlgorithmRunner.cs
│   ├── PerformanceTracker.cs
│   ├── Maze/
│   │   ├── Direction.cs
│   │   ├── Cell.cs
│   │   ├── Maze.cs
│   │   └── CellState.cs
│   ├── Generators/
│   │   ├── IMazeGenerator.cs
│   │   ├── GenerationStep.cs
│   │   ├── RecursiveBacktrackerGenerator.cs
│   │   ├── GrowingTreeGenerator.cs
│   │   ├── RecursiveDivisionGenerator.cs
│   │   └── CellularAutomataGenerator.cs   (optional)
│   ├── Solvers/
│   │   ├── IMazeSolver.cs
│   │   ├── SolverStep.cs
│   │   ├── BreadthFirstSolver.cs
│   │   ├── DepthFirstSolver.cs
│   │   ├── AStarSolver.cs
│   │   ├── GreedyBestFirstSolver.cs
│   │   ├── WallFollowerSolver.cs
│   │   └── DeadEndFillingSolver.cs
│   ├── Hud/
│   │   ├── Hud.cs
│   │   └── StatsPanel.cs
│   └── Views/
│       ├── MazeView2D.cs
│       └── MazeView3D.cs
└── docs/
    └── superpowers/plans/2026-04-28-maze-school-project.md   (dieses Dokument)
```

**Konventionen:**
- Klassendateiname == Klassenname (Godot-C#-Pflicht für angehängte Skripte).
- `public partial class` und `using Godot;` für jedes Godot-Skript.
- Englische Identifier, deutsche Kommentare bei didaktisch wertvollen Stellen.
- `null!` Backing-Field-Pattern für Knoten, die in `_Ready()` aufgelöst werden.
- Sliders/Inputs verwenden Min/Max/Step explizit, damit Werte zur Laufzeit gültig bleiben.

**Hinweise zur Ausführung:**
- Nach jedem Hinzufügen oder Umbenennen eines C#-Skripts mit `[Export]` oder `[Signal]`: `& $env:GODOT4 --path $PWD --build-solutions` ausführen (laut Skill `godot-csharp-windows`).
- Reine Codeänderungen: `dotnet build` reicht.
- Vor jeder größeren Phase einmal Headless-Smoke: `& $env:GODOT4 --headless --path $PWD --quit --verbose`.
- Bei `cd`-Pfaden in PowerShell den Workspace-Pfad absolut schreiben.
- Setze einmalig (z. B. in der aktuellen PowerShell-Session): `$env:GODOT4 = "C:\temp\_godot\Godot_v4.6.2-stable_mono_win64.exe"`.

---

## Phase 0 — Projekt-Grundgerüst

Ziel: Lauffähige, leere `Main`-Szene mit `Main.cs` und allen Override-Methoden. Erste erfolgreiche `dotnet build` und Headless-Smoke.

### Task 0.1: Verzeichnisstruktur anlegen

**Files:**
- Create: `scenes/`
- Create: `scripts/Maze/`
- Create: `scripts/Generators/`
- Create: `scripts/Solvers/`
- Create: `scripts/Hud/`
- Create: `scripts/Views/`

- [x] **Step 1: Verzeichnisse anlegen**

PowerShell:

```powershell
New-Item -ItemType Directory -Force -Path scenes, scripts\Maze, scripts\Generators, scripts\Solvers, scripts\Hud, scripts\Views | Out-Null
```

- [x] **Step 2: Verifizieren**

```powershell
Get-ChildItem -Recurse -Directory | Select-Object -ExpandProperty FullName
```

Erwartet: Alle sechs Unterordner vorhanden.

### Task 0.2: Main.tscn anlegen und als Hauptszene setzen

> Wir legen zuerst die Szene und die Projekteinstellung an. `Main.cs` und das C#-Projektgerüst (`.csproj` / `.sln`) folgen in Task 0.3, weil Godot 4.6.2 diese Dateien nicht zuverlässig per CLI erzeugt — der Editor muss einmal interaktiv die Solution anlegen.

**Files:**
- Create: `scenes/Main.tscn`
- Modify: `project.godot` (`[application] run/main_scene` Eintrag)

- [x] **Step 1: `scenes/Main.tscn` per Texteditor anlegen**

Godot speichert Szenen als reine Textdateien. Wir können diese Datei direkt anlegen, ohne den Editor zu starten. Die `[ext_resource]`-Zeile auf `Main.cs` setzt eine vorgemerkte Referenz — die Datei selbst tippen wir gleich in Task 0.3.

```text
[gd_scene load_steps=2 format=3 uid="uid://b0maze0main"]

[ext_resource type="Script" path="res://scripts/Main.cs" id="1_main"]

[node name="Main" type="Node"]
script = ExtResource("1_main")
```

> Hinweis: `uid` darf jeder eindeutige Bezeichner sein. Godot ersetzt ihn beim ersten Speichern ohnehin durch eine generierte UID, die String-Form `uid://...` ist erlaubt.

- [x] **Step 2: `project.godot` um Hauptszene erweitern**

Vor dem Block `[dotnet]` einfügen (oder `[application]` ergänzen):

```ini
[application]

config/name="maze-sascha"
config/features=PackedStringArray("4.6", "Forward Plus")
config/icon="res://icon.svg"
run/main_scene="res://scenes/Main.tscn"
```

### Task 0.3: Main.cs anlegen und C#-Projekt initialisieren

> Beim ersten Mal muss der Godot-Editor einmal interaktiv gestartet werden: er entdeckt das `.cs`-Skript, fragt nach dem Anlegen der Solution und erzeugt dann `maze-sascha.csproj` und `maze-sascha.sln`. Erst danach funktionieren `dotnet build` und der Headless-Smoke. Die rein per CLI erreichbare Option `--build-solutions` reicht in Godot 4.6.2 dafür **nicht** aus.

**Files:**
- Create: `scripts/Main.cs`

- [x] **Step 1: `Main.cs` schreiben**

```csharp
using Godot;

namespace Maze;

/// <summary>
/// Wurzelskript der Hauptszene. Verbindet HUD, Datenmodell und die aktive View.
/// In dieser Phase ist Main noch ein leeres Skelett mit allen Lebenszyklus-Methoden.
/// </summary>
public partial class Main : Node
{
    // Wird aufgerufen, wenn der Knoten zum SceneTree hinzugefügt wurde
    // und alle Kinder ebenfalls bereit sind. Hier werden später Referenzen
    // auf HUD und Views aufgelöst.
    public override void _Ready()
    {
        GD.Print("[Main] _Ready: Hauptszene wurde geladen.");
    }

    // Wird in jedem Frame aufgerufen. Wir nutzen es vorerst nicht aktiv,
    // implementieren es aber, damit Schüler die Standard-Lifecycle-Hooks sehen.
    public override void _Process(double delta)
    {
        // Bewusst leer. Spätere Phasen reichen das delta an den AlgorithmRunner.
    }

    // Wird mit fester Frequenz aufgerufen (Standard 60 Hz, für Physik).
    // Für unser Projekt nicht zwingend notwendig, der Vollständigkeit halber.
    public override void _PhysicsProcess(double delta)
    {
        // Bewusst leer.
    }

    // Letzter Hook vor dem Entfernen aus dem SceneTree. Hier werden später
    // laufende Coroutinen / Timer / Aufgaben sauber beendet.
    public override void _ExitTree()
    {
        GD.Print("[Main] _ExitTree: Hauptszene wird verlassen.");
    }
}
```

- [ ] **Step 2: Godot-Editor starten**

```powershell
& $env:GODOT4 --path $PWD --editor
```

> **Wichtig:** Godot 4.6.2 erzeugt `.csproj`/`.sln` **nicht** automatisch, nur weil eine `.cs`-Datei im Projekt liegt. Ein Auto-Dialog beim Start erscheint in dieser Version typischerweise auch nicht. Die Solution muss in Schritt 3 explizit über das Menü angestoßen werden.

- [ ] **Step 3: C#-Solution explizit über das Menü erzeugen**

Im laufenden Editor:

1. Menü öffnen: `Project` → `Tools` → `C#` → **`Create C# solution`** anklicken.
   - Falls der Eintrag fehlt oder ausgegraut ist (z. B. weil Reste vorhanden sind), Workspace-Root prüfen — gibt es bereits `maze-sascha.csproj`? Wenn nicht, weiter mit dem Fallback in Step 5.
2. Godot legt jetzt `maze-sascha.csproj` und `maze-sascha.sln` im Projekt-Root an.
3. Anschließend `Build → Build Project` (oder das Hammer-Symbol oben rechts) ausführen, damit Godot die Marshalling-Quellen + die initiale Assembly erzeugt.
4. Editor schließen.

- [ ] **Step 4: Verifizieren, dass `.csproj` und `.sln` da sind**

```powershell
Get-ChildItem -Path $PWD -Filter "*.csproj"
Get-ChildItem -Path $PWD -Filter "*.sln"
```

Erwartet: je ein Treffer (`maze-sascha.csproj`, `maze-sascha.sln`).


- [ ] **Step 5: Build prüfen**

```powershell
dotnet build
```

Erwartet: `Build succeeded`. Keine Warnungen außer `CS8618` falls der Compiler über später initialisierte Felder meckert (kommt erst in späteren Tasks).

- [ ] **Step 6: Headless-Smoke**

```powershell
& $env:GODOT4 --headless --path $PWD --quit --verbose
```

Erwartet: Im Output erscheint die Zeile `[Main] _Ready: Hauptszene wurde geladen.` gefolgt vom `_ExitTree`-Print. Keine Fehler.

### Task 0.4: Git initialisieren und ersten Commit setzen (optional)

- [ ] **Step 1: Git-Repo anlegen**

```powershell
git init
git add .
git commit -m "chore: scaffold maze project (Main scene + script)"
```

> Entfällt, falls ihr das Projekt bewusst ohne Git nutzt. Für Schulprojekte wird Git aber sehr empfohlen.

---

## Phase 1 — Datenmodell

Ziel: Render-unabhängige Repräsentation eines rechteckigen Zellgitters mit Wänden zwischen Nachbarn.

### Task 1.1: `Direction` Enum

**Files:**
- Create: `scripts/Maze/Direction.cs`

- [x] **Step 1: Datei anlegen**

```csharp
namespace Maze.Model;

/// <summary>
/// Vier Himmelsrichtungen für ein 4-Nachbarschafts-Gitter.
/// Reihenfolge ist wichtig, weil wir per Index gegen <c>_offsets</c> indexieren.
/// </summary>
public enum Direction
{
    North = 0,
    East  = 1,
    South = 2,
    West  = 3
}

/// <summary>
/// Hilfsmethoden rund um die <see cref="Direction"/>-Aufzählung.
/// Bewusst als statische Klasse, damit Schüler die Funktionen ohne Instanzierung verwenden können.
/// </summary>
public static class DirectionHelper
{
    // Versatz (dx, dy) je Richtung. Reihenfolge passt zur Enum.
    private static readonly (int dx, int dy)[] _offsets =
    {
        ( 0, -1), // North
        ( 1,  0), // East
        ( 0,  1), // South
        (-1,  0)  // West
    };

    public static (int dx, int dy) Offset(Direction direction) =>
        _offsets[(int)direction];

    /// <summary>
    /// Liefert die entgegengesetzte Richtung. Praktisch beim Wanddurchbruch:
    /// wenn wir von A nach Osten zu B gehen, müssen wir bei B die Westwand entfernen.
    /// </summary>
    public static Direction Opposite(Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.East  => Direction.West,
        Direction.South => Direction.North,
        Direction.West  => Direction.East,
        _ => throw new System.ArgumentOutOfRangeException(nameof(direction))
    };

    /// <summary>
    /// Alle vier Richtungen als statisches Array (read-only View).
    /// Praktisch für <c>foreach</c> in Generatoren und Solvern.
    /// </summary>
    public static readonly Direction[] All = { Direction.North, Direction.East, Direction.South, Direction.West };
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build
```

Erwartet: `Build succeeded`.

### Task 1.2: `CellState` Enum

> Repräsentiert den visuellen Zustand einer Zelle während Generation und Lösung. Liegt im Datenmodell, weil sowohl 2D- als auch 3D-View ihn lesen.

**Files:**
- Create: `scripts/Maze/CellState.cs`

- [x] **Step 1: Datei anlegen**

```csharp
namespace Maze.Model;

/// <summary>
/// Visueller / logischer Zustand einer Zelle. Wird vom Renderer auf eine Farbe gemappt.
/// </summary>
public enum CellState
{
    /// <summary>Noch von keinem Algorithmus angefasst.</summary>
    Untouched   = 0,
    /// <summary>Aktuell vom Generator besucht (Carving-Front).</summary>
    Carving     = 1,
    /// <summary>Vom Generator fertig bearbeitet, aber kein Solver-Status.</summary>
    Open        = 2,
    /// <summary>Solver hat die Zelle in der Frontier / OpenSet.</summary>
    Frontier    = 3,
    /// <summary>Solver hat die Zelle bereits abgeschlossen (ClosedSet).</summary>
    Visited     = 4,
    /// <summary>Teil des finalen, gefundenen Pfades.</summary>
    Path        = 5,
    /// <summary>Startzelle des Solvers.</summary>
    Start       = 6,
    /// <summary>Zielzelle des Solvers.</summary>
    Goal        = 7,
    /// <summary>Markierung für Dead-End-Filling: Zelle wurde "verstopft".</summary>
    Filled      = 8
}
```

### Task 1.3: `Cell`-Klasse

**Files:**
- Create: `scripts/Maze/Cell.cs`

- [x] **Step 1: Datei anlegen**

```csharp
namespace Maze.Model;

/// <summary>
/// Eine einzelne Zelle des Labyrinths.
/// Enthält Position, Wandstatus zu allen vier Nachbarn und einen visuellen Zustand.
/// </summary>
public sealed class Cell
{
    public int X { get; }
    public int Y { get; }

    // Eine Wand ist "vorhanden" (true) oder "durchbrochen" (false).
    // Index 0..3 nach <see cref="Direction"/>.
    private readonly bool[] _walls = { true, true, true, true };

    public CellState State { get; set; } = CellState.Untouched;

    /// <summary>Distanzwert für Heatmap / Solver-Anzeigen. -1 = unbekannt.</summary>
    public int Distance { get; set; } = -1;

    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool HasWall(Direction direction) => _walls[(int)direction];

    public void SetWall(Direction direction, bool present) =>
        _walls[(int)direction] = present;

    /// <summary>
    /// Entfernt die Wand zwischen zwei benachbarten Zellen.
    /// Achtung: Wir müssen beide Seiten konsistent halten — hier wird nur eine Seite verändert.
    /// Die andere Seite erledigt <see cref="Maze.RemoveWallBetween"/>.
    /// </summary>
    public void RemoveWall(Direction direction) => SetWall(direction, false);

    public override string ToString() => $"Cell({X},{Y}) State={State}";
}
```

### Task 1.4: `Maze`-Klasse

**Files:**
- Create: `scripts/Maze/Maze.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System;
using System.Collections.Generic;

namespace Maze.Model;

/// <summary>
/// Rechteckiges Zellgitter mit Wänden zwischen Nachbarn.
/// Reine Datenstruktur — kennt weder Godot noch Rendering.
/// </summary>
public sealed class Maze
{
    public int Width  { get; }
    public int Height { get; }

    private readonly Cell[,] _cells;

    public Maze(int width, int height)
    {
        if (width  < 2) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 2) throw new ArgumentOutOfRangeException(nameof(height));

        Width  = width;
        Height = height;
        _cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            _cells[x, y] = new Cell(x, y);
    }

    public Cell GetCell(int x, int y) => _cells[x, y];

    public bool IsInside(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>
    /// Liefert den Nachbarn in eine Richtung — oder null, wenn er außerhalb des Gitters liegt.
    /// </summary>
    public Cell? GetNeighbor(Cell cell, Direction direction)
    {
        var (dx, dy) = DirectionHelper.Offset(direction);
        int nx = cell.X + dx;
        int ny = cell.Y + dy;
        return IsInside(nx, ny) ? _cells[nx, ny] : null;
    }

    /// <summary>
    /// Entfernt die Wand zwischen zwei Zellen auf BEIDEN Seiten.
    /// </summary>
    public void RemoveWallBetween(Cell from, Direction toNeighbor)
    {
        Cell? neighbor = GetNeighbor(from, toNeighbor);
        if (neighbor is null)
            throw new InvalidOperationException("Kein Nachbar in dieser Richtung.");

        from.RemoveWall(toNeighbor);
        neighbor.RemoveWall(DirectionHelper.Opposite(toNeighbor));
    }

    /// <summary>
    /// Iteriert alle Zellen zeilenweise. Praktisch für Renderer und Solver.
    /// </summary>
    public IEnumerable<Cell> AllCells()
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width;  x++)
            yield return _cells[x, y];
    }

    /// <summary>
    /// Setzt alle CellState/Distance auf Default zurück. Wird vor jedem Solver-Lauf aufgerufen.
    /// Wände bleiben erhalten.
    /// </summary>
    public void ResetSolverState()
    {
        foreach (var cell in AllCells())
        {
            cell.State    = CellState.Open;
            cell.Distance = -1;
        }
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build
```

Erwartet: `Build succeeded`. Keine Warnungen.

---

## Phase 2 — HUD-Grundgerüst

Ziel: Sichtbares Bedienpanel mit Größe-Slider, Geschwindigkeit-Slider, Generator-Auswahl und Generate-Button. Sendet Signale, die `Main` empfängt.

### Task 2.1: `Hud.cs` schreiben

**Files:**
- Create: `scripts/Hud/Hud.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using Godot;
using Maze.Model;

namespace Maze.UI;

/// <summary>
/// Bedienoberfläche oben am Bildschirm.
/// Sendet typisierte Signale an Main, statt direkt Felder zu ändern.
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

    // ---- Knotenreferenzen (in _Ready aufgelöst) ----
    private HSlider     _widthSlider     = null!;
    private HSlider     _heightSlider    = null!;
    private HSlider     _speedSlider     = null!;
    private OptionButton _generatorChooser = null!;
    private OptionButton _solverChooser    = null!;
    private Button      _generateButton  = null!;
    private Button      _solveButton     = null!;
    private Button      _pauseButton     = null!;
    private Button      _stepButton      = null!;
    private Button      _resetButton     = null!;
    private CheckBox    _viewToggle      = null!;
    private Label       _widthLabel      = null!;
    private Label       _heightLabel     = null!;
    private Label       _speedLabel      = null!;

    public override void _Ready()
    {
        _widthSlider      = GetNode<HSlider>("Root/Margin/VBox/Sizes/WidthSlider");
        _heightSlider     = GetNode<HSlider>("Root/Margin/VBox/Sizes/HeightSlider");
        _speedSlider      = GetNode<HSlider>("Root/Margin/VBox/SpeedRow/SpeedSlider");
        _generatorChooser = GetNode<OptionButton>("Root/Margin/VBox/Algos/GeneratorChooser");
        _solverChooser    = GetNode<OptionButton>("Root/Margin/VBox/Algos/SolverChooser");
        _generateButton   = GetNode<Button>("Root/Margin/VBox/Buttons/GenerateButton");
        _solveButton      = GetNode<Button>("Root/Margin/VBox/Buttons/SolveButton");
        _pauseButton      = GetNode<Button>("Root/Margin/VBox/Buttons/PauseButton");
        _stepButton       = GetNode<Button>("Root/Margin/VBox/Buttons/StepButton");
        _resetButton      = GetNode<Button>("Root/Margin/VBox/Buttons/ResetButton");
        _viewToggle       = GetNode<CheckBox>("Root/Margin/VBox/Algos/View3DToggle");
        _widthLabel       = GetNode<Label>("Root/Margin/VBox/Sizes/WidthLabel");
        _heightLabel      = GetNode<Label>("Root/Margin/VBox/Sizes/HeightLabel");
        _speedLabel       = GetNode<Label>("Root/Margin/VBox/SpeedRow/SpeedLabel");

        // ---- Slider-Werte initial sicherstellen ----
        _widthSlider.MinValue  = 5;  _widthSlider.MaxValue  = 75;  _widthSlider.Step = 1;  _widthSlider.Value = 25;
        _heightSlider.MinValue = 5;  _heightSlider.MaxValue = 75;  _heightSlider.Step = 1;  _heightSlider.Value = 25;
        _speedSlider.MinValue  = 1;  _speedSlider.MaxValue  = 240; _speedSlider.Step = 1;  _speedSlider.Value  = 30;

        UpdateLabels();

        // ---- Signal-Wiring (C#-Eventsyntax) ----
        _widthSlider.ValueChanged   += _ => UpdateLabels();
        _heightSlider.ValueChanged  += _ => UpdateLabels();
        _speedSlider.ValueChanged   += OnSpeedChanged;
        _generateButton.Pressed     += OnGeneratePressed;
        _solveButton.Pressed        += OnSolvePressed;
        _pauseButton.Toggled        += OnPauseToggled;
        _stepButton.Pressed         += OnStepPressed;
        _resetButton.Pressed        += OnResetPressed;
        _viewToggle.Toggled         += OnViewToggled;

        FillGeneratorChooser();
        FillSolverChooser();
    }

    private void UpdateLabels()
    {
        _widthLabel.Text  = $"Breite:  {(int)_widthSlider.Value}";
        _heightLabel.Text = $"Höhe:    {(int)_heightSlider.Value}";
        _speedLabel.Text  = $"Tempo:  {(int)_speedSlider.Value} Schritte/s";
    }

    private void OnSpeedChanged(double value)
    {
        UpdateLabels();
        EmitSignal(SignalName.SpeedChanged, (float)value);
    }

    private void OnGeneratePressed()
    {
        int  w = (int)_widthSlider.Value;
        int  h = (int)_heightSlider.Value;
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
        AddGenerator("Recursive Backtracker",  "recursive-backtracker");
        AddGenerator("Growing Tree (75% newest, 25% random)", "growing-tree");
        AddGenerator("Recursive Division",     "recursive-division");
        AddGenerator("Cellular Automata (4-5)", "cellular-automata");
        _generatorChooser.Selected = 0;
    }

    private void FillSolverChooser()
    {
        _solverChooser.Clear();
        AddSolver("Breadth-First Search (BFS)", "bfs");
        AddSolver("Depth-First Search (DFS)",   "dfs");
        AddSolver("A*",                          "a-star");
        AddSolver("Greedy Best-First",          "greedy");
        AddSolver("Wall Follower (links)",      "wall-follower");
        AddSolver("Dead-End Filling",            "dead-end-filling");
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
```

### Task 2.2: `Hud.tscn` als Text-Resource anlegen

**Files:**
- Create: `scenes/Hud.tscn`

- [ ] **Step 1: Datei anlegen**

> Tipp: Diese Szenenstruktur kann später bequem im Editor verfeinert werden. Wir erzeugen sie hier textuell, damit der Build Schritt für Schritt prüfbar ist.

```text
[gd_scene load_steps=2 format=3 uid="uid://b0maze0hud"]

[ext_resource type="Script" path="res://scripts/Hud/Hud.cs" id="1_hud"]

[node name="Hud" type="CanvasLayer"]
script = ExtResource("1_hud")

[node name="Root" type="PanelContainer" parent="."]
anchor_right = 1.0
offset_bottom = 220.0
mouse_filter = 1

[node name="Margin" type="MarginContainer" parent="Root"]
offset_right = 1280.0
offset_bottom = 220.0
theme_override_constants/margin_left = 12
theme_override_constants/margin_top = 8
theme_override_constants/margin_right = 12
theme_override_constants/margin_bottom = 8

[node name="VBox" type="VBoxContainer" parent="Root/Margin"]

[node name="Sizes" type="HBoxContainer" parent="Root/Margin/VBox"]

[node name="WidthLabel" type="Label" parent="Root/Margin/VBox/Sizes"]
text = "Breite: 25"

[node name="WidthSlider" type="HSlider" parent="Root/Margin/VBox/Sizes"]
min_value = 5.0
max_value = 75.0
step = 1.0
value = 25.0
size_flags_horizontal = 3

[node name="HeightLabel" type="Label" parent="Root/Margin/VBox/Sizes"]
text = "Höhe: 25"

[node name="HeightSlider" type="HSlider" parent="Root/Margin/VBox/Sizes"]
min_value = 5.0
max_value = 75.0
step = 1.0
value = 25.0
size_flags_horizontal = 3

[node name="SpeedRow" type="HBoxContainer" parent="Root/Margin/VBox"]

[node name="SpeedLabel" type="Label" parent="Root/Margin/VBox/SpeedRow"]
text = "Tempo: 30 Schritte/s"

[node name="SpeedSlider" type="HSlider" parent="Root/Margin/VBox/SpeedRow"]
min_value = 1.0
max_value = 240.0
step = 1.0
value = 30.0
size_flags_horizontal = 3

[node name="Algos" type="HBoxContainer" parent="Root/Margin/VBox"]

[node name="GeneratorChooser" type="OptionButton" parent="Root/Margin/VBox/Algos"]

[node name="SolverChooser" type="OptionButton" parent="Root/Margin/VBox/Algos"]

[node name="View3DToggle" type="CheckBox" parent="Root/Margin/VBox/Algos"]
text = "3D-Ansicht"

[node name="Buttons" type="HBoxContainer" parent="Root/Margin/VBox"]

[node name="GenerateButton" type="Button" parent="Root/Margin/VBox/Buttons"]
text = "Erstellen"

[node name="SolveButton" type="Button" parent="Root/Margin/VBox/Buttons"]
text = "Lösen"

[node name="PauseButton" type="Button" parent="Root/Margin/VBox/Buttons"]
text = "Pause"
toggle_mode = true

[node name="StepButton" type="Button" parent="Root/Margin/VBox/Buttons"]
text = "Schritt"

[node name="ResetButton" type="Button" parent="Root/Margin/VBox/Buttons"]
text = "Reset"
```

### Task 2.3: HUD in Main einbinden

**Files:**
- Modify: `scenes/Main.tscn`
- Modify: `scripts/Main.cs`

- [ ] **Step 1: `Main.tscn` erweitern**

Datei komplett ersetzen durch:

```text
[gd_scene load_steps=3 format=3 uid="uid://b0maze0main"]

[ext_resource type="Script" path="res://scripts/Main.cs" id="1_main"]
[ext_resource type="PackedScene" uid="uid://b0maze0hud" path="res://scenes/Hud.tscn" id="2_hud"]

[node name="Main" type="Node"]
script = ExtResource("1_main")

[node name="Hud" parent="." instance=ExtResource("2_hud")]
```

- [ ] **Step 2: `Main.cs` um HUD-Verbindung erweitern**

Datei komplett ersetzen durch:

```csharp
using Godot;
using Maze.UI;

namespace Maze;

/// <summary>
/// Wurzelskript der Hauptszene. In dieser Phase nimmt Main HUD-Signale entgegen
/// und gibt sie als Console-Print aus, damit Schüler sehen, dass die Verkabelung funktioniert.
/// </summary>
public partial class Main : Node
{
    private Hud _hud = null!;

    public override void _Ready()
    {
        _hud = GetNode<Hud>("Hud");

        // Signale per C#-Eventsyntax abonnieren — typsicher und ohne Magic Strings.
        _hud.GenerateRequested    += OnGenerateRequested;
        _hud.SolveRequested       += OnSolveRequested;
        _hud.SpeedChanged         += OnSpeedChanged;
        _hud.PauseToggle          += OnPauseToggled;
        _hud.StepRequested        += OnStepRequested;
        _hud.ResetRequested       += OnResetRequested;
        _hud.ViewToggleRequested  += OnViewToggled;

        GD.Print("[Main] HUD verbunden.");
    }

    public override void _Process(double delta) { /* spätere Phase */ }
    public override void _PhysicsProcess(double delta) { /* spätere Phase */ }
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
```

- [ ] **Step 3: Solution-Build, weil Signale neu sind**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

- [ ] **Step 4: Editor starten und HUD ausprobieren**

```powershell
& $env:GODOT4 --path $PWD
```

Erwartet: HUD oben sichtbar, Slider/Buttons funktionieren, Console zeigt entsprechende `[Main]`-Prints.

---

## Phase 3 — 2D-Visualisierung Grundgerüst

Ziel: Eine `MazeView2D`-Szene rendert ein vom Main übergebenes `Maze`-Objekt mit `_Draw()`. Zellfarben spiegeln `CellState` wider; Wände werden als Linien gezeichnet.

### Task 3.1: `MazeView2D.cs` schreiben

**Files:**
- Create: `scripts/Views/MazeView2D.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using Godot;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// 2D-Visualisierung des Labyrinths. Liest <see cref="Maze.Model.Maze"/> und zeichnet
/// jede Zelle als gefülltes Rechteck (Farbe nach Zustand) plus Wände als Linien.
/// </summary>
public partial class MazeView2D : Node2D
{
    [Export] public int    CellSizePx       = 24;
    [Export] public int    WallThicknessPx  = 2;
    [Export] public bool   ShowDistances    = false;

    // Farb-Map für Zellzustände. Public statisch, damit das StatsPanel die gleichen Farben
    // wiederverwenden kann (Legenden-Farbpunkte).
    public static readonly System.Collections.Generic.Dictionary<CellState, Color> StateColors = new()
    {
        { CellState.Untouched, new Color("#1e1e1e") },
        { CellState.Carving,   new Color("#ffaa00") },
        { CellState.Open,      new Color("#2c2c2c") },
        { CellState.Frontier,  new Color("#8ab4f8") },
        { CellState.Visited,   new Color("#3d5a80") },
        { CellState.Path,      new Color("#f6c177") },
        { CellState.Start,     new Color("#a3be8c") },
        { CellState.Goal,      new Color("#bf616a") },
        { CellState.Filled,    new Color("#000000") }
    };

    private static readonly Color WallColor   = new("#dcdcdc");
    private static readonly Color HeatmapMin  = new("#003366");
    private static readonly Color HeatmapMax  = new("#ff6f3c");

    private Model.Maze? _maze;

    /// <summary>Setzt das aktuelle Maze und löst Neuzeichnung aus.</summary>
    public void SetMaze(Model.Maze maze)
    {
        _maze = maze;
        QueueRedraw();
    }

    /// <summary>Erzwingt eine Neuzeichnung — wird nach jedem Algorithmus-Schritt aufgerufen.</summary>
    public void Refresh() => QueueRedraw();

    public override void _Draw()
    {
        if (_maze is null)
            return;

        // ---- Zellfüllungen ----
        int maxDistance = ComputeMaxDistance(_maze);

        foreach (var cell in _maze.AllCells())
        {
            var rect = new Rect2(
                cell.X * CellSizePx,
                cell.Y * CellSizePx,
                CellSizePx,
                CellSizePx
            );

            Color fill = ShowDistances && cell.Distance >= 0
                ? Heatmap(cell.Distance, maxDistance)
                : StateColors[cell.State];

            DrawRect(rect, fill, filled: true);
        }

        // ---- Wände als Linien ----
        foreach (var cell in _maze.AllCells())
        {
            float x0 = cell.X * CellSizePx;
            float y0 = cell.Y * CellSizePx;
            float x1 = x0 + CellSizePx;
            float y1 = y0 + CellSizePx;

            if (cell.HasWall(Direction.North))
                DrawLine(new Vector2(x0, y0), new Vector2(x1, y0), WallColor, WallThicknessPx);
            if (cell.HasWall(Direction.West))
                DrawLine(new Vector2(x0, y0), new Vector2(x0, y1), WallColor, WallThicknessPx);
            // Süd- und Ostwände werden nur am Rand gezeichnet, sonst doppelt.
            if (cell.Y == _maze.Height - 1 && cell.HasWall(Direction.South))
                DrawLine(new Vector2(x0, y1), new Vector2(x1, y1), WallColor, WallThicknessPx);
            if (cell.X == _maze.Width - 1 && cell.HasWall(Direction.East))
                DrawLine(new Vector2(x1, y0), new Vector2(x1, y1), WallColor, WallThicknessPx);
        }
    }

    private static int ComputeMaxDistance(Model.Maze maze)
    {
        int max = 0;
        foreach (var c in maze.AllCells())
            if (c.Distance > max) max = c.Distance;
        return max;
    }

    private static Color Heatmap(int distance, int maxDistance)
    {
        if (maxDistance <= 0) return HeatmapMin;
        float t = (float)distance / maxDistance;
        return HeatmapMin.Lerp(HeatmapMax, t);
    }
}
```

### Task 3.2: `MazeView2D.tscn` anlegen

**Files:**
- Create: `scenes/MazeView2D.tscn`

- [ ] **Step 1: Datei anlegen**

```text
[gd_scene load_steps=2 format=3 uid="uid://b0maze0view2d"]

[ext_resource type="Script" path="res://scripts/Views/MazeView2D.cs" id="1_view2d"]

[node name="MazeView2D" type="Node2D"]
script = ExtResource("1_view2d")
position = Vector2(64, 256)
```

> Die Position 64,256 verschiebt die View unter das HUD. Wir kalibrieren das in einer späteren Phase abhängig von der Maze-Größe.

### Task 3.3: Statisches Maze testweise rendern

**Files:**
- Modify: `scenes/Main.tscn`
- Modify: `scripts/Main.cs`

- [ ] **Step 1: `Main.tscn` um MazeView2D ergänzen**

```text
[gd_scene load_steps=4 format=3 uid="uid://b0maze0main"]

[ext_resource type="Script" path="res://scripts/Main.cs" id="1_main"]
[ext_resource type="PackedScene" uid="uid://b0maze0hud" path="res://scenes/Hud.tscn" id="2_hud"]
[ext_resource type="PackedScene" uid="uid://b0maze0view2d" path="res://scenes/MazeView2D.tscn" id="3_view2d"]

[node name="Main" type="Node"]
script = ExtResource("1_main")

[node name="MazeView2D" parent="." instance=ExtResource("3_view2d")]

[node name="Hud" parent="." instance=ExtResource("2_hud")]
```

- [ ] **Step 2: `Main.cs` erweitern**

Den Body von `_Ready()` und den Generate-Handler ersetzen:

```csharp
using Godot;
using Maze.Model;
using Maze.UI;
using Maze.Views;

namespace Maze;

public partial class Main : Node
{
    private Hud         _hud   = null!;
    private MazeView2D  _view2D = null!;
    private Model.Maze? _currentMaze;

    public override void _Ready()
    {
        _hud    = GetNode<Hud>("Hud");
        _view2D = GetNode<MazeView2D>("MazeView2D");

        _hud.GenerateRequested    += OnGenerateRequested;
        _hud.SolveRequested       += OnSolveRequested;
        _hud.SpeedChanged         += OnSpeedChanged;
        _hud.PauseToggle          += OnPauseToggled;
        _hud.StepRequested        += OnStepRequested;
        _hud.ResetRequested       += OnResetRequested;
        _hud.ViewToggleRequested  += OnViewToggled;

        GD.Print("[Main] HUD + 2D-View verbunden.");
    }

    private void OnGenerateRequested(int width, int height, string generatorId)
    {
        GD.Print($"[Main] Erstelle leeres Maze {width}x{height} (TEST, ohne Generator).");
        _currentMaze = new Model.Maze(width, height);

        // Vorerst zeigen wir das Voll-Wand-Maze nur an. Generatoren folgen in Phase 5.
        foreach (var c in _currentMaze.AllCells())
            c.State = CellState.Open;

        _view2D.SetMaze(_currentMaze);
    }

    private void OnSolveRequested(string solverId)         => GD.Print($"[Main] Solve {solverId}");
    private void OnSpeedChanged(float stepsPerSecond)      => GD.Print($"[Main] Tempo {stepsPerSecond}");
    private void OnPauseToggled(bool paused)               => GD.Print($"[Main] Pause {paused}");
    private void OnStepRequested()                         => GD.Print("[Main] Schritt");
    private void OnResetRequested()                        => GD.Print("[Main] Reset");
    private void OnViewToggled(bool use3D)                 => GD.Print($"[Main] 3D {use3D}");

    public override void _Process(double delta) { }
    public override void _PhysicsProcess(double delta) { }
    public override void _ExitTree() => GD.Print("[Main] _ExitTree.");
}
```

- [ ] **Step 3: Build und visueller Smoketest**

```powershell
dotnet build
& $env:GODOT4 --path $PWD
```

Erwartet: Beim Klick auf "Erstellen" erscheint ein vollwandiges Gitter unter dem HUD.

---

## Phase 4 — Generator-Schnittstelle und AlgorithmRunner

Ziel: Algorithmen liefern Schritte, ein zentraler Runner zieht pro Tick einen Schritt und übergibt ihn an die View.

### Task 4.1: `GenerationStep` und `IMazeGenerator`

**Files:**
- Create: `scripts/Generators/GenerationStep.cs`
- Create: `scripts/Generators/IMazeGenerator.cs`

- [ ] **Step 1: `GenerationStep.cs` anlegen**

```csharp
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Beschreibt einen einzelnen Animationsschritt eines Generators.
/// Enthält genug Information, damit die View ihn als Visualisierung umsetzen kann,
/// aber selbst keine Render-Logik.
/// </summary>
public sealed record GenerationStep(
    /// <summary>Zelle, deren Zustand sich ändert (z. B. neue Carving-Zelle).</summary>
    Cell Cell,
    /// <summary>Optionaler Nachbar — wenn gesetzt, wird die Wand zwischen Cell und Neighbor entfernt.</summary>
    Cell? Neighbor,
    /// <summary>Richtung von Cell -> Neighbor (für die Wandberechnung).</summary>
    Direction? RemoveWallTowards,
    /// <summary>Neuer Zellzustand für die Visualisierung.</summary>
    CellState NewState,
    /// <summary>Frei wählbarer Beschreibungstext (für Statspanel-Tooltips).</summary>
    string Description
);
```

- [ ] **Step 2: `IMazeGenerator.cs` anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Gemeinsame Schnittstelle aller Erstellungsalgorithmen.
/// Liefert eine Sequenz von <see cref="GenerationStep"/>-Objekten — eines pro Animationsschritt.
/// </summary>
public interface IMazeGenerator
{
    /// <summary>Eindeutige ID, die im HUD-OptionButton verwendet wird.</summary>
    string Id   { get; }
    /// <summary>Lesbarer Anzeigename.</summary>
    string Name { get; }

    /// <summary>
    /// Generiert das übergebene Labyrinth in-place und liefert die Schritte.
    /// Implementierungen verwenden <see cref="System.Random"/> als Quelle für Zufall.
    /// </summary>
    IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random);
}
```

### Task 4.2: `AlgorithmRunner`

**Files:**
- Create: `scripts/AlgorithmRunner.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Godot;
using Maze.Generators;
using Maze.Model;
using Maze.Solvers;

namespace Maze;

/// <summary>
/// Treibt einen <see cref="IEnumerator{T}"/> aus Generator/Solver tickweise voran.
/// Geschwindigkeit (Schritte/s) wird per Property gesteuert; Pause/Step werden unterstützt.
/// </summary>
public partial class AlgorithmRunner : Node
{
    [Signal] public delegate void GenerationStepProducedEventHandler();
    [Signal] public delegate void SolverStepProducedEventHandler();
    [Signal] public delegate void GenerationFinishedEventHandler();
    [Signal] public delegate void SolverFinishedEventHandler();

    public float StepsPerSecond { get; set; } = 30f;
    public bool  IsPaused       { get; set; }
    public bool  IsRunning      => _genIterator != null || _solverIterator != null;

    public GenerationStep? LastGenerationStep { get; private set; }
    public SolverStep?     LastSolverStep     { get; private set; }

    private IEnumerator<GenerationStep>? _genIterator;
    private IEnumerator<SolverStep>?     _solverIterator;
    private double _accumulator;

    public void StartGeneration(IEnumerable<GenerationStep> steps)
    {
        _genIterator?.Dispose();
        _genIterator    = steps.GetEnumerator();
        _solverIterator = null;
        _accumulator    = 0;
    }

    public void StartSolver(IEnumerable<SolverStep> steps)
    {
        _solverIterator?.Dispose();
        _solverIterator = steps.GetEnumerator();
        _genIterator    = null;
        _accumulator    = 0;
    }

    public void StopAll()
    {
        _genIterator?.Dispose();    _genIterator    = null;
        _solverIterator?.Dispose(); _solverIterator = null;
    }

    /// <summary>Manuelles Vorrücken um genau einen Schritt (Step-Modus).</summary>
    public void ForceSingleStep()
    {
        if (_genIterator    != null) AdvanceGenerator();
        if (_solverIterator != null) AdvanceSolver();
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

            if (_genIterator    != null) { AdvanceGenerator(); continue; }
            if (_solverIterator != null) { AdvanceSolver();    continue; }
        }
    }

    private void AdvanceGenerator()
    {
        if (_genIterator!.MoveNext())
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
        if (_solverIterator!.MoveNext())
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
```

- [ ] **Step 2: Runner als Autoload-Knoten in Main einhängen**

`scenes/Main.tscn` erweitern:

```text
[gd_scene load_steps=5 format=3 uid="uid://b0maze0main"]

[ext_resource type="Script" path="res://scripts/Main.cs" id="1_main"]
[ext_resource type="PackedScene" uid="uid://b0maze0hud" path="res://scenes/Hud.tscn" id="2_hud"]
[ext_resource type="PackedScene" uid="uid://b0maze0view2d" path="res://scenes/MazeView2D.tscn" id="3_view2d"]
[ext_resource type="Script" path="res://scripts/AlgorithmRunner.cs" id="4_runner"]

[node name="Main" type="Node"]
script = ExtResource("1_main")

[node name="MazeView2D" parent="." instance=ExtResource("3_view2d")]

[node name="Runner" type="Node" parent="."]
script = ExtResource("4_runner")

[node name="Hud" parent="." instance=ExtResource("2_hud")]
```

- [ ] **Step 3: Build**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

---

## Phase 5 — Recursive Backtracker

Ziel: Erster vollständig implementierter Generator. Schaltbar im HUD, animiert in 2D.

### Task 5.1: `RecursiveBacktrackerGenerator`

**Files:**
- Create: `scripts/Generators/RecursiveBacktrackerGenerator.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Klassischer Recursive Backtracker (DFS-Carving) — iterativ mit Stack.
/// 1. Wähle eine Start-Zelle, markiere sie als besucht und lege sie auf den Stack.
/// 2. Solange der Stack nicht leer ist:
///    - Wähle die oberste Zelle.
///    - Hat sie einen unbesuchten Nachbarn, brich Wand zu einem zufälligen davon und schiebe Nachbarn auf den Stack.
///    - Sonst: pop.
/// </summary>
public sealed class RecursiveBacktrackerGenerator : IMazeGenerator
{
    public string Id   => "recursive-backtracker";
    public string Name => "Recursive Backtracker";

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        var visited = new bool[maze.Width, maze.Height];
        var stack   = new Stack<Cell>();

        Cell start = maze.GetCell(0, 0);
        visited[start.X, start.Y] = true;
        stack.Push(start);
        yield return new GenerationStep(start, null, null, CellState.Carving, "Start");

        while (stack.Count > 0)
        {
            Cell current = stack.Peek();

            // Unbesuchte Nachbarn sammeln.
            var unvisited = new List<Direction>();
            foreach (var direction in DirectionHelper.All)
            {
                Cell? n = maze.GetNeighbor(current, direction);
                if (n != null && !visited[n.X, n.Y])
                    unvisited.Add(direction);
            }

            if (unvisited.Count == 0)
            {
                // Sackgasse — backtracking.
                stack.Pop();
                current.State = CellState.Open;
                yield return new GenerationStep(current, null, null, CellState.Open, "Backtrack");
                continue;
            }

            // Zufällig einen Nachbarn wählen, Wand entfernen, ihn als nächste Carving-Zelle pushen.
            Direction pick = unvisited[random.Next(unvisited.Count)];
            Cell      next = maze.GetNeighbor(current, pick)!;
            maze.RemoveWallBetween(current, pick);
            visited[next.X, next.Y] = true;
            stack.Push(next);

            yield return new GenerationStep(next, current, pick, CellState.Carving, "Carve");
        }
    }
}
```

### Task 5.2: Generator-Registry und Hud-Anbindung in Main

**Files:**
- Modify: `scripts/Main.cs`

- [x] **Step 1: Main vollständig erweitern**

```csharp
using System;
using System.Collections.Generic;
using Godot;
using Maze.Generators;
using Maze.Model;
using Maze.UI;
using Maze.Views;

namespace Maze;

public partial class Main : Node
{
    private Hud              _hud    = null!;
    private MazeView2D       _view2D = null!;
    private AlgorithmRunner  _runner = null!;

    private Model.Maze? _currentMaze;

    private readonly Dictionary<string, IMazeGenerator> _generators = new()
    {
        ["recursive-backtracker"] = new RecursiveBacktrackerGenerator()
        // weitere folgen in Phase 6
    };

    private readonly Random _random = new();

    public override void _Ready()
    {
        _hud    = GetNode<Hud>("Hud");
        _view2D = GetNode<MazeView2D>("MazeView2D");
        _runner = GetNode<AlgorithmRunner>("Runner");

        _hud.GenerateRequested    += OnGenerateRequested;
        _hud.SolveRequested       += OnSolveRequested;
        _hud.SpeedChanged         += OnSpeedChanged;
        _hud.PauseToggle          += OnPauseToggled;
        _hud.StepRequested        += OnStepRequested;
        _hud.ResetRequested       += OnResetRequested;
        _hud.ViewToggleRequested  += OnViewToggled;

        _runner.GenerationStepProduced += OnGenerationStepProduced;
        _runner.GenerationFinished     += OnGenerationFinished;

        _runner.StepsPerSecond = 30f;
    }

    private void OnGenerateRequested(int width, int height, string generatorId)
    {
        if (!_generators.TryGetValue(generatorId, out var generator))
        {
            GD.PrintErr($"Unbekannter Generator: {generatorId}");
            return;
        }

        _runner.StopAll();
        _currentMaze = new Model.Maze(width, height);
        _view2D.SetMaze(_currentMaze);

        _runner.StartGeneration(generator.Generate(_currentMaze, _random));
        GD.Print($"[Main] Generator {generator.Name} gestartet.");
    }

    private void OnGenerationStepProduced()
    {
        var step = _runner.LastGenerationStep;
        if (step is null || _currentMaze is null) return;

        // Wandentfernung wurde bereits in Generate() ausgeführt.
        // Wir aktualisieren nur den visuellen Zustand.
        step.Cell.State = step.NewState;
        _view2D.Refresh();
    }

    private void OnGenerationFinished()
    {
        if (_currentMaze is null) return;
        foreach (var c in _currentMaze.AllCells())
            c.State = CellState.Open;
        _view2D.Refresh();
        GD.Print("[Main] Generator fertig.");
    }

    private void OnSolveRequested(string solverId)    => GD.Print($"[Main] Solve {solverId} (folgt)");
    private void OnSpeedChanged(float stepsPerSecond) => _runner.StepsPerSecond = stepsPerSecond;
    private void OnPauseToggled(bool paused)          => _runner.IsPaused = paused;
    private void OnStepRequested()                    => _runner.ForceSingleStep();
    private void OnResetRequested()
    {
        _runner.StopAll();
        _currentMaze = null;
        _view2D.SetMaze(new Model.Maze(2, 2));
        GD.Print("[Main] Reset.");
    }
    private void OnViewToggled(bool use3D) => GD.Print($"[Main] 3D {use3D} (folgt)");

    public override void _Process(double delta) { }
    public override void _PhysicsProcess(double delta) { }
    public override void _ExitTree() => GD.Print("[Main] _ExitTree.");
}
```

- [x] **Step 2: Build & Smoketest**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
& $env:GODOT4 --path $PWD
```

Erwartet: "Erstellen" mit "Recursive Backtracker" zeichnet animiert das Labyrinth. Geschwindigkeit reagiert auf Slider. Pause/Step/Reset funktionieren.

---

## Phase 6 — Weitere Generatoren

Ziel: Drei zusätzliche Erstellungsalgorithmen. Auswahl im HUD bleibt unverändert; Main muss sie nur in `_generators` registrieren.

### Task 6.1: `GrowingTreeGenerator`

**Files:**
- Create: `scripts/Generators/GrowingTreeGenerator.cs`

- [x] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Growing Tree mit konfigurierbarer Auswahlstrategie. Default: 75% newest, 25% random.
/// 1. Aktive Liste mit Startzelle initialisieren.
/// 2. Solange aktive Liste nicht leer:
///    - Wähle Index (newest = letztes Element, random = beliebig).
///    - Falls die Zelle einen unbesuchten Nachbarn hat: Wand brechen, Nachbar an die Liste anhängen.
///    - Sonst Zelle aus der Liste entfernen.
/// </summary>
public sealed class GrowingTreeGenerator : IMazeGenerator
{
    public string Id   => "growing-tree";
    public string Name => "Growing Tree (75% newest, 25% random)";

    private readonly float _newestProbability;

    public GrowingTreeGenerator(float newestProbability = 0.75f)
    {
        _newestProbability = newestProbability;
    }

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        var visited = new bool[maze.Width, maze.Height];
        var active  = new List<Cell>();

        Cell start = maze.GetCell(random.Next(maze.Width), random.Next(maze.Height));
        visited[start.X, start.Y] = true;
        active.Add(start);
        yield return new GenerationStep(start, null, null, CellState.Carving, "Start");

        while (active.Count > 0)
        {
            int idx = random.NextDouble() < _newestProbability
                ? active.Count - 1
                : random.Next(active.Count);
            Cell current = active[idx];

            var unvisited = new List<Direction>();
            foreach (var direction in DirectionHelper.All)
            {
                Cell? n = maze.GetNeighbor(current, direction);
                if (n != null && !visited[n.X, n.Y])
                    unvisited.Add(direction);
            }

            if (unvisited.Count == 0)
            {
                active.RemoveAt(idx);
                current.State = CellState.Open;
                yield return new GenerationStep(current, null, null, CellState.Open, "Drop");
                continue;
            }

            Direction pick = unvisited[random.Next(unvisited.Count)];
            Cell      next = maze.GetNeighbor(current, pick)!;
            maze.RemoveWallBetween(current, pick);
            visited[next.X, next.Y] = true;
            active.Add(next);

            yield return new GenerationStep(next, current, pick, CellState.Carving, "Carve");
        }
    }
}
```

- [x] **Step 2: In Main registrieren**

In `scripts/Main.cs` das `_generators`-Dictionary erweitern:

```csharp
private readonly Dictionary<string, IMazeGenerator> _generators = new()
{
    ["recursive-backtracker"] = new RecursiveBacktrackerGenerator(),
    ["growing-tree"]          = new GrowingTreeGenerator()
};
```

### Task 6.2: `RecursiveDivisionGenerator`

> Erzeugt Labyrinthe durch rekursives Teilen einer offenen Fläche mit einer Wand, in die ein einzelnes Loch geschlagen wird. Wir starten mit einer komplett offenen Fläche und müssen daher initial alle Innenwände entfernen.

**Files:**
- Create: `scripts/Generators/RecursiveDivisionGenerator.cs`

- [x] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Recursive Division: Teilt einen Rechteckbereich rekursiv mit einer Wand,
/// in die genau ein Durchgang geschlagen wird. Erzeugt architektonisch wirkende Labyrinthe.
/// </summary>
public sealed class RecursiveDivisionGenerator : IMazeGenerator
{
    public string Id   => "recursive-division";
    public string Name => "Recursive Division";

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        // 1) Komplett offene Fläche herstellen (alle Innenwände raus).
        for (int y = 0; y < maze.Height; y++)
        for (int x = 0; x < maze.Width;  x++)
        {
            Cell c = maze.GetCell(x, y);
            if (x < maze.Width - 1)  maze.RemoveWallBetween(c, Direction.East);
            if (y < maze.Height - 1) maze.RemoveWallBetween(c, Direction.South);
            c.State = CellState.Open;
        }
        yield return new GenerationStep(maze.GetCell(0, 0), null, null, CellState.Open, "Cleared");

        // 2) Stack-basierte Division (statt Rekursion, damit wir per yield Schritte ausgeben können).
        var work = new Stack<(int x, int y, int w, int h)>();
        work.Push((0, 0, maze.Width, maze.Height));

        while (work.Count > 0)
        {
            var (x, y, w, h) = work.Pop();
            if (w < 2 || h < 2) continue;

            bool horizontal = ChooseOrientation(w, h, random);

            if (horizontal)
            {
                // Horizontale Wand zwischen y+wallY-1 und y+wallY (also unter Reihe wallY-1).
                int wallRow  = y + 1 + random.Next(h - 1);
                int passage  = x + random.Next(w);

                for (int cx = x; cx < x + w; cx++)
                {
                    if (cx == passage) continue;
                    Cell upper = maze.GetCell(cx, wallRow - 1);
                    upper.SetWall(Direction.South, true);
                    Cell lower = maze.GetCell(cx, wallRow);
                    lower.SetWall(Direction.North, true);
                    yield return new GenerationStep(upper, lower, Direction.South, CellState.Open, "Wall");
                }

                work.Push((x, y,            w, wallRow - y));
                work.Push((x, wallRow,      w, h - (wallRow - y)));
            }
            else
            {
                int wallCol = x + 1 + random.Next(w - 1);
                int passage = y + random.Next(h);

                for (int cy = y; cy < y + h; cy++)
                {
                    if (cy == passage) continue;
                    Cell left  = maze.GetCell(wallCol - 1, cy);
                    left.SetWall(Direction.East, true);
                    Cell right = maze.GetCell(wallCol, cy);
                    right.SetWall(Direction.West, true);
                    yield return new GenerationStep(left, right, Direction.East, CellState.Open, "Wall");
                }

                work.Push((x,           y, wallCol - x,        h));
                work.Push((wallCol,     y, w - (wallCol - x),  h));
            }
        }
    }

    private static bool ChooseOrientation(int width, int height, System.Random random)
    {
        if (width  < height) return true;   // horizontal teilen
        if (height < width)  return false;  // vertikal teilen
        return random.Next(2) == 0;
    }
}
```

- [x] **Step 2: Registrieren**

```csharp
private readonly Dictionary<string, IMazeGenerator> _generators = new()
{
    ["recursive-backtracker"] = new RecursiveBacktrackerGenerator(),
    ["growing-tree"]          = new GrowingTreeGenerator(),
    ["recursive-division"]    = new RecursiveDivisionGenerator()
};
```

### Task 6.3 (optional): `CellularAutomataGenerator` (Parr 2018, "true maze")

> **Wichtig:** Hier verwenden wir **nicht** die klassische 4-5-Höhlenregel — die produziert Höhlen, keine Labyrinthe. Statt dessen implementieren wir den Cellular-Automata-Ansatz aus **Justin A. Parr, _Generating Mazes Using Cellular Automata_ (2018)**, der ein echtes perfektes Spanning-Tree-Labyrinth (1-Zellen-breite Korridore, keine Zyklen, alle Zellen erreichbar) per Zustandsmaschine pro Zelle erzeugt.
>
> **Zustände pro Zelle:** `Disconnected (0)` → `Seed (1)` → `Invite (2)` → `Connected (3)`.
>
> **Regelablauf pro CA-Tick (Snapshot/Backbuffer-Mechanik):**
> 1. **Disconnected**: Falls ein Nachbar im `Invite`-Zustand mit `InviteVector` auf mich zeigt → ich werde `Seed`, merke mir den Nachbarn als Eltern (`ConnectVector`), die Wand zwischen uns wird entfernt.
> 2. **Seed**: Sammle disconnected Nachbarn. Wähle eine Richtung mit Direktionspersistenz (mit `1 - TurnProbability` geradeaus, also Gegenteil von `ConnectVector`; sonst zufällig). Setze `InviteVector`. Werde `Invite`. Hat keine Disconnected Nachbarn → werde `Connected` (Seed stirbt).
> 3. **Invite**: Mit `BranchProbability` zurück zu `Seed` (Verzweigung), sonst → `Connected`.
> 4. **Connected**: Nur aktiv, wenn **kein** Seed/Invite mehr lebt. Dann: borderst du Disconnected, wirst du mit `BranchProbability` wieder Seed (Failsafe).
>
> Optimum laut Paper: `BranchProbability = 5%`, `TurnProbability = 10%` (= 90% geradeaus).

**Files:**
- Create: `scripts/Generators/CellularAutomataGenerator.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Cellular Automata-Generator nach Justin A. Parr (2018).
/// Erzeugt ein perfektes Spanning-Tree-Labyrinth durch eine simple
/// 4-Zustands-Maschine pro Zelle und Snapshot-basierte CA-Ticks.
/// </summary>
public sealed class CellularAutomataGenerator : IMazeGenerator
{
    public string Id   => "cellular-automata";
    public string Name => "Cellular Automata (Parr, true maze)";

    // Optimum laut Paper: 5% Verzweigungswahrscheinlichkeit
    private const int BranchProbabilityPercent = 5;
    // Optimum laut Paper: 10% Drehwahrscheinlichkeit (= 90% geradeaus)
    private const int TurnProbabilityPercent   = 10;

    // Reine Sicherheit gegen pathologische Endlosschleifen.
    private const int SafetyMaxTicks           = 1_000_000;

    private enum CaState : byte
    {
        Disconnected = 0,
        Seed         = 1,
        Invite       = 2,
        Connected    = 3
    }

    private struct CellInfo
    {
        public CaState     State;
        // Zeigt von dieser Zelle auf die Eltern-Zelle (für Direktionspersistenz).
        public Direction?  ConnectVector;
        // Im Invite-Zustand: zeigt auf den eingeladenen Nachbarn.
        public Direction?  InviteVector;
    }

    public IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random)
    {
        int w = maze.Width;
        int h = maze.Height;

        // Doppelpufferung: Wir LESEN aus current[,] und SCHREIBEN nach next[,].
        // Am Ende jedes Ticks wird next nach current kopiert.
        var current = new CellInfo[w, h];
        var next    = new CellInfo[w, h];

        // Eine zufällige Startzelle wird der Initial-Seed.
        int sx = random.Next(w);
        int sy = random.Next(h);
        current[sx, sy].State = CaState.Seed;

        Cell startCell = maze.GetCell(sx, sy);
        startCell.State = CellState.Carving;
        yield return new GenerationStep(startCell, null, null, CellState.Carving, "Initial seed");

        for (int tick = 0; tick < SafetyMaxTicks; tick++)
        {
            // Backbuffer mit dem aktuellen Stand vorbelegen — Zellen, die keine Regel
            // ausführen, behalten dadurch ihren Zustand.
            Array.Copy(current, next, current.Length);

            // Vor dem Regelpass einmal feststellen, ob aktuell überhaupt noch
            // ein Seed/Invite "lebt". Wird vom Connected-Failsafe gebraucht.
            bool anyActive = AnyActive(current, w, h);

            // -------- Regelpass über alle Zellen --------
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                CellInfo me = current[x, y];

                switch (me.State)
                {
                    case CaState.Disconnected:
                        foreach (var step in TryAcceptInvitation(maze, current, next, x, y, random))
                            yield return step;
                        break;

                    case CaState.Seed:
                        foreach (var step in RunSeed(maze, current, next, x, y, me, random))
                            yield return step;
                        break;

                    case CaState.Invite:
                        foreach (var step in RunInvite(maze, next, x, y, random))
                            yield return step;
                        break;

                    case CaState.Connected:
                        if (!anyActive)
                            foreach (var step in TryRevive(maze, current, next, x, y, random))
                                yield return step;
                        break;
                }
            }

            // Snapshot übernehmen: next wird zu current für den nächsten Tick.
            Array.Copy(next, current, next.Length);

            // Termination: alle Zellen entweder Connected oder noch aktiv?
            // Wir sind fertig, wenn KEINE Disconnected-Zelle mehr existiert
            // UND kein Seed/Invite mehr lebt (sonst wären weitere Carves möglich).
            if (!AnyDisconnected(current, w, h) && !AnyActive(current, w, h))
                yield break;
        }
    }

    // ---- Zustandsregeln ------------------------------------------------------

    private static IEnumerable<GenerationStep> TryAcceptInvitation(
        Model.Maze maze, CellInfo[,] current, CellInfo[,] next,
        int x, int y, System.Random random)
    {
        // Suche einen Nachbarn im Invite-Zustand, der per InviteVector auf mich zeigt.
        foreach (var dir in DirectionHelper.All)
        {
            var (dx, dy) = DirectionHelper.Offset(dir);
            int nx = x + dx;
            int ny = y + dy;
            if (!maze.IsInside(nx, ny)) continue;

            CellInfo neighbor = current[nx, ny];
            if (neighbor.State != CaState.Invite) continue;
            if (neighbor.InviteVector != DirectionHelper.Opposite(dir)) continue;

            // Einladung annehmen: ich werde Seed, Wand zwischen uns fällt,
            // Eltern-Vektor zeigt auf den einladenden Nachbarn.
            next[x, y].State         = CaState.Seed;
            next[x, y].ConnectVector = dir;
            next[x, y].InviteVector  = null;

            Cell me  = maze.GetCell(x, y);
            Cell par = maze.GetCell(nx, ny);
            maze.RemoveWallBetween(me, dir);
            me.State = CellState.Carving;

            yield return new GenerationStep(me, par, dir, CellState.Carving, "Accept invite");
            yield break;
        }
    }

    private static IEnumerable<GenerationStep> RunSeed(
        Model.Maze maze, CellInfo[,] current, CellInfo[,] next,
        int x, int y, CellInfo me, System.Random random)
    {
        // Bitfeld der Disconnected-Nachbarn (Bit i = Richtung i).
        int neighborMask = BuildDisconnectedMask(maze, current, x, y);

        if (neighborMask == 0)
        {
            // Kein freier Nachbar mehr -> Seed stirbt, wird Connected.
            next[x, y].State        = CaState.Connected;
            next[x, y].InviteVector = null;
            Cell c = maze.GetCell(x, y);
            c.State = CellState.Open;
            yield return new GenerationStep(c, null, null, CellState.Open, "Seed dies");
            yield break;
        }

        // Richtungswahl mit Persistenz: 90% geradeaus, 10% zufällig.
        Direction picked = ChooseDirection(neighborMask, me.ConnectVector, random);
        next[x, y].State        = CaState.Invite;
        next[x, y].InviteVector = picked;
        // Kein Yield: Seed und Invite sehen visuell gleich aus (beide Carving).
    }

    private static IEnumerable<GenerationStep> RunInvite(
        Model.Maze maze, CellInfo[,] next,
        int x, int y, System.Random random)
    {
        if (random.Next(100) < BranchProbabilityPercent)
        {
            // Verzweigen: zurück zu Seed (zusätzlicher aktiver Front-Zelle).
            next[x, y].State        = CaState.Seed;
            next[x, y].InviteVector = null;
            // Kein Yield: bleibt visuell Carving.
        }
        else
        {
            // Stabilisieren: Connected.
            next[x, y].State        = CaState.Connected;
            next[x, y].InviteVector = null;
            Cell c = maze.GetCell(x, y);
            c.State = CellState.Open;
            yield return new GenerationStep(c, null, null, CellState.Open, "Connected");
        }
    }

    private static IEnumerable<GenerationStep> TryRevive(
        Model.Maze maze, CellInfo[,] current, CellInfo[,] next,
        int x, int y, System.Random random)
    {
        // Failsafe: nur wenn KEIN Seed/Invite mehr lebt — wird vom Aufrufer geprüft.
        // Ich bin Connected. Borderne ich Disconnected? Dann mit BranchProbability
        // wieder Seed werden, damit die Front weiterläuft.
        bool bordersDisconnected = false;
        foreach (var dir in DirectionHelper.All)
        {
            var (dx, dy) = DirectionHelper.Offset(dir);
            int nx = x + dx;
            int ny = y + dy;
            if (!maze.IsInside(nx, ny)) continue;
            if (current[nx, ny].State == CaState.Disconnected)
            {
                bordersDisconnected = true;
                break;
            }
        }
        if (!bordersDisconnected) yield break;
        if (random.Next(100) >= BranchProbabilityPercent) yield break;

        next[x, y].State = CaState.Seed;
        Cell c = maze.GetCell(x, y);
        c.State = CellState.Carving;
        yield return new GenerationStep(c, null, null, CellState.Carving, "Revive");
    }

    // ---- Hilfsfunktionen -----------------------------------------------------

    private static int BuildDisconnectedMask(Model.Maze maze, CellInfo[,] grid, int x, int y)
    {
        int mask = 0;
        foreach (var dir in DirectionHelper.All)
        {
            var (dx, dy) = DirectionHelper.Offset(dir);
            int nx = x + dx;
            int ny = y + dy;
            if (!maze.IsInside(nx, ny)) continue;
            if (grid[nx, ny].State != CaState.Disconnected) continue;
            mask |= 1 << (int)dir;
        }
        return mask;
    }

    private static Direction ChooseDirection(int neighborMask, Direction? connectVector, System.Random random)
    {
        // Geradeaus = Gegenteil von ConnectVector (Eltern liegen "hinter mir",
        // also will ich in die Gegenrichtung weitergehen).
        bool turnNow = random.Next(100) < TurnProbabilityPercent;

        if (!turnNow && connectVector.HasValue)
        {
            Direction straight = DirectionHelper.Opposite(connectVector.Value);
            if ((neighborMask & (1 << (int)straight)) != 0)
                return straight;
            // Geradeaus geht nicht (Wand/Rand/besetzt) — wir müssen sowieso umlenken.
        }

        // Zufällige verfügbare Richtung wählen.
        Span<Direction> available = stackalloc Direction[4];
        int count = 0;
        foreach (var dir in DirectionHelper.All)
            if ((neighborMask & (1 << (int)dir)) != 0)
                available[count++] = dir;
        return available[random.Next(count)];
    }

    private static bool AnyActive(CellInfo[,] grid, int w, int h)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            CaState s = grid[x, y].State;
            if (s == CaState.Seed || s == CaState.Invite) return true;
        }
        return false;
    }

    private static bool AnyDisconnected(CellInfo[,] grid, int w, int h)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            if (grid[x, y].State == CaState.Disconnected) return true;
        return false;
    }
}
```

> **Hinweise zur Visualisierung:** Wir geben pro CA-Tick mehrere `GenerationStep`s aus — einen pro sichtbarer Zellzustands­änderung (Accept, Seed-Tod, Connected, Revive). Reine Seed↔Invite-Wechsel werden nicht ausgegeben, weil sie visuell beide "aktiv/Carving" sind. Das ergibt eine schön welleartige Animation, ohne unsichtbare Leerschritte.
>
> **Vergleich mit den anderen Generatoren:** Recursive Backtracker erzeugt lange gewundene Gänge mit hohem River-Faktor; Growing Tree interpoliert je nach Strategie zwischen Backtracker- und Prim-Look; Recursive Division wirkt architektonisch. Der Parr-CA hat einen ganz eigenen Charakter: lange parallele Gänge, viele 90°-Abzweigungen, Spiralen — siehe Bilder im PDF auf Seite 32.

- [x] **Step 2: Registrieren**

```csharp
private readonly Dictionary<string, IMazeGenerator> _generators = new()
{
    ["recursive-backtracker"] = new RecursiveBacktrackerGenerator(),
    ["growing-tree"]          = new GrowingTreeGenerator(),
    ["recursive-division"]    = new RecursiveDivisionGenerator(),
    ["cellular-automata"]     = new CellularAutomataGenerator()
};
```

- [ ] **Step 3: Build und visueller Test**

```powershell
dotnet build
& $env:GODOT4 --path $PWD
```

---

## Phase 7 — 3D-Visualisierung

Ziel: Ein `MazeView3D` rendert das Maze als Boden + Wandquader in 3D. Toggle im HUD schaltet zwischen 2D und 3D.

> Hinweis: Wir extrudieren das 2D-Maze in 3D (Wände nach oben). Echtes 3D-Maze (6 Nachbarn) ist als spätere Erweiterung möglich, sprengt aber den Phase-7-Rahmen.

### Task 7.1: `MazeView3D.cs` schreiben

**Files:**
- Create: `scripts/Views/MazeView3D.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using Godot;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// 3D-Visualisierung des Labyrinths. Erzeugt für jede Wand einen MeshInstance3D-Würfel
/// und einen großen Bodenmesh. Wird bei jedem SetMaze/Refresh komplett neu gebaut —
/// für die Größenordnungen unseres Schulprojekts (bis ~75x75) ist das schnell genug.
/// </summary>
public partial class MazeView3D : Node3D
{
    [Export] public float CellSize       = 1.0f;
    [Export] public float WallHeight     = 1.4f;
    [Export] public float WallThickness  = 0.1f;

    private Node3D _wallContainer = null!;
    private MeshInstance3D _floor = null!;

    private static readonly StandardMaterial3D WallMaterial = new()
    {
        AlbedoColor = new Color("#dcdcdc")
    };

    private static readonly StandardMaterial3D FloorMaterial = new()
    {
        AlbedoColor = new Color("#2c2c2c")
    };

    private static readonly BoxMesh WallMesh = new();

    private Model.Maze? _maze;

    public override void _Ready()
    {
        _wallContainer = GetNode<Node3D>("WallContainer");
        _floor         = GetNode<MeshInstance3D>("Floor");
    }

    public void SetMaze(Model.Maze maze)
    {
        _maze = maze;
        Rebuild();
    }

    public void Refresh()
    {
        // In dieser einfachen Variante reicht ein vollständiger Neubau.
        if (_maze != null) Rebuild();
    }

    private void Rebuild()
    {
        ClearWalls();
        if (_maze is null) return;

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
        var size = new Vector3(maze.Width * CellSize, 0.05f, maze.Height * CellSize);
        _floor.Mesh = new BoxMesh { Size = size };
        _floor.MaterialOverride = FloorMaterial;
        _floor.Position = new Vector3(maze.Width * CellSize / 2f, -0.025f, maze.Height * CellSize / 2f);
    }

    private void BuildWalls(Model.Maze maze)
    {
        for (int y = 0; y < maze.Height; y++)
        for (int x = 0; x < maze.Width;  x++)
        {
            Cell c = maze.GetCell(x, y);
            if (c.HasWall(Direction.North))
                AddWall(x * CellSize + CellSize / 2f, y * CellSize, CellSize, WallThickness);
            if (c.HasWall(Direction.West))
                AddWall(x * CellSize, y * CellSize + CellSize / 2f, WallThickness, CellSize);
            if (y == maze.Height - 1 && c.HasWall(Direction.South))
                AddWall(x * CellSize + CellSize / 2f, (y + 1) * CellSize, CellSize, WallThickness);
            if (x == maze.Width - 1 && c.HasWall(Direction.East))
                AddWall((x + 1) * CellSize, y * CellSize + CellSize / 2f, WallThickness, CellSize);
        }
    }

    private void AddWall(float cx, float cz, float lengthX, float lengthZ)
    {
        var inst = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(lengthX, WallHeight, lengthZ) },
            MaterialOverride = WallMaterial,
            Position = new Vector3(cx, WallHeight / 2f, cz)
        };
        _wallContainer.AddChild(inst);
    }
}
```

### Task 7.2: `MazeView3D.tscn` anlegen

**Files:**
- Create: `scenes/MazeView3D.tscn`

- [ ] **Step 1: Datei anlegen**

```text
[gd_scene load_steps=2 format=3 uid="uid://b0maze0view3d"]

[ext_resource type="Script" path="res://scripts/Views/MazeView3D.cs" id="1_view3d"]

[node name="MazeView3D" type="Node3D"]
visible = false
script = ExtResource("1_view3d")

[node name="Camera3D" type="Camera3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 0.7071, 0.7071, 0, -0.7071, 0.7071, 12, 25, 12)
fov = 60.0

[node name="Sun" type="DirectionalLight3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 0.5, 0.866, 0, -0.866, 0.5, 0, 12, 0)
shadow_enabled = true

[node name="Floor" type="MeshInstance3D" parent="."]

[node name="WallContainer" type="Node3D" parent="."]
```

### Task 7.3: View-Toggle in Main

**Files:**
- Modify: `scenes/Main.tscn`
- Modify: `scripts/Main.cs`

- [x] **Step 1: `Main.tscn` um 3D-View erweitern**

```text
[gd_scene load_steps=6 format=3 uid="uid://b0maze0main"]

[ext_resource type="Script" path="res://scripts/Main.cs" id="1_main"]
[ext_resource type="PackedScene" uid="uid://b0maze0hud" path="res://scenes/Hud.tscn" id="2_hud"]
[ext_resource type="PackedScene" uid="uid://b0maze0view2d" path="res://scenes/MazeView2D.tscn" id="3_view2d"]
[ext_resource type="Script" path="res://scripts/AlgorithmRunner.cs" id="4_runner"]
[ext_resource type="PackedScene" uid="uid://b0maze0view3d" path="res://scenes/MazeView3D.tscn" id="5_view3d"]

[node name="Main" type="Node"]
script = ExtResource("1_main")

[node name="MazeView2D" parent="." instance=ExtResource("3_view2d")]

[node name="MazeView3D" parent="." instance=ExtResource("5_view3d")]

[node name="Runner" type="Node" parent="."]
script = ExtResource("4_runner")

[node name="Hud" parent="." instance=ExtResource("2_hud")]
```

- [x] **Step 2: `Main.cs` View-Toggle implementieren**

In `Main.cs`:

```csharp
private MazeView3D _view3D = null!;

// in _Ready():
_view3D = GetNode<MazeView3D>("MazeView3D");

// OnGenerateRequested erweitern, damit auch 3D das neue Maze bekommt:
_view2D.SetMaze(_currentMaze);
_view3D.SetMaze(_currentMaze);

// OnGenerationFinished erweitern:
_view2D.Refresh();
_view3D.Refresh();

// OnViewToggled neu:
private void OnViewToggled(bool use3D)
{
    _view2D.Visible = !use3D;
    _view3D.Visible =  use3D;
    GD.Print($"[Main] 3D-Ansicht = {use3D}");
}
```

> Hinweis: Da `MazeView3D.tscn` mit `visible = false` startet, ist der Default die 2D-Ansicht. Erst Klick auf den Toggle aktiviert 3D.

> **Performance-Hinweis (nachträglich korrigiert):** `MazeView3D.SetMaze()` wird **nicht** in `OnGenerateRequested` und **nicht** in `OnGenerationStepProduced` aufgerufen – das wäre für jeden einzelnen Generierungsschritt zu langsam. Stattdessen wird `_view3D.SetMaze(_currentMaze)` exakt **einmal** in `OnGenerationFinished` aufgerufen, sobald das Maze vollständig ist. In `OnViewToggled` wird zusätzlich `SetMaze` aufgerufen, falls der Nutzer auf 3D umschaltet bevor die View das erste Mal gebaut wurde.

- [ ] **Step 3: Build und Test**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
& $env:GODOT4 --path $PWD
```

Erwartet: Erstellen erzeugt das Maze, "3D-Ansicht" aktivieren zeigt es als Wandquader-Modell. Wechsel beidseitig möglich.

---

## Phase 8 — Solver-Schnittstelle

Ziel: Genau wie `IMazeGenerator`, aber für Lösungsalgorithmen. Solver werden vom selben `AlgorithmRunner` getrieben.

### Task 8.1: `SolverStep` und `IMazeSolver`

**Files:**
- Create: `scripts/Solvers/SolverStep.cs`
- Create: `scripts/Solvers/IMazeSolver.cs`

- [ ] **Step 1: `SolverStep.cs` anlegen**

```csharp
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Ein einzelner Animationsschritt eines Solvers.
/// </summary>
public sealed record SolverStep(
    Cell Cell,
    CellState NewState,
    int Distance,
    string Description
);
```

- [ ] **Step 2: `IMazeSolver.cs` anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

public interface IMazeSolver
{
    string Id   { get; }
    string Name { get; }

    /// <summary>
    /// Liefert Animationsschritte. Implementierung markiert am Ende den finalen Pfad
    /// (CellState.Path) und gibt die letzten Schritte als Path-Updates aus.
    /// </summary>
    IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal);
}
```

### Task 8.2: AlgorithmRunner und Main solverfähig machen

`AlgorithmRunner` ist bereits aus Phase 4 solverfähig (StartSolver/AdvanceSolver/SolverStep-Felder). In Main:

**Files:**
- Modify: `scripts/Main.cs`

- [ ] **Step 1: Solver-Registry und Handler ergänzen**

```csharp
using Maze.Solvers;
// ...

private readonly Dictionary<string, IMazeSolver> _solvers = new();

// in _Ready():
_runner.SolverStepProduced += OnSolverStepProduced;
_runner.SolverFinished     += OnSolverFinished;

// neue Methoden:
private void OnSolveRequested(string solverId)
{
    if (_currentMaze is null)        { GD.PrintErr("Kein Maze."); return; }
    if (!_solvers.TryGetValue(solverId, out var solver))
    {
        GD.PrintErr($"Unbekannter Solver: {solverId}");
        return;
    }

    _currentMaze.ResetSolverState();
    Cell start = _currentMaze.GetCell(0, 0);
    Cell goal  = _currentMaze.GetCell(_currentMaze.Width - 1, _currentMaze.Height - 1);
    start.State = CellState.Start;
    goal.State  = CellState.Goal;
    _view2D.Refresh();
    _view3D.Refresh();

    _runner.StopAll();
    _runner.StartSolver(solver.Solve(_currentMaze, start, goal));
}

private void OnSolverStepProduced()
{
    var step = _runner.LastSolverStep;
    if (step is null) return;
    step.Cell.State    = step.NewState;
    step.Cell.Distance = step.Distance;
    _view2D.Refresh();
}

private void OnSolverFinished()
{
    GD.Print("[Main] Solver fertig.");
    _view2D.Refresh();
}
```

> Damit der Solver-Toggle die View nicht mit Carving-Resten überschreibt, ruft `OnSolveRequested` `ResetSolverState` auf — Generation-Wände bleiben dabei erhalten.

---

## Phase 9 — Solver-Implementierungen

Sechs Algorithmen — die "kuratierte Auswahl" aus dem Briefing.

### Task 9.1: Breadth-First Search

**Files:**
- Create: `scripts/Solvers/BreadthFirstSolver.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Klassische Breitensuche. Garantiert kürzesten Pfad in ungewichteten Gittern.
/// Visualisiert sich als Welle vom Start aus.
/// </summary>
public sealed class BreadthFirstSolver : IMazeSolver
{
    public string Id   => "bfs";
    public string Name => "Breadth-First Search";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        var queue       = new Queue<Cell>();
        var cameFrom    = new Dictionary<Cell, Cell?>();
        var distances   = new Dictionary<Cell, int>();

        queue.Enqueue(start);
        cameFrom[start]  = null;
        distances[start] = 0;
        yield return new SolverStep(start, CellState.Frontier, 0, "Start in Frontier");

        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();
            if (current.State != CellState.Start && current.State != CellState.Goal)
                yield return new SolverStep(current, CellState.Visited, distances[current], "Visit");

            if (current == goal) break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction)) continue;
                Cell? next = maze.GetNeighbor(current, direction);
                if (next is null || cameFrom.ContainsKey(next)) continue;

                cameFrom[next]  = current;
                distances[next] = distances[current] + 1;
                queue.Enqueue(next);
                if (next != goal)
                    yield return new SolverStep(next, CellState.Frontier, distances[next], "Enqueue");
            }
        }

        // Pfad zurückverfolgen.
        if (!cameFrom.ContainsKey(goal)) yield break;
        var path = new List<Cell>();
        for (Cell? c = goal; c != null; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        for (int i = 0; i < path.Count; i++)
            yield return new SolverStep(path[i], CellState.Path, i, "Path");
    }
}
```

- [ ] **Step 2: In Main registrieren**

```csharp
private readonly Dictionary<string, IMazeSolver> _solvers = new()
{
    ["bfs"] = new BreadthFirstSolver()
};
```

### Task 9.2: Depth-First Search Solver

**Files:**
- Create: `scripts/Solvers/DepthFirstSolver.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// DFS-Solver. Findet einen Pfad, aber nicht zwingend den kürzesten.
/// Visuell wie eine Schlange, die sich tief in einen Ast schlängelt.
/// </summary>
public sealed class DepthFirstSolver : IMazeSolver
{
    public string Id   => "dfs";
    public string Name => "Depth-First Search";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        var stack    = new Stack<Cell>();
        var cameFrom = new Dictionary<Cell, Cell?>();
        var depth    = new Dictionary<Cell, int>();

        stack.Push(start);
        cameFrom[start] = null;
        depth[start]    = 0;
        yield return new SolverStep(start, CellState.Frontier, 0, "Start auf Stack");

        while (stack.Count > 0)
        {
            Cell current = stack.Pop();
            if (current.State != CellState.Start && current.State != CellState.Goal)
                yield return new SolverStep(current, CellState.Visited, depth[current], "Visit");
            if (current == goal) break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction)) continue;
                Cell? next = maze.GetNeighbor(current, direction);
                if (next is null || cameFrom.ContainsKey(next)) continue;

                cameFrom[next] = current;
                depth[next]    = depth[current] + 1;
                stack.Push(next);
                if (next != goal)
                    yield return new SolverStep(next, CellState.Frontier, depth[next], "Push");
            }
        }

        if (!cameFrom.ContainsKey(goal)) yield break;
        var path = new List<Cell>();
        for (Cell? c = goal; c != null; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        for (int i = 0; i < path.Count; i++)
            yield return new SolverStep(path[i], CellState.Path, i, "Path");
    }
}
```

### Task 9.3: A* Solver

**Files:**
- Create: `scripts/Solvers/AStarSolver.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// A*-Solver mit Manhattan-Heuristik (passt für 4er-Nachbarschaft).
/// f(n) = g(n) + h(n), expandiert immer das niedrigste f.
/// </summary>
public sealed class AStarSolver : IMazeSolver
{
    public string Id   => "a-star";
    public string Name => "A* (Manhattan)";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        // Wir nutzen eine SortedSet als Priority Queue, mit Tiebreaker per Insert-Counter.
        long counter = 0;
        var openSet = new SortedSet<(int f, long counter, Cell cell)>(Comparer<(int,long,Cell)>.Create(
            (a, b) =>
            {
                int c = a.f.CompareTo(b.f);
                if (c != 0) return c;
                return a.counter.CompareTo(b.counter);
            }));

        var gScore   = new Dictionary<Cell, int> { [start] = 0 };
        var cameFrom = new Dictionary<Cell, Cell?> { [start] = null };
        openSet.Add((Heuristic(start, goal), counter++, start));
        yield return new SolverStep(start, CellState.Frontier, 0, "Start");

        while (openSet.Count > 0)
        {
            var (_, _, current) = openSet.Min;
            openSet.Remove(openSet.Min);

            if (current.State != CellState.Start && current.State != CellState.Goal)
                yield return new SolverStep(current, CellState.Visited, gScore[current], "Expand");
            if (current == goal) break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction)) continue;
                Cell? neighbor = maze.GetNeighbor(current, direction);
                if (neighbor is null) continue;

                int tentativeG = gScore[current] + 1;
                if (gScore.TryGetValue(neighbor, out int known) && tentativeG >= known) continue;

                cameFrom[neighbor] = current;
                gScore[neighbor]   = tentativeG;
                int f = tentativeG + Heuristic(neighbor, goal);
                openSet.Add((f, counter++, neighbor));

                if (neighbor != goal)
                    yield return new SolverStep(neighbor, CellState.Frontier, tentativeG, $"Open f={f}");
            }
        }

        if (!cameFrom.ContainsKey(goal)) yield break;
        var path = new List<Cell>();
        for (Cell? c = goal; c != null; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        for (int i = 0; i < path.Count; i++)
            yield return new SolverStep(path[i], CellState.Path, i, "Path");
    }

    private static int Heuristic(Cell a, Cell b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
```

### Task 9.4: Greedy Best-First

**Files:**
- Create: `scripts/Solvers/GreedyBestFirstSolver.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Greedy Best-First nutzt nur die Heuristik h(n), keine Pfadkosten g(n).
/// Sehr schnell, aber findet meist nicht den kürzesten Pfad.
/// </summary>
public sealed class GreedyBestFirstSolver : IMazeSolver
{
    public string Id   => "greedy";
    public string Name => "Greedy Best-First";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        long counter = 0;
        var openSet = new SortedSet<(int h, long counter, Cell cell)>(Comparer<(int,long,Cell)>.Create(
            (a, b) =>
            {
                int c = a.h.CompareTo(b.h);
                if (c != 0) return c;
                return a.counter.CompareTo(b.counter);
            }));

        var cameFrom = new Dictionary<Cell, Cell?> { [start] = null };
        openSet.Add((Heuristic(start, goal), counter++, start));
        yield return new SolverStep(start, CellState.Frontier, 0, "Start");

        while (openSet.Count > 0)
        {
            var (_, _, current) = openSet.Min;
            openSet.Remove(openSet.Min);

            if (current.State != CellState.Start && current.State != CellState.Goal)
                yield return new SolverStep(current, CellState.Visited, Heuristic(current, goal), "Expand");
            if (current == goal) break;

            foreach (var direction in DirectionHelper.All)
            {
                if (current.HasWall(direction)) continue;
                Cell? neighbor = maze.GetNeighbor(current, direction);
                if (neighbor is null || cameFrom.ContainsKey(neighbor)) continue;

                cameFrom[neighbor] = current;
                openSet.Add((Heuristic(neighbor, goal), counter++, neighbor));
                if (neighbor != goal)
                    yield return new SolverStep(neighbor, CellState.Frontier, Heuristic(neighbor, goal), "Open");
            }
        }

        if (!cameFrom.ContainsKey(goal)) yield break;
        var path = new List<Cell>();
        for (Cell? c = goal; c != null; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        for (int i = 0; i < path.Count; i++)
            yield return new SolverStep(path[i], CellState.Path, i, "Path");
    }

    private static int Heuristic(Cell a, Cell b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
```

### Task 9.5: Wall Follower (Linke-Hand-Regel)

**Files:**
- Create: `scripts/Solvers/WallFollowerSolver.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Linke-Hand-Regel. Funktioniert garantiert in einfach zusammenhängenden Labyrinthen.
/// Der "Agent" hat eine Blickrichtung; er versucht zuerst links abzubiegen,
/// dann geradeaus, dann rechts, dann zurück.
/// </summary>
public sealed class WallFollowerSolver : IMazeSolver
{
    public string Id   => "wall-follower";
    public string Name => "Wall Follower (Linke Hand)";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        Cell      current  = start;
        Direction facing   = Direction.North;
        int       step     = 0;
        var       visited  = new HashSet<Cell> { start };
        var       cameFrom = new Dictionary<Cell, Cell?> { [start] = null };

        yield return new SolverStep(start, CellState.Frontier, 0, "Start");

        while (current != goal && step < maze.Width * maze.Height * 4)
        {
            // Reihenfolge: links, geradeaus, rechts, kehrt.
            foreach (var dir in new[] { TurnLeft(facing), facing, TurnRight(facing), TurnAround(facing) })
            {
                if (current.HasWall(dir)) continue;
                Cell? next = maze.GetNeighbor(current, dir);
                if (next is null) continue;

                facing = dir;
                if (!cameFrom.ContainsKey(next)) cameFrom[next] = current;
                current = next;
                if (visited.Add(current))
                    yield return new SolverStep(current, CellState.Visited, ++step, "Walk");
                else
                    yield return new SolverStep(current, CellState.Frontier, step, "Revisit");
                break;
            }
        }

        if (current != goal) yield break;
        var path = new List<Cell>();
        for (Cell? c = goal; c != null; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        for (int i = 0; i < path.Count; i++)
            yield return new SolverStep(path[i], CellState.Path, i, "Path");
    }

    private static Direction TurnLeft(Direction d)    => (Direction)(((int)d + 3) % 4);
    private static Direction TurnRight(Direction d)   => (Direction)(((int)d + 1) % 4);
    private static Direction TurnAround(Direction d)  => (Direction)(((int)d + 2) % 4);
}
```

### Task 9.6: Dead-End Filling

**Files:**
- Create: `scripts/Solvers/DeadEndFillingSolver.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Solvers;

/// <summary>
/// Dead-End Filling: füllt iterativ alle Sackgassen mit "Wand" auf, bis nur noch
/// (Pfad-)Korridore bleiben. Funktioniert nur in perfekten Labyrinthen, in denen
/// genau ein Pfad zwischen Start und Ziel existiert.
/// </summary>
public sealed class DeadEndFillingSolver : IMazeSolver
{
    public string Id   => "dead-end-filling";
    public string Name => "Dead-End Filling";

    public IEnumerable<SolverStep> Solve(Model.Maze maze, Cell start, Cell goal)
    {
        // wallsBlocked[(cell, dir)] => Wand wird simuliert geschlossen.
        // In dieser einfachen Variante markieren wir Zellen direkt als Filled.
        var filled = new HashSet<Cell>();
        bool changed = true;
        int distance = 0;

        while (changed)
        {
            changed = false;
            foreach (var cell in maze.AllCells())
            {
                if (filled.Contains(cell) || cell == start || cell == goal) continue;
                if (CountOpenNeighbors(maze, cell, filled) <= 1)
                {
                    filled.Add(cell);
                    yield return new SolverStep(cell, CellState.Filled, distance, "Fill");
                    changed = true;
                }
            }
            distance++;
        }

        // Restzellen ergeben den Pfad.
        var path = new List<Cell>();
        Cell current = start;
        var cameFrom = new Dictionary<Cell, Cell?> { [start] = null };
        var queue = new Queue<Cell>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            current = queue.Dequeue();
            if (current == goal) break;
            foreach (var dir in DirectionHelper.All)
            {
                if (current.HasWall(dir)) continue;
                Cell? next = maze.GetNeighbor(current, dir);
                if (next is null || filled.Contains(next) || cameFrom.ContainsKey(next)) continue;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal)) yield break;
        for (Cell? c = goal; c != null; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        for (int i = 0; i < path.Count; i++)
            yield return new SolverStep(path[i], CellState.Path, i, "Path");
    }

    private static int CountOpenNeighbors(Model.Maze maze, Cell cell, HashSet<Cell> filled)
    {
        int count = 0;
        foreach (var dir in DirectionHelper.All)
        {
            if (cell.HasWall(dir)) continue;
            Cell? n = maze.GetNeighbor(cell, dir);
            if (n is null || filled.Contains(n)) continue;
            count++;
        }
        return count;
    }
}
```

- [ ] **Step 7: Alle Solver in Main registrieren**

```csharp
private readonly Dictionary<string, IMazeSolver> _solvers = new()
{
    ["bfs"]              = new BreadthFirstSolver(),
    ["dfs"]              = new DepthFirstSolver(),
    ["a-star"]           = new AStarSolver(),
    ["greedy"]           = new GreedyBestFirstSolver(),
    ["wall-follower"]    = new WallFollowerSolver(),
    ["dead-end-filling"] = new DeadEndFillingSolver()
};
```

- [ ] **Step 8: Build & Test**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
& $env:GODOT4 --path $PWD
```

Erwartet: Generieren -> Solver auswählen -> "Lösen" zeichnet die Welle / Schlange / heuristische Suche und am Schluss den Pfad.

---

## Phase 10 — Performance- und Speicheranzeige

Ziel: `PerformanceTracker` misst Stoppuhr und Speicher; `StatsPanel` zeigt Ergebnisse live an.

### Task 10.1: `PerformanceTracker`

**Files:**
- Create: `scripts/PerformanceTracker.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using System;
using System.Diagnostics;

namespace Maze;

/// <summary>
/// Misst Laufzeit, Speicherzuwachs und Anzahl Schritte für einen Algorithmus-Lauf.
/// Bewusst einfach gehalten — keine Statistik-Bibliothek, damit Schüler den Code lesen können.
/// </summary>
public sealed class PerformanceTracker
{
    private readonly Stopwatch _stopwatch = new();
    private long _memoryBefore;

    public TimeSpan Elapsed   => _stopwatch.Elapsed;
    public int      Steps     { get; private set; }
    public int      VisitedCells { get; private set; }
    public int      PathLength   { get; private set; }
    public long     ManagedMemoryDeltaBytes { get; private set; }

    public void Start()
    {
        Steps = 0;
        VisitedCells = 0;
        PathLength   = 0;
        // GC sauber machen, damit das Delta aussagekräftig ist.
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        _memoryBefore = GC.GetTotalMemory(false);
        _stopwatch.Restart();
    }

    public void TickStep() => Steps++;

    public void IncrementVisited() => VisitedCells++;

    public void SetPathLength(int length) => PathLength = length;

    public void Stop()
    {
        _stopwatch.Stop();
        long after = GC.GetTotalMemory(false);
        ManagedMemoryDeltaBytes = after - _memoryBefore;
    }
}
```

### Task 10.2: `StatsPanel.cs` und Szene

**Files:**
- Create: `scripts/Hud/StatsPanel.cs`
- Create: `scenes/StatsPanel.tscn`

- [ ] **Step 1: `StatsPanel.cs` anlegen**

```csharp
using Godot;
using System;

namespace Maze.UI;

/// <summary>
/// Schwebendes Panel rechts unten, das die aktuellen Tracker-Werte anzeigt.
/// Wird von Main bei jedem Step und am Ende mit Werten gefüttert.
/// </summary>
public partial class StatsPanel : PanelContainer
{
    private Label _timeLabel    = null!;
    private Label _stepsLabel   = null!;
    private Label _visitedLabel = null!;
    private Label _pathLabel    = null!;
    private Label _memoryLabel  = null!;

    public override void _Ready()
    {
        _timeLabel    = GetNode<Label>("Margin/VBox/TimeLabel");
        _stepsLabel   = GetNode<Label>("Margin/VBox/StepsLabel");
        _visitedLabel = GetNode<Label>("Margin/VBox/VisitedLabel");
        _pathLabel    = GetNode<Label>("Margin/VBox/PathLabel");
        _memoryLabel  = GetNode<Label>("Margin/VBox/MemoryLabel");
    }

    public void UpdateStats(TimeSpan elapsed, int steps, int visited, int pathLength, long memoryDeltaBytes)
    {
        _timeLabel.Text    = $"Zeit:      {elapsed.TotalMilliseconds:F1} ms";
        _stepsLabel.Text   = $"Schritte:  {steps}";
        _visitedLabel.Text = $"Besucht:   {visited}";
        _pathLabel.Text    = $"Pfadlänge: {pathLength}";
        _memoryLabel.Text  = $"Speicher:  {memoryDeltaBytes / 1024.0:F1} KB";
    }
}
```

- [ ] **Step 2: `StatsPanel.tscn` anlegen**

```text
[gd_scene load_steps=2 format=3 uid="uid://b0maze0stats"]

[ext_resource type="Script" path="res://scripts/Hud/StatsPanel.cs" id="1_stats"]

[node name="StatsPanel" type="PanelContainer"]
anchor_left = 1.0
anchor_top = 1.0
anchor_right = 1.0
anchor_bottom = 1.0
offset_left = -260.0
offset_top = -160.0
script = ExtResource("1_stats")

[node name="Margin" type="MarginContainer" parent="."]
theme_override_constants/margin_left = 12
theme_override_constants/margin_top = 8
theme_override_constants/margin_right = 12
theme_override_constants/margin_bottom = 8

[node name="VBox" type="VBoxContainer" parent="Margin"]

[node name="TimeLabel" type="Label" parent="Margin/VBox"]
text = "Zeit: -"

[node name="StepsLabel" type="Label" parent="Margin/VBox"]
text = "Schritte: -"

[node name="VisitedLabel" type="Label" parent="Margin/VBox"]
text = "Besucht: -"

[node name="PathLabel" type="Label" parent="Margin/VBox"]
text = "Pfadlänge: -"

[node name="MemoryLabel" type="Label" parent="Margin/VBox"]
text = "Speicher: -"
```

- [ ] **Step 3: StatsPanel in Hud einhängen**

In `Hud.tscn` ans Ende einfügen (vor allen anderen Knoten löschen NICHT!):

```text
[ext_resource type="PackedScene" uid="uid://b0maze0stats" path="res://scenes/StatsPanel.tscn" id="2_stats"]
```

(als zusätzliche `[ext_resource]`-Zeile direkt unter dem Skript-Resource)

und am Ende:

```text
[node name="StatsPanel" parent="." instance=ExtResource("2_stats")]
```

- [ ] **Step 4: Main mit Tracker verdrahten**

```csharp
private readonly PerformanceTracker _tracker = new();
private StatsPanel _stats = null!;

// in _Ready():
_stats = GetNode<StatsPanel>("Hud/StatsPanel");

// OnGenerateRequested erweitern (am Anfang):
_tracker.Start();

// OnGenerationStepProduced erweitern (am Ende):
_tracker.TickStep();
_tracker.IncrementVisited();
_stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, 0, 0);

// OnGenerationFinished erweitern:
_tracker.Stop();
_stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, 0, _tracker.ManagedMemoryDeltaBytes);

// OnSolveRequested erweitern (am Anfang):
_tracker.Start();

// OnSolverStepProduced erweitern:
_tracker.TickStep();
if (_runner.LastSolverStep is { NewState: CellState.Visited }) _tracker.IncrementVisited();
if (_runner.LastSolverStep is { NewState: CellState.Path } pathStep) _tracker.SetPathLength(pathStep.Distance + 1);
_stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, 0);

// OnSolverFinished erweitern:
_tracker.Stop();
_stats.UpdateStats(_tracker.Elapsed, _tracker.Steps, _tracker.VisitedCells, _tracker.PathLength, _tracker.ManagedMemoryDeltaBytes);
```

> Hinweis: Wegen der typisierten Cell-States verwenden wir `CellState`-Pattern-Matching. Achte darauf, `using Maze.Model;` zu importieren.

- [ ] **Step 5: Build & Test**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
& $env:GODOT4 --path $PWD
```

Erwartet: Beim Erstellen / Lösen aktualisieren sich die Anzeigen live, am Ende mit finalem Speicher-Delta.

---

## Phase 11 — Cheat-Sheet & Politur

Ziel: Letzter Schliff für die 2D-Ansicht als didaktisches "Cheat-Sheet".

### Task 11.1: Heatmap-Toggle im HUD

**Files:**
- Modify: `scenes/Hud.tscn` (CheckBox hinzufügen)
- Modify: `scripts/Hud/Hud.cs` (Signal + Wiring)
- Modify: `scripts/Main.cs` (View-Property setzen)

- [ ] **Step 1: In `Hud.tscn` neben dem View3DToggle einfügen**

```text
[node name="HeatmapToggle" type="CheckBox" parent="Root/Margin/VBox/Algos"]
text = "Distanz-Heatmap"
```

- [ ] **Step 2: In `Hud.cs`**

```csharp
[Signal] public delegate void HeatmapToggleEventHandler(bool enabled);

private CheckBox _heatmapToggle = null!;

// in _Ready() ergänzen:
_heatmapToggle = GetNode<CheckBox>("Root/Margin/VBox/Algos/HeatmapToggle");
_heatmapToggle.Toggled += enabled => EmitSignal(SignalName.HeatmapToggle, enabled);
```

- [ ] **Step 3: In `Main.cs`**

```csharp
// in _Ready():
_hud.HeatmapToggle += enabled =>
{
    _view2D.ShowDistances = enabled;
    _view2D.Refresh();
};
```

### Task 11.2: Tastatur-Shortcut für "Step"

**Files:**
- Modify: `scripts/Main.cs`

- [ ] **Step 1: `_UnhandledInput` ergänzen**

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    if (@event is InputEventKey { Pressed: true, Keycode: Key.Space } && _runner.IsPaused)
        _runner.ForceSingleStep();
}
```

> Damit ist Leertaste = "Schritt", solange Pause aktiv ist.

### Task 11.3: Smoke-Run abschließen

- [ ] **Step 1: Build, Headless-Smoke, manueller Visualtest**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
& $env:GODOT4 --headless --path $PWD --quit --verbose
& $env:GODOT4 --path $PWD
```

- [ ] **Step 2: Manuelle Test-Checkliste**

- [ ] Generate Recursive Backtracker @ 25×25 zeigt animierte Carving-Welle
- [ ] Generate Growing Tree zeigt anderen visuellen Charakter
- [ ] Generate Recursive Division zeigt geometrisch wirkendes Layout
- [ ] BFS zeigt symmetrische Wellenfront
- [ ] DFS zeigt schlangenartiges Suchen
- [ ] A* expandiert zielgerichtet
- [ ] Greedy verfolgt Heuristik dogmatisch (oft suboptimal)
- [ ] Wall Follower bewegt sich agentenartig
- [ ] Dead-End Filling füllt von außen nach innen
- [ ] 3D-Toggle wechselt zur Wandquader-Ansicht
- [ ] Heatmap-Toggle färbt Distanzen ein
- [ ] Pause/Step funktionieren mit Maus und Leertaste
- [ ] Reset stellt Zustand sauber her
- [ ] StatsPanel zeigt Zeit, Schritte, Pfadlänge, Speicher

---

## Optionale Erweiterungen (nicht im Pflicht-Plan)

Wenn ihr nach Abschluss von Phase 11 noch Zeit habt, sind folgende Punkte didaktisch besonders ergiebig:

- **Bidirektionales BFS:** zwei Wellenfronten, treffen sich in der Mitte. Schöner Vergleich zu BFS.
- **Trémaux-Algorithmus:** zwei-Strich-Markierungen. Funktioniert auch in nicht perfekten Labyrinthen.
- **Pledge-Algorithmus:** Wall Follower mit Winkelzähler.
- **Echtes 3D-Maze:** statt 2D-Maze + Extrusion eine 3D-`Maze`-Datenstruktur (sechs Nachbarn pro Zelle), eigener `RecursiveBacktracker3D`-Generator. Erweitert `IMazeGenerator`/`IMazeSolver` um eine optionale 3D-Variante.
- **Algorithmus-Vergleichs-Modus:** zwei Solver gleichzeitig auf identischer Maze-Kopie laufen lassen, side-by-side.
- **Replay/Export:** Schritte in eine Datei schreiben (CSV) und in der App wieder einlesen — für deterministische Demos im Klassenzimmer.

---

## Self-Review-Notizen

- Spec-Abdeckung:
  - 4 Generatoren (Phase 5 + 6): Recursive Backtracker, Growing Tree, Recursive Division, Cellular Automata ✔
  - 6 Solver (Phase 9): BFS, DFS, A*, Greedy, Wall Follower, Dead-End Filling ✔
  - 3D + 2D-Ansicht (Phase 3 + 7) ✔
  - Cheat-Sheet (Heatmap, Statspanel) (Phase 10 + 11) ✔
  - Performance/Speicher (Phase 10) ✔
  - Runtime-Konfiguration Größe + Tempo (Phase 2) ✔
  - Schritt-für-Schritt-Animation (Phase 4 + AlgorithmRunner) ✔
  - Schülerlesbar (deutsche Kommentare, klare Variablennamen) ✔
- Keine Platzhalter (TBD/TODO) im Code, alle Tasks haben vollständige Implementierungen.
- Typkonsistenz geprüft: `Cell`, `Maze.Model.Maze`, `Direction`, `CellState`, `GenerationStep`, `SolverStep`, `IMazeGenerator`/`IMazeSolver` werden in allen Phasen identisch verwendet. Namespace `Maze.Model` wird durchgängig verwendet.
- `Maze.Model.Maze` und `Maze`-Namespace kollidieren nicht, weil der Top-Level-Namespace `Maze` heißt und die Klasse `Maze.Model.Maze` über vollqualifizierten Pfad `Model.Maze` referenziert wird (siehe `_currentMaze` Felddeklaration).
