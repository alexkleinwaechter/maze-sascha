# Maze Patrol Guards - Offene Punkte fuer Folgesession

> **Erstellt:** 2026-05-06 nach Implementierung Phase 23/24/25 + Bugfix-Pass.
> **Spec:** `docs/superpowers/specs/2026-05-06-maze-patrol-guards-design.md`
> **Plan:** `docs/superpowers/plans/2026-05-06-maze-patrol-guards-plan.md`
> **Balance-Notizen:** `docs/superpowers/specs/2026-05-06-maze-patrol-guards-balance.md`
>
> Alle drei High-Severity-Bugs aus dem Code-Review wurden bereits gefixt (RepathCooldown, SearchBudgetSteps, Reset/Generate-Cleanup). Build ist gruen.
>
> **Update 2026-05-06:** Option A (Off-Path-Bonus im Patrouillenrouten-Generator) ist eingebaut.
> Routen verlassen den Solver-Pfad jetzt regelmaessig - garantiert Zeitfenster, in denen
> Pfad-Zellen unbeobachtet sind. Nicht mathematisch garantiert (kein Coverage-Check), aber
> probabilistisch deutlich entschaerft.

---

## 1. Smoke-Test im Godot-Editor (zwingend zuerst)

Nicht aus der Headless-Umgebung machbar - muss manuell laufen.

**Test-Ablauf:**

1. Maze erstellen 25x25 (Recursive Backtracker).
2. 3D-Ansicht an, "Selbst spielen" an.
3. "Waechter aktiv" anschalten, Difficulty = Easy.
4. Pruefen:
   - [ ] Genau 1 Guard sichtbar, mit gruenem Spotlight am Boden.
   - [ ] Guard laeuft erkennbare Patrouillenroute.
   - [ ] Bei Sichtkontakt: Guard wechselt zu gelb (Alert), dann rot (Chase).
   - [ ] HUD-Status zeigt "Entdeckt!".
   - [ ] Wenn man entkommt: nach 0.5s Sichtverlust wechselt Guard auf orange (Search).
   - [ ] Search endet nach 5s oder 8 Schritten -> blau (Return) -> wieder gruen (Patrol).
   - [ ] Bei Cell-Kollision: rote Niederlage-Anzeige "Verloren - erwischt".
   - [ ] Mindestspawnabstand: kein Guard direkt neben Startzelle.
   - [ ] Schleichmodus (Shift): Spieler langsamer, Sneak-Icon im HUD, Guard erkennt erst bei halber Distanz.
5. Difficulty Hard: 4 Guards, FPS spielbar?
6. Reset-/Generate-Tests: kein Guard-Phantom auf neuem Maze.

**Erwartung:** Durchspielbar, nichts crasht. Wenn Sichtkegel-Visualisierung nicht ueberzeugt -> Spotlight-Parameter (Tilt-Winkel, Energy, Range, Angle-Attenuation) feintunen.

---

## 2. Audio-Assets befuellen

**Status:** Knoten `DetectAudio` und `EscapeAudio` existieren in `scenes/MazeView3D.tscn`. Streams sind null. `GuardDirector.PlayAudio` ueberspringt null-Streams sauber.

**ToDo:**

- [ ] Zwei OGG-Files anlegen unter `assets/audio/guards/`:
  - `detect_sting.ogg` (~0.4s, abrupter Sting bei `Patrol -> Alert`)
  - `escape_cue.ogg` (~0.6s, weicher Cue bei `Chase -> Patrol/Search-Ende`)
- [ ] In `scenes/MazeView3D.tscn` als `AudioStream` an die jeweiligen Player binden.
- [ ] Volume und Bus pruefen (default Master OK fuer MVP).
- [ ] Optional: Pitch-Variation (+/- 5%) fuer weniger Repetition bei wiederholten Detections.

**Quelle:** Eigene Aufnahme oder CC0-Sounds (z. B. freesound.org).

---

## 3. Phase 24.3: Patrouillen-Bodenmarker

**Status:** Noch nicht implementiert. Spotlight + Mode-Farbcode liefern bereits hohe Lesbarkeit, aber Patrouillenrouten als planbare Routine sind im First-Person nicht erkennbar.

**Vorschlag:**

