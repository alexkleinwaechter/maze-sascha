# Maze School Project — First-Person-Modus (Phasen 20–21) — Implementierungsplan

> **Für agentische Worker:** REQUIRED SUB-SKILL: Verwende `superpowers:subagent-driven-development` (empfohlen) oder `superpowers:executing-plans` zur task-by-task-Umsetzung. Schritte verwenden Checkbox-Syntax (`- [ ]`).
>
> **Für Schüler:** Du kannst die Phasen und Tasks einzeln nacheinander durcharbeiten. Jeder Task ist so aufgebaut, dass er für sich genommen lauffähigen Code produziert (`dotnet build` bricht nicht, das Spiel startet weiterhin). Kommentare im Code sind bewusst ausführlich.
>
> **Vorläufer-Plan A:** [`2026-04-28-maze-school-project.md`](2026-04-28-maze-school-project.md) (Phasen 0–11 — Grundgerüst, Generatoren, Solver, 2D/3D-Views, HUD).
>
> **Vorläufer-Plan B:** [`2026-04-29-maze-improvements.md`](2026-04-29-maze-improvements.md) (Phasen 12–15 — große Mazes, Tempo-frei, frei steuerbare 3D-Kamera, 2D-Pan/Zoom).
>
> **Vorläufer-Plan C:** [`2026-04-30-maze-gamification.md`](2026-04-30-maze-gamification.md) (Phasen 16–19 — Solver-Bot, Selbst-Spiel-Modus, Entdeckungs-Modus, Lego-Figur).

**Goal:** Einen First-Person-Modus wie in einem klassischen Ego-Shooter ergänzen. Die Kamera sitzt auf Augenhöhe der Lego-Figur, die Blickrichtung folgt der Maus, und der Körper der Spielfigur dreht sich beim Umsehen synchron mit (so dass W immer "nach vorne, dorthin wo ich schaue" bedeutet). Zusätzlich gibt es einen **Free-Look-Modus** (`Alt`-Taste halten): solange `Alt` gedrückt ist, dreht sich nur die Kamera, der Körper bleibt stehen — der Spieler kann also umsehen, ohne seine Bewegungsrichtung zu wechseln. Nach dem Loslassen schwenkt die Kamera weich zurück auf die Körper-Vorwärtsrichtung.

**Didaktischer Bogen:** Phase 17 (Selbst spielen) zeigte das Maze aus der Vogelperspektive mit Verfolger-Kamera. Phase 20 ändert die Perspektive auf "ich stehe selbst zwischen den Wänden" — das macht den Suchraum noch unmittelbarer. Phase 21 führt das Konzept "Kopf vs. Körper" ein: Hardware (Maus) steuert per Default beides gemeinsam; mit einem Modifier kann man Kopf und Körper entkoppeln. Daraus lässt sich im Unterricht das Thema *Eingabe-Mapping* und *Modi vs. Modifier-Tasten* aufgreifen.

**Architecture:** Zwei aufeinander aufbauende Phasen, beide ändern primär `MazeView3D.tscn`, `CameraController3D.cs` und `PlayerCharacter3D.cs`.

- **Phase 20** ergänzt einen zweiten Camera3D-Knoten als Kind eines neuen `HeadAnchor`-Pivots unter `Player`. Der HeadAnchor sitzt auf Augenhöhe (~0,6 m über dem Boden, oberhalb der Lego-Figur). Eine HUD-Checkbox "First-Person" schaltet zwischen der bestehenden Free/Follow-Kamera und der neuen Ego-Kamera um. Der `FirstPersonCameraController` liest Mausbewegung und schreibt **Yaw** in `Player.Rotation.Y` (also die Körperdrehung der gesamten Spielfigur) und **Pitch** in `HeadAnchor.Rotation.X` (nur der Kopf nickt). Diese Trennung ist Standard-Best-Practice für FPS-Kameras: zwei separate Achsen auf zwei separate Knoten verhindern Gimbal-Lock und halten die Bewegungslogik einfach (W folgt immer Player-Forward).
- **Phase 21** baut darauf den Free-Look-Modus auf. Solange `Alt` gehalten wird, schreibt die Mausbewegung Yaw nicht mehr in `Player.Rotation.Y`, sondern in einen *zusätzlichen* Yaw-Offset auf dem `HeadAnchor`. Das entkoppelt Kopf und Körper. Beim Loslassen läuft per Tween eine 0,15-s-Lerp-Animation auf 0° zurück, sodass der Blick weich zur Körper-Vorwärtsrichtung snapt. Pitch verhält sich unverändert (immer am HeadAnchor).

**Recherche-Quellen (Stand Mai 2026):**
- [Yo Soy Freeman — Achieving better mouse input in Godot 4: The perfect camera controller](https://yosoyfreeman.github.io/article/godot/tutorial/achieving-better-mouse-input-in-godot-4-the-perfect-camera-controller/) — Best Practices: Yaw auf Body, Pitch auf Head, Pitch-Clamp ±89°, `Input.UseAccumulatedInput = false`, `Basis.Orthonormalize()` nach jeder Drehung.
- [Wikipedia — Free look](https://en.wikipedia.org/wiki/Free_look) — historische Definition von "Mouselook" als unabhängige Blickrichtung.
- [TV Tropes — Freelook Button](https://tvtropes.org/pmwiki/pmwiki.php/Main/FreelookButton) — Konvention: Hold-Modifier-Taste (`Alt`/`CapsLock`) löst die Kamera vorübergehend vom Körper.
- [Cyberpunk 2077 ImmersiveFirstPerson Mod](https://github.com/cp2077/ImmersiveFirstPerson) — referenzt Free-Look als Industriestandard für moderne Ego-Spiele.

**Tech Stack:**
- Godot 4.6.2 .NET (mono), C# 12 (`<TargetFramework>net8.0</TargetFramework>`)
- Forward+ Renderer mit D3D12
- Windows + PowerShell Workflow gemäß `.github/skills/godot-csharp-windows`
- Godot Executable: `$env:GODOT4` (Fallback `C:\temp\_godot\Godot_v4.6.2-stable_mono_win64.exe`)

**Konventionen (aus den Vorläuferplänen übernommen):**
- Klassendateiname == Klassenname (Godot-C#-Pflicht).
- `public partial class` und `using Godot;` für jedes Godot-Skript.
- Englische Identifier, deutsche Kommentare bei didaktisch wertvollen Stellen.
- `null!` Backing-Field-Pattern für Knoten, die in `_Ready()` aufgelöst werden.
- Nach jedem Hinzufügen oder Umbenennen eines C#-Skripts mit `[Export]` oder `[Signal]`: `& $env:GODOT4 --path $PWD --build-solutions` ausführen.
- Reine Codeänderungen: `dotnet build` reicht.

**Designentscheidungen — bewusst getroffen:**

1. **Body-coupled Yaw als Default, nicht decoupled.**
   Im FPS-Modus dreht sich der Körper mit der Maus mit. Konsequenz: W bewegt sich immer in Blickrichtung — kein zusätzlicher Mental-Overhead beim Spielen. Free-Look ist die *Ausnahme*, nicht die Regel.

2. **Kamera als Kind eines neuen `HeadAnchor`-Pivots, NICHT als Kind von `LegoFigure/HeadPivot`.**
   Die Lego-Figur bobt den Kopf während der Lauf-Animation (`HeadPivot.Rotation = sin(v)/10`). Würden wir die Kamera daran hängen, hätten wir Motion-Sickness. Stattdessen sitzt die Kamera auf einem stabilen Welt-Anchor auf Augenhöhe.

3. **Cell-aligned Bewegung bleibt unverändert.**
   Die Manual-Mode-Bewegung (1 Zelle pro Tastendruck mit Wandkollision) wird nicht verändert. W zielt jetzt allerdings in die *Kamera*-Vorwärtsrichtung statt in eine fixe Welt-Achse — `GetManualDirectionFromView()` macht das bereits korrekt, weil es die aktive Kamera abfragt.

4. **Free-Look-Taste = `Alt` (nicht `CapsLock`).**
   `CapsLock` lässt sich auf Windows schlecht "halten" (es ist toggle-by-default), `Alt` ist der von z.B. Cyberpunk verwendete Standard und auch unter Windows als Hold-Key zuverlässig.

5. **Pitch-Clamp ±85° (nicht ±90°).**
   Knapp unter 90° lässt die Kamera nicht "überkippen" und vermeidet visuelle Artefakte am Zenit. Genauer Wert: `Mathf.DegToRad(85f)` ≈ 1.484 rad.

---

## Phase 20 — First-Person-Kamera mit Body-coupled Maus-Look

Ziel: Eine zweite, unter `Player` parented Camera3D, die per HUD-Toggle aktiv wird. Mausbewegung dreht den Körper (Yaw) und nickt den Kopf (Pitch). Der Cursor wird beim Aktivieren des Modus eingefangen und auf `Esc` (oder beim Verlassen des Modus) wieder freigegeben.

### Task 20.1: `HeadAnchor` und zweite `Camera3D` in `MazeView3D.tscn` anlegen

**Files:**
- Modify: `scenes/MazeView3D.tscn`

- [ ] **Step 1: Szene öffnen und Knoten ergänzen**

Öffne `scenes/MazeView3D.tscn` im Godot-Editor (oder direkt als Textdatei). Wir fügen unter dem bestehenden `Player`-Knoten einen `HeadAnchor` mit einer Kind-Camera3D hinzu. Wichtig: **NICHT** unterhalb von `Figure` aufhängen — die Lego-Figur ist mit Faktor 0.025 skaliert und ihr `HeadPivot` bobt während der Lauf-Animation. Beides wäre für eine Ego-Kamera unbrauchbar.

Direkt unter dem bestehenden `[node name="PlayerLight" ...]`-Block einfügen:

```text
[node name="HeadAnchor" type="Node3D" parent="Player"]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0.6, 0)

[node name="FirstPersonCamera" type="Camera3D" parent="Player/HeadAnchor"]
fov = 75.0
near = 0.05
current = false
script = ExtResource("6_fpcam")
```

Erweitere die `[ext_resource]`-Liste am Anfang der Datei um:

```text
[ext_resource type="Script" path="res://scripts/Views/FirstPersonCameraController.cs" id="6_fpcam"]
```

Erhöhe `load_steps` in der `[gd_scene]`-Zeile um 1 (für die neue ext_resource).

> **Hinweis:** `current = false` ist Pflicht — die Default-Kamera (`Camera3D` direkt unter `MazeView3D`) bleibt zunächst aktiv. `MakeCurrent()` schalten wir erst per Code beim FPS-Toggle. `near = 0.05` ist deutlich kleiner als der Godot-Default (0.05 statt 0.05 — bewusst klein, damit Wände direkt vor dem Gesicht nicht clippen). `fov = 75` ist ein für Ego-Spiele übliches Sichtfeld zwischen 60 (statisch) und 90 (Schnellschuss).

- [ ] **Step 2: Build prüfen**

Da das Skript `FirstPersonCameraController.cs` in 20.2 neu angelegt wird, kann `--build-solutions` jetzt fehlschlagen. Den Test erst nach 20.2 ausführen — hier erstmal nur sichtprüfen, dass die Szene korrekt im Editor öffnet.

- [ ] **Step 3: Commit**

```bash
git add scenes/MazeView3D.tscn
git commit -m "Task 20.1: HeadAnchor und FirstPersonCamera-Knoten in MazeView3D"
```

### Task 20.2: `FirstPersonCameraController` Skript

**Files:**
- Create: `scripts/Views/FirstPersonCameraController.cs`

- [ ] **Step 1: Datei anlegen**

```csharp
using Godot;

namespace Maze.Views;

/// <summary>
/// Ego-Kamera-Controller (First-Person-View). Sitzt als Kind von Player/HeadAnchor.
///
/// Architektur (Best Practice fuer FPS):
///   Player        -> Yaw   (Y-Rotation, also Koerper-Drehung)
///   HeadAnchor    -> Pitch (X-Rotation, also Kopf-Nicken)
///   Diese Camera  -> nur Position (relativ zum HeadAnchor)
///
/// Die zwei Achsen liegen bewusst auf zwei verschiedenen Knoten — das verhindert
/// Gimbal-Lock und entkoppelt Bewegungs-Forward (Player-Yaw) vom Blick-Pitch.
///
/// Phase 21 wird zusaetzlich einen Free-Look-Modus einfuehren, in dem nur der
/// HeadAnchor rotiert (Body bleibt stehen). Dieser Controller stellt dafuer
/// bereits den Hook <see cref="FreeLookHeld"/> zur Verfuegung.
/// </summary>
public partial class FirstPersonCameraController : Camera3D
{
    [Export] public float MouseSensitivity = 0.0022f;   // rad pro Mauspixel
    [Export] public float MaxPitchDeg = 85f;            // ±85° vermeidet Zenit-Artefakte
    [Export] public float KeyTurnSpeed = 1.8f;          // rad/s fuer Pfeiltasten als Maus-Backup

    /// <summary>
    /// Wird in Phase 21 von aussen gesetzt: solange true, geht Yaw NICHT in den Body,
    /// sondern in den HeadAnchor-Yaw-Offset. Phase 20 laesst dieses Flag immer false.
    /// </summary>
    public bool FreeLookHeld { get; set; }

    private Node3D _player = null!;       // Body-Yaw
    private Node3D _headAnchor = null!;   // Pitch (und in Phase 21: Free-Look-Yaw)

    private float _pitch;                 // aktueller Pitch in Radiant
    private bool _captured;               // Cursor-Status

    private float _maxPitchRad;

    public override void _Ready()
    {
        // Eingabe-Akkumulation deaktivieren — Best Practice fuer praezise Maus-Eingabe.
        // Sonst werden mehrere Mausbewegungen pro Frame zusammengefasst und das Bild ruckelt.
        Input.UseAccumulatedInput = false;

        _player = GetParent<Node3D>().GetParent<Node3D>();   // Camera -> HeadAnchor -> Player
        _headAnchor = GetParent<Node3D>();
        _maxPitchRad = Mathf.DegToRad(MaxPitchDeg);

        // Initialwerte aus aktuellem Transform ziehen, damit das erste Maus-Event
        // keinen sichtbaren Sprung verursacht.
        _pitch = _headAnchor.Rotation.X;
    }

    /// <summary>
    /// Aktiviert den FPS-Modus: Kamera wird Current, Cursor wird eingefangen.
    /// </summary>
    public void Activate()
    {
        MakeCurrent();
        CaptureMouse(true);
    }

    /// <summary>
    /// Verlaesst den FPS-Modus: Cursor wieder sichtbar machen. Die alte Kamera muss
    /// von aussen wieder Current geschaltet werden (siehe Main).
    /// </summary>
    public void Deactivate()
    {
        CaptureMouse(false);
    }

    private void CaptureMouse(bool capture)
    {
        _captured = capture;
        Input.MouseMode = capture ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
        if (!Current) return;

        // Pfeiltasten als Maus-Backup (Schueler ohne Maus / Touchpad-User).
        float yawDelta = 0f;
        float pitchDelta = 0f;
        if (Input.IsPhysicalKeyPressed(Key.Left))  yawDelta   -= KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Right)) yawDelta   += KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Up))    pitchDelta -= KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Down))  pitchDelta += KeyTurnSpeed * (float)delta;

        if (yawDelta != 0f || pitchDelta != 0f)
            ApplyLook(yawDelta, pitchDelta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Current) return;

        // Esc gibt den Cursor frei — wir verlassen den FPS-Modus aber NICHT;
        // den Modus-Switch macht Main per HUD-Toggle. Esc ist nur eine
        // Bequemlichkeit, damit man kurz auf das HUD klicken kann.
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            CaptureMouse(false);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Klick ins Spiel-Bild faengt den Cursor wieder ein, falls er per Esc
        // freigegeben wurde.
        if (@event is InputEventMouseButton { Pressed: true } mb && !_captured)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                CaptureMouse(true);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseMotion motion && _captured)
        {
            ApplyLook(motion.Relative.X * MouseSensitivity,
                      motion.Relative.Y * MouseSensitivity);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Wendet ein Yaw-/Pitch-Delta an. Yaw geht in Body (oder im Free-Look in HeadAnchor),
    /// Pitch immer in den HeadAnchor.
    /// </summary>
    private void ApplyLook(float yawDelta, float pitchDelta)
    {
        if (yawDelta != 0f)
        {
            // Phase 21: FreeLookHeld == true wuerde Yaw in HeadAnchor schreiben.
            // Phase 20: FreeLookHeld bleibt immer false, deshalb laeuft alles in den Body.
            Node3D yawTarget = FreeLookHeld ? _headAnchor : _player;
            // Negativ, weil positive Maus-X (rechts) "nach rechts schauen" bedeutet,
            // was in Godot's Y-up-Konvention einer NEGATIVEN Y-Rotation entspricht.
            yawTarget.RotateY(-yawDelta);
            yawTarget.Basis = yawTarget.Basis.Orthonormalized(); // Drift verhindern
        }

        if (pitchDelta != 0f)
        {
            _pitch = Mathf.Clamp(_pitch + pitchDelta, -_maxPitchRad, _maxPitchRad);
            // Pitch wird absolut gesetzt (nicht inkrementell), damit Clamp greift.
            _headAnchor.Rotation = new Vector3(_pitch, _headAnchor.Rotation.Y, 0f);
        }
    }

    public override void _Notification(int what)
    {
        // Fenster-Fokus verloren -> Cursor freigeben, sonst klemmt er unsichtbar.
        if (what == NotificationApplicationFocusOut && _captured)
            CaptureMouse(false);
    }
}
```

> **Best-Practice-Notizen:**
> - `Input.UseAccumulatedInput = false` reduziert Maus-Latenz — ohne den Schalter werden mehrere Bewegungs-Events pro Frame zu einem zusammengefasst, was bei hohen FPS spürbar ist.
> - Yaw geht in den **Player** (Body), Pitch in den **HeadAnchor** (Kopf). Diese Trennung ist Standard in modernen FPS-Engines und vermeidet Gimbal-Lock.
> - `Basis.Orthonormalized()` nach jeder Yaw-Drehung verhindert Floating-Point-Drift, der nach vielen Drehungen zu schiefer Welt führt.
> - `Pitch`-Clamp auf ±85° (statt ±90°) vermeidet Zenit-Artefakte.

- [ ] **Step 2: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Erwartet: `Build succeeded`. Die Szene öffnet, FPS-Kamera ist aber noch ohne sichtbaren Effekt (weil noch niemand `Activate()` ruft).

- [ ] **Step 3: Commit**

```bash
git add scripts/Views/FirstPersonCameraController.cs
git commit -m "Task 20.2: FirstPersonCameraController mit Body-Yaw und Head-Pitch"
```

### Task 20.3: HUD-Toggle für First-Person-Modus

**Files:**
- Modify: `scripts/Hud/Hud.cs`
- Modify: `scenes/Hud.tscn`

- [ ] **Step 1: Signal in `Hud.cs` ergänzen**

In der Signal-Sektion oben in `Hud.cs`:

```csharp
[Signal] public delegate void FirstPersonToggleEventHandler(bool active);
```

In den Knoten-Feldern:

```csharp
private CheckBox _firstPersonToggle = null!;
```

In `_Ready()` nach den anderen Toggle-Resolves:

```csharp
_firstPersonToggle = GetNode<CheckBox>("Root/Margin/VBox/Algos/FirstPersonToggle");
_firstPersonToggle.Toggled += OnFirstPersonToggled;
```

Am Klassenende eine Methode ergänzen:

```csharp
private void OnFirstPersonToggled(bool active) =>
    EmitSignal(SignalName.FirstPersonToggle, active);
```

- [ ] **Step 2: Toggle in `scenes/Hud.tscn` einfügen**

Öffne `scenes/Hud.tscn` und füge in der `Algos`-HBox-Container-Reihe (dort wo schon `View3DToggle`, `HeatmapToggle`, `FollowCamToggle`, `ExploreModeToggle` stehen) einen weiteren CheckBox-Knoten ein:

```text
[node name="FirstPersonToggle" type="CheckBox" parent="Root/Margin/VBox/Algos"]
text = "First-Person"
```

> **Hinweis:** Die exakte Position innerhalb des `Algos`-Containers ist Geschmacksfrage — am Ende anhängen ist am unkompliziertesten.

- [ ] **Step 3: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add scripts/Hud/Hud.cs scenes/Hud.tscn
git commit -m "Task 20.3: HUD-Checkbox 'First-Person' mit Signal"
```

### Task 20.4: `Main` schaltet die Kamera um, deaktiviert Free-/Follow-Kamera

**Files:**
- Modify: `scripts/Main.cs`

- [ ] **Step 1: Felder und Signal-Wiring**

In `Main.cs`, oberhalb von `_Ready()`:

```csharp
private FirstPersonCameraController _fpCamera = null!;
private CameraController3D _orbitCamera = null!;
private bool _firstPersonActive;
```

In `_Ready()`, nach `_view3D` aufgelöst ist:

```csharp
_orbitCamera = _view3D.GetNode<CameraController3D>("Camera3D");
_fpCamera = _view3D.GetNode<FirstPersonCameraController>("Player/HeadAnchor/FirstPersonCamera");
```

Im Signal-Wiring:

```csharp
_hud.FirstPersonToggle += OnFirstPersonToggled;
```

- [ ] **Step 2: `OnFirstPersonToggled`-Handler**

Am Klassenende:

```csharp
private void OnFirstPersonToggled(bool active)
{
    _firstPersonActive = active;

    if (active)
    {
        // FPS-Modus benoetigt eine Spielfigur — wenn die Figur nicht sichtbar
        // ist (kein Solver gelaufen, kein Manual-Mode), erzwingen wir den
        // Manual-Mode auf Start-Zelle, damit der User ueberhaupt etwas sieht.
        if (!_player.Visible)
        {
            if (_currentMaze == null)
            {
                GD.PrintErr("[Main] First-Person ohne Maze nicht moeglich.");
                _hud.SetFirstPersonPressed(false);
                _firstPersonActive = false;
                return;
            }
            // Player zumindest an Startzelle setzen, ohne Manual-Mode.
            _player.Position = new Vector3(
                _view3D.CellSize * 0.5f,
                _player.StandHeight,
                _view3D.CellSize * 0.5f);
            _player.Visible = true;
        }

        // Follow-Cam abschalten, falls sie aktiv war (FPS und Follow schliessen sich aus).
        _orbitCamera.DisableFollow();

        _fpCamera.Activate();
    }
    else
    {
        _fpCamera.Deactivate();
        _orbitCamera.MakeCurrent();
    }
}
```

> **Hinweis:** `SetFirstPersonPressed(false)` setzt den HUD-Toggle ohne erneutes Signal-Feuern zurück — siehe Step 3.

- [ ] **Step 3: Hilfsmethode in `Hud.cs` ergänzen**

In `Hud.cs` nach `ShowVictory`:

```csharp
public void SetFirstPersonPressed(bool pressed) =>
    _firstPersonToggle.SetPressedNoSignal(pressed);
```

- [ ] **Step 4: `OnViewToggled` defensiv erweitern**

Wenn der User von 3D zurück auf 2D schaltet, soll der FPS-Modus automatisch beendet werden. In `OnViewToggled`:

```csharp
private void OnViewToggled(bool use3D)
{
    _view2D.Visible = !use3D;
    _view3D.Visible = use3D;
    if (use3D && _currentMaze != null)
        _view3D.SetMaze(_currentMaze);

    // FPS-Modus beim Verlassen der 3D-Ansicht zwangsweise abschalten,
    // sonst bleibt der Cursor eingefangen.
    if (!use3D && _firstPersonActive)
    {
        _fpCamera.Deactivate();
        _orbitCamera.MakeCurrent();
        _firstPersonActive = false;
        _hud.SetFirstPersonPressed(false);
    }

    GD.Print($"[Main] 3D-Ansicht = {use3D}");
}
```

- [ ] **Step 5: Build prüfen**

```powershell
& $env:GODOT4 --path $PWD --build-solutions
dotnet build
```

Manueller Smoke-Test: Spiel starten → Maze generieren → 3D-View aktivieren → "Selbst spielen" → "First-Person" einschalten. Erwartet: Cursor wird captured, Maus bewegt Yaw (Body) und Pitch (Head). WASD bewegt cell-aligned in Blickrichtung.

- [ ] **Step 6: Commit**

```bash
git add scripts/Main.cs scripts/Hud/Hud.cs
git commit -m "Task 20.4: Kamera-Switch und HUD-Verdrahtung fuer FPS-Modus"
```

### Task 20.5: `PlayerCharacter3D.FaceDirection` im FPS-Modus überspringen

Im FPS-Modus dreht die Maus den Player-Body — die alte `FaceDirection`-Logik (die den Body in die Bewegungsrichtung dreht) würde dem entgegenlaufen. Wir überspringen sie, sobald der Player-Yaw "manuell gesetzt" ist.

**Files:**
- Modify: `scripts/Views/PlayerCharacter3D.cs`

- [ ] **Step 1: Flag hinzufügen**

In `PlayerCharacter3D` ein Flag und Setter:

```csharp
/// <summary>
/// Wenn true, wird FaceDirection() NICHT aufgerufen — die Body-Rotation kommt
/// dann von aussen (z.B. FirstPersonCameraController). Wird von Main beim
/// Aktivieren des FPS-Modus auf true gesetzt.
/// </summary>
public bool ExternalBodyYaw { get; set; }
```

- [ ] **Step 2: `FaceDirection`-Aufrufe schützen**

In `ProcessFollowPath` und `ProcessManual` jeweils das `FaceDirection(...)` mit `if (!ExternalBodyYaw)` umklammern. Beispiel `ProcessManual`:

```csharp
// Figur sofort in Bewegungsrichtung drehen (beim ersten Frame der Animation).
if (!ExternalBodyYaw)
    FaceDirection(_animTo - _animFrom);
```

- [ ] **Step 3: `Main.OnFirstPersonToggled` setzt das Flag**

In `Main.OnFirstPersonToggled`, im `if (active)` Branch nach `_fpCamera.Activate()`:

```csharp
_player.ExternalBodyYaw = true;
```

Im `else`-Branch nach `_orbitCamera.MakeCurrent()`:

```csharp
_player.ExternalBodyYaw = false;
```

- [ ] **Step 4: Build prüfen**

```powershell
dotnet build
```

- [ ] **Step 5: Manueller Smoke-Test**

Maze generieren → "Selbst spielen" → "First-Person" → mit der Maus drehen. Erwartet: Body-Yaw folgt der Maus (außen sichtbar wäre, dass die Lego-Figur sich dreht; im FPS-View sieht man den Effekt am Anker der Welt). WASD bewegt cell-aligned in Blickrichtung. Die Lego-Figur "verdreht" sich nicht mehr ruckartig zur Bewegungsrichtung.

- [ ] **Step 6: Commit**

```bash
git add scripts/Views/PlayerCharacter3D.cs scripts/Main.cs
git commit -m "Task 20.5: ExternalBodyYaw-Flag verhindert FaceDirection-Konflikt im FPS"
```

---

## Phase 21 — Free-Look: Schauen ohne Bewegungsrichtungswechsel

Ziel: Während die `Alt`-Taste gehalten wird, dreht die Maus nur die Kamera (genauer: den `HeadAnchor`-Yaw-Offset), nicht mehr den Body. So kann der Spieler nach links schauen, während er weiter geradeaus laufen würde. Beim Loslassen schwenkt die Kamera per Tween in 0,15 s zurück auf 0° relativ zum Body.

**Didaktischer Punkt:** Der Unterschied zwischen *Modus* (z.B. FPS-Modus, der bis zum Abschalten aktiv bleibt) und *Modifier* (Alt-Hold, das nur während des Drückens wirkt) ist grundlegend für UI-Design. Hier sieht man beides direkt nebeneinander.

### Task 21.1: `FreeLookHeld` aus `Alt`-Tastenstatus speisen

**Files:**
- Modify: `scripts/Views/FirstPersonCameraController.cs`

- [ ] **Step 1: `_Process` um Alt-Erkennung ergänzen**

In `FirstPersonCameraController._Process`, **vor** der Pfeiltasten-Logik:

```csharp
// Free-Look: solange Alt gehalten ist, geht Yaw nur in den HeadAnchor (nicht in den Body).
// Standard-Konvention in modernen FPS-Spielen (z.B. Cyberpunk 2077 Immersive Mod, Vintage Story).
bool wantFreeLook = Input.IsPhysicalKeyPressed(Key.Alt);
if (wantFreeLook != FreeLookHeld)
    SetFreeLookHeld(wantFreeLook);
```

Eine Hilfsmethode dazu:

```csharp
private float _freeLookYaw;          // aktueller Yaw-Offset auf dem HeadAnchor in rad
private float _freeLookSnapBackTime; // Restdauer fuer Snap-Back-Animation in s
private const float FreeLookSnapBackDuration = 0.15f;
private const float FreeLookMaxYawDeg = 100f;  // ±100° clamp; mehr fuehlt sich unnatuerlich an

private void SetFreeLookHeld(bool held)
{
    FreeLookHeld = held;
    if (!held)
    {
        // Beim Loslassen Snap-Back-Animation starten (laeuft in _Process unten).
        _freeLookSnapBackTime = FreeLookSnapBackDuration;
    }
}
```

- [ ] **Step 2: Snap-Back-Animation in `_Process` ausführen**

Ans Ende von `_Process` (nach Pfeiltasten-Block) ergänzen:

```csharp
// Snap-Back: nach dem Loslassen von Alt schwenkt der HeadAnchor weich auf 0 zurueck.
if (!FreeLookHeld && _freeLookSnapBackTime > 0f)
{
    float dt = (float)delta;
    _freeLookSnapBackTime = Mathf.Max(0f, _freeLookSnapBackTime - dt);

    // Fortschritt 0..1, dann ease-out (1 - (1-t)^2) fuer organischen Verlauf.
    float t = 1f - (_freeLookSnapBackTime / FreeLookSnapBackDuration);
    float eased = 1f - (1f - t) * (1f - t);

    // Linear vom aktuellen _freeLookYaw zu 0; weil wir im Tween-Frame den
    // Wert aktiv setzen, faellt der Pivot weich zurueck.
    float newYaw = Mathf.Lerp(_freeLookYaw, 0f, eased);
    ApplyFreeLookYawAbsolute(newYaw);

    if (_freeLookSnapBackTime <= 0f)
    {
        ApplyFreeLookYawAbsolute(0f);
        _freeLookYaw = 0f;
    }
}
```

> **Hinweis:** Die ease-out-Funktion `1 - (1-t)^2` ist quadratisch und liefert einen weichen Anhalter. Reines Lerp (linear) wirkt mechanisch.

- [ ] **Step 3: `ApplyLook` und Free-Look-Yaw-Tracking erweitern**

Ersetze die Yaw-Logik in `ApplyLook` durch:

```csharp
if (yawDelta != 0f)
{
    if (FreeLookHeld)
    {
        // Yaw geht in den HeadAnchor-Offset, NICHT in den Body.
        float maxRad = Mathf.DegToRad(FreeLookMaxYawDeg);
        _freeLookYaw = Mathf.Clamp(_freeLookYaw - yawDelta, -maxRad, maxRad);
        ApplyFreeLookYawAbsolute(_freeLookYaw);
    }
    else
    {
        // Klassisch: Yaw geht in den Body.
        _player.RotateY(-yawDelta);
        _player.Basis = _player.Basis.Orthonormalized();
    }
}
```

Und die Hilfsmethode:

```csharp
private void ApplyFreeLookYawAbsolute(float yawRad)
{
    // HeadAnchor.Rotation.Y wird absolut gesetzt; Pitch (X) bleibt unveraendert.
    _headAnchor.Rotation = new Vector3(_headAnchor.Rotation.X, yawRad, 0f);
}
```

- [ ] **Step 4: Build prüfen**

```powershell
dotnet build
```

- [ ] **Step 5: Manueller Smoke-Test**

Im FPS-Modus `Alt` halten → Maus bewegen → Kamera dreht, Body bleibt stehen. `Alt` loslassen → Kamera schwenkt weich zurück auf Body-Forward (sollte sich anfühlen wie ein leichter "Rückwärtsschwung"). Vorwärts laufen mit W während `Alt` gedrückt ist → Spieler bewegt sich weiter in der ursprünglichen Body-Richtung, nicht in Blickrichtung.

- [ ] **Step 6: Commit**

```bash
git add scripts/Views/FirstPersonCameraController.cs
git commit -m "Task 21.1: Free-Look (Alt-Hold) mit Snap-Back-Animation"
```

### Task 21.2: Edge-Case — Alt+W darf Bewegung nicht in Blickrichtung schicken

`PlayerCharacter3D.GetManualDirectionFromView()` quantisiert die Bewegungsrichtung anhand der **aktiven Kamera**-Forward-Achse. Im FPS-Modus mit Free-Look würde W also in die *Blick*-Richtung gehen — das ist das genaue Gegenteil dessen, was Free-Look bezwecken soll.

**Files:**
- Modify: `scripts/Views/PlayerCharacter3D.cs`

- [ ] **Step 1: `ExternalBodyYaw`-aware Movement-Resolution**

Wenn `ExternalBodyYaw == true`, soll die Bewegung nicht aus der Kamera-Forward kommen, sondern aus der **Body-Forward** (also `Player.GlobalTransform.Basis.Z * -1`).

Ersetze in `GetPlanarForward` und `GetPlanarRight` die Camera-basierte Logik durch eine konditionale:

```csharp
private Direction? GetManualDirectionFromView()
{
    Vector3 forward;
    Vector3 right;

    if (ExternalBodyYaw)
    {
        // FPS-Modus: Bewegung folgt Body-Yaw, NICHT der Kamera (sonst wuerde Free-Look
        // die Bewegungsrichtung mit aendern).
        forward = -GlobalTransform.Basis.Z;
        right = GlobalTransform.Basis.X;
        forward.Y = 0f;
        right.Y = 0f;
        if (forward.LengthSquared() < 0.0001f) forward = Vector3.Forward;
        if (right.LengthSquared() < 0.0001f) right = Vector3.Right;
        forward = forward.Normalized();
        right = right.Normalized();
    }
    else
    {
        Camera3D camera = GetViewport().GetCamera3D();
        forward = GetPlanarForward(camera);
        right = GetPlanarRight(camera);
    }

    Vector3 inputWorld;
    if (Input.IsPhysicalKeyPressed(Key.W)) inputWorld = forward;
    else if (Input.IsPhysicalKeyPressed(Key.S)) inputWorld = -forward;
    else if (Input.IsPhysicalKeyPressed(Key.A)) inputWorld = -right;
    else if (Input.IsPhysicalKeyPressed(Key.D)) inputWorld = right;
    else return null;

    return QuantizeWorldDirectionToMaze(inputWorld);
}
```

- [ ] **Step 2: Build prüfen**

```powershell
dotnet build
```

- [ ] **Step 3: Manueller Smoke-Test (kritischer Test der ganzen Phase)**

1. Maze generieren → 3D → Selbst spielen → First-Person.
2. Mit Maus drehen (Body-Yaw): W bewegt in Blickrichtung. ✓
3. `Alt` halten, mit Maus drehen: Kamera dreht, Body bleibt. ✓
4. Bei gehaltenem `Alt` W drücken: Spieler bewegt sich in **ursprünglicher** Body-Richtung (nicht in Blickrichtung). ✓
5. `Alt` loslassen: Kamera schwenkt weich zurück. ✓

- [ ] **Step 4: Commit**

```bash
git add scripts/Views/PlayerCharacter3D.cs
git commit -m "Task 21.2: Movement folgt Body-Yaw statt Kamera-Yaw im FPS"
```

### Task 21.3: HUD-Hinweis zur Free-Look-Taste (optional, didaktisch wertvoll)

Damit Schüler die Funktion entdecken, ein dezenter Hinweis-Label neben dem FPS-Toggle.

**Files:**
- Modify: `scenes/Hud.tscn`

- [ ] **Step 1: Label hinzufügen**

In der gleichen `Algos`-Container-Reihe direkt hinter dem `FirstPersonToggle`:

```text
[node name="FirstPersonHint" type="Label" parent="Root/Margin/VBox/Algos"]
text = "(Alt = umsehen)"
modulate = Color(0.7, 0.7, 0.7, 1)
```

- [ ] **Step 2: Sichtprüfung im Editor**

Die Position kann variieren — wichtig ist nur, dass das Label sichtbar in der Nähe des FPS-Toggles steht.

- [ ] **Step 3: Commit**

```bash
git add scenes/Hud.tscn
git commit -m "Task 21.3: Hinweis 'Alt = umsehen' im HUD"
```

---

## Abschluss-Smoke-Test (gesamter Plan)

Nach Phase 21 sollte folgende Bedien-Sequenz vollständig funktionieren:

1. Spiel starten.
2. Maze generieren (z.B. 25×25 Recursive Backtracker).
3. "3D" einschalten.
4. "Selbst spielen" einschalten — Verfolger-Kamera von oben.
5. "First-Person" einschalten — Cursor wird captured, Sicht aus Augenhöhe der Lego-Figur.
6. Maus links/rechts → Body dreht sich, Sicht folgt. WASD bewegt cell-aligned in Blickrichtung.
7. `Alt` halten + Maus links/rechts → nur Kamera dreht (max. ±100°). W bewegt weiter in alter Body-Richtung.
8. `Alt` loslassen → Kamera schwenkt weich (~0,15 s) zurück.
9. `Esc` → Cursor frei, HUD klickbar. Klick ins Spielbild → Cursor wieder captured.
10. "First-Person" abschalten → Verfolger-Kamera wieder aktiv. Cursor frei.
11. "Selbst spielen" abschalten → zurück in den Algorithmen-Modus.

Wenn alle Schritte ohne Crash, ohne Cursor-Klemmer und ohne sichtbare Sprünge laufen, ist der Plan erfolgreich abgeschlossen.

---

## Zukünftige Erweiterungen (nicht Teil dieses Plans)

- **Sprint / Schritt-Tempo per Shift:** Schnelleres Cell-Step-Tempo, ohne Wandkollisionen anzubrechen.
- **Continuous Movement (statt Cell-Step):** CharacterBody3D mit echter Wandkollision für ein noch "echteres" FPS-Feel. Würde aber das Maze-konzeptuelle Modell ("Bewegung in diskreten Schritten") aufweichen.
- **Crouch / Look-Down:** Auf eine Bodenmarkierung schauen, indem man sich duckt — könnte für eine spätere Inventar-/Item-Mechanik nützlich sein.
- **Headbob (cinematisch):** Sinus-Welle auf `HeadAnchor.Position.Y` während Bewegung. Bewusst weggelassen, weil schnell motion-sickness-induzierend.
- **Field-of-View-Slider:** HUD-Slider 60°–110° für Spieler mit unterschiedlichen Bildschirm-/Sehkomfort-Präferenzen.