- [ ] `MazeView3D` um Decal/MultiMesh fuer Patrouillen-Pads erweitern (analog `_visitedPads` fuer Explore-Mode).
- [ ] Director ruft beim Spawn `MazeView3D.MarkPatrolCells(routeCells)` auf.
- [ ] Visual: dezente Bodenmarkierung (helles Decal, ~30% Opacity).
- [ ] HUD-Toggle "Patrouillen anzeigen" als CheckBox in der GuardsRow.
- [ ] Optional: Pads erst sichtbar, wenn Guard mindestens einmal vorbeigekommen ist (statt sofort).

**Aufwand:** ~1-2 Stunden.

---

## 4. Phase 25.3: Distanzbasierte Update-Qualitaet

**Status:** Noch nicht implementiert. Bei Hard (4 Guards) auf 125x125 wird das wahrscheinlich relevant.

**ToDo:**

- [ ] Im `GuardDirector._Process` pro Guard: Distanz zum Spieler messen.
- [ ] Nahe Guards (<= 20 Zellen): voller Tick (8 Hz), Spotlight aktiv.
- [ ] Ferne Guards: 2 Hz, Spotlight off.
- [ ] Hard-Cap fuer LOS-Checks pro Tick (z. B. max 4 Checks/Tick - bisher gibt es 1 LOS-Test pro Guard pro Tick, also bei 4 Guards bereits am Limit).

**Anker:** `GuardDirector.Tick(float dt)` in der Schleife `for (int i = 0; i < _guards.Count; i++)`.

**Aufwand:** ~1 Stunde.

---

## 5. Tests fuer GuardPerception und GuardNavigator

**Status:** Keine Tests im Repo. Spec hatte "kommentierte Testfaelle im Code" gefordert; aktuell nur Doc-Kommentare in `GuardPerception.cs`.

**Vorschlag:**

- [ ] Test-Projekt aufsetzen (`maze-sascha.Tests.csproj`) oder GdUnit4 als Godot-Plugin.
- [ ] Mindestens drei Tests pro Modul:
  - `GuardPerception.IsInFov`: Guard schaut Ost, Spieler oestlich -> true; westlich -> false.
  - `GuardPerception.HasLineOfSight`: Wand zwischen Zellen blockiert; offener Korridor frei.
  - `GuardNavigator.NextStepTowards`: Erster Schritt auf BFS-Pfad korrekt; nicht-erreichbares Ziel -> null.
  - `GuardPatrolRouteBuilder.Build`: Route-Length im erwarteten Bereich; alle Cell-Paare durch offene Wand verbunden.

**Begruendung:** Diese drei Module sind pure Funktionen ohne Godot-Abhaengigkeit, eignen sich also gut fuer reine xUnit-/NUnit-Tests.

**Aufwand:** ~2-3 Stunden inkl. Setup.

---

## 6. Balance-Testmatrix mit echten Daten fuellen

**Status:** Tabelle in `2026-05-06-maze-patrol-guards-balance.md` ist leer.

**ToDo:**

- [ ] 3-5 Runs pro Zelle der 4x3-Matrix (Maze-Groesse x Difficulty) spielen.
- [ ] `GuardTelemetry.Summarize()` aus Konsolenlog kopieren.
- [ ] Subjektives Frustlevel (fair/knapp/unfair) notieren.
- [ ] Stellschrauben tunen (siehe Frust-Kandidaten im Balance-Doc).

**Erwartung:** Default-Speeds und Default-Reichweite werden vermutlich bei einem oder zwei Difficulty-Settings nachgezogen. Wahrscheinliche Kandidaten: Chase-Speed-Faktor 1.15x evtl. zu hoch in engen Mazes.

---

## 7. Goal-naehe-Cap fuer Spawns

**Status:** Aktuell wird bei Spawn nur Mindestabstand zur Spielerstart-Zelle geprueft. Bei `Spawn auf BFS-Pfad` (Phase 25 implementiert) kann ein Guard sehr nah am Goal landen.

**Vorschlag:**

- [ ] Zusaetzlich Mindestabstand zum Goal pruefen (z. B. >= 4 Zellen).
- [ ] Anker: `GuardDirector.SpawnCandidates(...)`, Filterzeile bei der Pool-Erzeugung.

**Aufwand:** ~10 Minuten. Aber: erst nach Smoke-Test, sonst Pre-Optimization.

---

## 8. Telemetrie-Persistenz (optional)

**Status:** Telemetrie wird nur in die Konsole geloggt.

**Vorschlag:**

- [ ] In `GuardTelemetry.OnRunEnd()` JSON-Snapshot serialisieren nach `user://guard_runs/<timestamp>.json`.
- [ ] Per HUD-Toggle "Telemetrie speichern" steuerbar (default off).
- [ ] Datei-Format: `{ runStart, mazeSize, difficulty, ...metrics }`.

**Begruendung:** Erlaubt Schuelern eine spaetere Analyse mehrerer Runs. War in Spec als "optional spaeter" markiert.

**Aufwand:** ~30 Minuten.

---

## 9. Spotlight-Feintuning nach Smoke-Test

**Status:** Aktuell -15 Grad Tilt, Y=0.65, Energy 1.4, Range = DetectionRange (8 Zellen).

**Erwartete Tunings:**

- [ ] Eventuell Tilt staerker (-25 Grad), wenn der Boden-Kegel unsichtbar wirkt.
- [ ] Energy hoeher, wenn Explore-Mode-Fog den Spotlight verschluckt.
- [ ] SpotAngle ggf. niedriger (60 Grad), damit der visuelle Kegel klarer "vorne" zeigt - aber **dann muss `GuardPerception.HalfAngleDeg` synchron angepasst werden**, sonst sieht Guard mehr/weniger als der Lichtkegel suggeriert. **Visual und Logik muessen identisch bleiben - das ist Fairness-kritisch.**

**Anker:** `GuardCharacter3D.BuildVisuals()`.

---

## 10. (Optional) Telemetry.OnSneakTick verkabeln

**Status:** `GuardTelemetry.OnSneakTick` existiert, wird aber nicht aufgerufen. Sneak-Anteil bleibt 0.

**Fix:**

- [ ] In `Main._Process` (im `if (_isManualMode)`-Block): `if (sneaking) _guardDirector.Telemetry?.OnSneakTick((float)delta);`
- [ ] Alternativ: `GuardDirector.Tick` checkt selbst `_getPlayerSneaking()` pro Frame.

**Aufwand:** ~5 Minuten.

---

## Empfohlene Reihenfolge fuer Folgesession

1. **Smoke-Test** (Punkt 1) — ohne den ist alles weitere Spekulation.
2. **Spotlight-Feintuning** (Punkt 9) — direkt nach Smoke-Test, weil dann die visuelle Bewertung frisch ist.
3. **Audio-Assets** (Punkt 2) — niedrig haengende Frucht, Lesbarkeit-Boost.
4. **Goal-naehe-Cap** (Punkt 7) und **Telemetry.OnSneakTick** (Punkt 10) — Mini-Fixes nach Smoke-Test-Erfahrungen.
5. **Balance-Matrix** (Punkt 6) — mit befuellten Audio-Cues spielt es sich anders.
6. **Patrouillen-Bodenmarker** (Punkt 3) — wenn Lesbarkeit nach Spotlight-Tuning immer noch zu wenig ist.
7. **Distanzbasierte Update-Quality** (Punkt 4) — wenn 125x125 Hard tatsaechlich problematisch.
8. **Tests** (Punkt 5) — am Ende; lohnt sich, weil Logik dann stabil ist.
9. **Telemetrie-Persistenz** (Punkt 8) — Nice-to-have fuer Schueler-Analyse.

---

## Bekannte Limitierungen (kein TODO, dokumentarisch)

- **Im First-Person ohne Mini-Map:** Spieler kann Guards nicht weit voraus sehen. Das ist gewollt (Stealth-Spannung), aber bei sehr grossen Mazes evtl. zu hart. Mini-Map koennte spaetere Phase werden.
- **Keine Guard-Kollision untereinander:** Zwei Guards koennen auf derselben Zelle landen. Im aktuellen Setup unwahrscheinlich (Inter-Spawn-Distanz 5 + unterschiedliche Routen), aber theoretisch moeglich.
- **Patrouillenrouten sind statisch nach Spawn:** Aenderungen am Maze waehrend des Runs (gibt es nicht) wuerden Routen invalide machen.
