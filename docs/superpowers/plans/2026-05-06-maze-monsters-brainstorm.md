# Maze School Project — Monster/Gegner im 3D-Labyrinth (Brainstorming + Plan)

> Ziel dieses Dokuments: Eine neue spielerische Komponente (Gegner/Monster) definieren, die zur **zellenbasierten, eingeschraenkten Bewegung** des Spielers passt, und daraus einen umsetzbaren Phasenplan ableiten.

**Ausgangslage (Stand 2026-05-06):**
- Spielerbewegung ist cell-aligned (ein Schritt pro Eingabe, Wandkollision aktiv).
- First-Person ist verfuegbar; Blickrichtung beeinflusst Bewegungsrichtung.
- Der Spieler hat keine schnelle Ausweichbewegung wie in einem echten Shooter (kein freies Strafing, kein Sprint-Jump-Movement).

**Design-Leitplanke:**
Ein Gegner darf Spannung erzeugen, aber nicht auf reine Reflexe setzen. Erfolgsfaktor soll primär Planung, Orientierung und Timing sein.

---

## Brainstorming (Kandidaten)

### Kandidat A — Echo-Stalker (turn-basiertes Jagdmonster)

**Kurzidee:**
Nach jedem Spielerzug darf der Gegner ebenfalls genau einen Zellzug machen. Er nutzt ein einfaches Jagdverhalten in diskreten Schritten (nicht kontinuierlich), optional mit kurzer Verzogerung.

**Warum passend zur eingeschraenkten Bewegung:**
- Gleiches Bewegungsmodell fuer beide Seiten (fair).
- Keine unlesbaren High-Speed-Chases im First-Person-Tunnel.
- Spieler kann bewusst "Tempo machen" oder kurz orientieren, ohne instant bestraft zu werden.

**Stärken:**
- Sehr klar vermittelbar (didaktisch gut).
- Technisch gut in vorhandene Zelllogik integrierbar.
- Lässt sich leicht in Schwierigkeitsgrade staffeln (z. B. Gegner zieht nur alle 2 Spielerzuege).

**Risiken:**
- Ohne Zusatzregeln kann es schnell repetitiv wirken.
- Braucht gutes Feedback (Audio/HUD), sonst fuehlt sich Verfolgung unklar an.

---

### Kandidat B — Patrouillen-Waechter mit Sichtkegel

**Kurzidee:**
Mehrere Gegner patrouillieren feste Routen. Wird der Spieler im Sichtkegel erkannt, beginnt eine Verfolgung.

**Warum passend:**
- Setzt eher auf Positionierung als auf Reflexe.
- Eignet sich gut fuer Schleich-Spielgefuehl im Labyrinth.

**Stärken:**
- Taktische Tiefe (Timingfenster, Ecken nutzen).
- Gute Basis fuer spaetere Mehrgegner-Levels.

**Risiken:**
- Sichtkegel in Grid + First-Person korrekt und fair zu visualisieren ist aufwaendiger.
- Deutlich mehr Content-/Balancing-Arbeit (Patrouillenpunkte pro Maze).

---

### Kandidat C — Gebietskontrolle ("Slime breitet sich aus")

**Kurzidee:**
Kein klassisches Monster-Model, sondern ein Gefahrenfeld, das pro Zug in benachbarte Zellen waechst. Beruehrung kostet Zeit/Leben.

**Warum passend:**
- Nutzt Grid-Staerken maximal aus.
- Erzwingt Routenentscheidungen statt Reflexduelle.

**Stärken:**
- Sehr robust fuer große Mazes.
- Deterministisch, gut fuer Unterricht/Analyse.

**Risiken:**
- Weniger "charakterhaft" als ein Monster.
- Braucht gute visuelle Lesbarkeit in First-Person.

---

## Entscheidung

**Aktueller Beschluss (2026-05-06):** Kandidat B — Patrouillen-Waechter mit Sichtkegel.

**Begruendung (revidiert):**
- Turn-basiertes Jagen (Kandidat A) skaliert fuer sehr grosse Mazes schlechter im Spielfluss.
- Flaechen-Ausbreitung (Kandidat C) passt schlechter zu Mazes mit stark linearem/engen Loesungsweg.
- Patrouillen + Entdeckung + Verfolgung liefert mehr stealth-orientierte Spannung und passt besser zum First-Person-Gefuehl.

**Weiterfuehrende Dokumente:**
- Design-Spec: `docs/superpowers/specs/2026-05-06-maze-patrol-guards-design.md`
- Implementierungsplan: `docs/superpowers/plans/2026-05-06-maze-patrol-guards-plan.md`

---

## Gameplay-Spezifikation (MVP)

### Grundregeln

1. Aktiv nur im 3D-Manual-Play-Modus.
2. Gegner startet in einer definierten Spawnzelle (z. B. nah am Ziel oder in einer Ecke).
3. Zugreihenfolge: Spielerzug -> Gegnerzug.
4. Gegner bewegt sich pro Gegnerzug maximal 1 Zelle.
5. Kollision (gleiche Zelle wie Spieler) = Run verloren, Neustart anbieten.

### Faireitsregeln fuer begrenzte Spielerbewegung

1. Start-Gnadezeit: Gegner steht fuer die ersten N Spielerzuege still (z. B. 4).
2. Taktbremse: Gegner zieht nur alle K Spielerzuege (z. B. K=2 auf "Normal").
3. Kein Spawn neben Spielerstart (Mindestdistanz in Manhattan-Zellen, z. B. >= 6).
4. Optionales "Leash": Gegner jagt nicht ueber die komplette Karte im MVP, sondern in einem Radius, um Frust auf grossen Mazes zu reduzieren.

### Gegner-KI (MVP)

1. Wenn Spieler bekannt: kuerzesten Nachbarschritt in Richtung Spieler waehlen (lokales BFS auf Grid).
2. Wenn kein valider Jagdschritt: zufaelligen offenen Nachbarn nutzen.
3. Bei Gleichstand zwischen Nachbarn: random tie-break fuer weniger Vorhersagbarkeit.

---

## Implementierungsplan (Phase 22)

### Task 22.1 — Datenmodell fuer Monsterzustand

**Files (geplant):**
- Create: `scripts/Gameplay/MonsterState.cs`
- Optional Modify: `scripts/Main.cs`

- [ ] Monster-Statusklasse einfuehren (`CurrentCell`, `IsActive`, `TurnsUntilMove`, `GraceTurnsLeft`).
- [ ] Konfigurierbare Schwierigkeitsparameter als einfache Struct/Record-Config vorbereiten.
- [ ] Initiale Parameter in `Main` hinterlegen (ohne HUD-UI-Overhead im ersten Schritt).

**Ergebnis:**
Main kann den Monster-Lebenszyklus verwalten, ohne View/Rendering schon anzufassen.

### Task 22.2 — Bewegungs-Signal aus Spielerfigur bereitstellen

**Files (geplant):**
- Modify: `scripts/Views/PlayerCharacter3D.cs`

- [ ] Neues Signal einfuehren, z. B. `ManualCellEntered(Cell cell)`.
- [ ] Signal nur im Manual-Modus emittieren, wenn eine Zellanimation abgeschlossen wurde.
- [ ] Bestehendes Verhalten unveraendert lassen (keine Regression fuer Bot-/Solver-Modus).

**Ergebnis:**
Main bekommt einen sauberen Hook "Spieler hat Zellzug abgeschlossen" fuer den Gegner-Turn.

### Task 22.3 — Gegnerlogik-Service (Grid-BFS Schrittwahl)

**Files (geplant):**
- Create: `scripts/Gameplay/MonsterNavigator.cs`
- Optional Reuse: `scripts/Maze/Direction.cs`, `scripts/Maze/Maze.cs`

- [ ] Helfer bauen, der aus `monsterCell` und `playerCell` den naechsten offenen Zellschritt liefert.
- [ ] Nur 4-Nachbarn, nur durch offene Waende (`HasWall == false`).
- [ ] Fallback auf random offenen Nachbarn, wenn kein Jagdpfad gefunden.

**Ergebnis:**
Deterministische, testbare Schrittentscheidung unabhaengig von Godot-Node-Logik.

### Task 22.4 — 3D-Visualisierung des Gegners (minimal)

**Files (geplant):**
- Modify: `scenes/MazeView3D.tscn`
- Create: `scripts/Views/MonsterCharacter3D.cs`
- Optional Modify: `scripts/Views/MazeView3D.cs`

- [ ] Einfaches sichtbares Monster als Node3D + MeshInstance3D anlegen (Capsule/Cuboid reicht fuer MVP).
- [ ] API bereitstellen: `SetCell(Cell c, float cellSize)`, `AnimateStep(Cell from, Cell to)`.
- [ ] Sichtbarkeit an Manual-Play koppeln.

**Ergebnis:**
Gegner ist im 3D-Labyrinth sichtbar und bewegt sich cell-aligned wie der Spieler.

### Task 22.5 — Main-Orchestrierung (Turn-System)

**Files (geplant):**
- Modify: `scripts/Main.cs`
- Optional Modify: `scripts/Hud/Hud.cs`, `scenes/Hud.tscn`

- [ ] Beim Start von Manual-Play Monster spawnen und Zustandswerte resetten.
- [ ] Bei jedem `ManualCellEntered` Gegnerturn evaluieren (Grace, Taktbremse, Schrittwahl).
- [ ] Kollision pruefen und Niederlage-Flow ausloesen.
- [ ] Beim Verlassen von 3D/FPS/Manual alles sauber deaktivieren.

**Ergebnis:**
Vollstaendiger loop: Spielerzug -> Gegnerreaktion -> Verlustbedingung.

### Task 22.6 — HUD-Feedback und Audio-Hinweise (Fairness)

**Files (geplant):**
- Modify: `scripts/Hud/Hud.cs`
- Modify: `scenes/Hud.tscn`
- Optional: neues Audio-Node in `scenes/Main.tscn` oder `scenes/MazeView3D.tscn`

- [ ] Kurze Warnanzeige "Monster zieht!" oder Distanz-Indikator (z. B. nah/mittel/weit).
- [ ] Optional Footstep/Heartbeat-Layer je nach Distanz.
- [ ] Text-Feedback bei Niederlage inkl. Restart-Hinweis.

**Ergebnis:**
Die Gefahr wird lesbar, statt "ploetzlich unfair" zu wirken.

### Task 22.7 — Balancing & Testmatrix

**Files (geplant):**
- Create: `docs/superpowers/specs/2026-05-06-monster-balance-notes.md`

- [ ] Drei Presets definieren: Easy/Normal/Hard (GraceTurns, MoveEveryN, SpawnDistance).
- [ ] Testfaelle dokumentieren (klein 15x15, mittel 35x35, gross 75x75).
- [ ] Fail-Kriterien: "kein Entkommen moeglich" innerhalb weniger Zuege trotz sauberem Spiel.

**Ergebnis:**
Nachvollziehbares Balancing statt ad-hoc Tuning.

---

## Akzeptanzkriterien (MVP)

1. In 3D-Manual kann ein sichtbarer Gegner aktiviert werden.
2. Gegner bewegt sich diskret und nicht schneller als spezifiziert.
3. Spieler hat reproduzierbar fairen Start (Grace + Spawnabstand).
4. Kollision fuehrt zu klarer Niederlage-Rueckmeldung.
5. `dotnet build` bleibt gruen.

---

## Optionale Ausbaustufe (nach MVP)

1. Kandidat-B-Elemente: kurzer Sichtkegel + Suchmodus statt permanenter Volljagd.
2. Mehrere Gegnertypen mit unterschiedlichen Takten.
3. Sammelobjekte (z. B. "Lichtbatterie"), die temporär Gegner verlangsamen.
4. Replay-Debug-Overlay (Zughistorie), didaktisch fuer Algorithmik-Vergleich.

---

## Empfehlung fuer den naechsten Schritt

Der Inhalt unter "Phase 22" in diesem Dokument gilt als Vorentwurf und wird durch den neuen Kandidat-B-Fokus ersetzt. Weiterarbeit bitte nur noch auf Basis der neuen Spec und des neuen Plans.

*** Add File: c:\SourcesPrivate\Minu\maze-sascha\docs\superpowers\specs\2026-05-06-maze-patrol-guards-design.md
# Maze School Project — Patrol Guards Design (Kandidat B)

> **Status:** Spec, erstellt am 2026-05-06.
> **Vorlaeufer:** `docs/superpowers/plans/2026-05-05-maze-first-person.md` (First-Person aktiv), `docs/superpowers/plans/2026-04-30-maze-gamification.md` (Manual Play, Explore Mode).
> **Nächster Schritt:** Implementierung über `docs/superpowers/plans/2026-05-06-maze-patrol-guards-plan.md`.

## Goal

Eine neue Gegenspieler-Mechanik fuer den 3D-Manual-Modus umsetzen: **Patrouillen-Waechter mit Sichtkegel**, die den Spieler entdecken, verfolgen und bei Sichtverlust in einen Suchmodus wechseln.

Wichtige Randbedingungen:

1. Funktioniert auch bei grossen Mazes (bis in den hohen zweistelligen oder dreistelligen Bereich).
2. Fuehlt sich fair an trotz eingeschraenkter Spielerbewegung (cell-aligned, keine High-Speed-Ausweichmanöver).
3. Bleibt didaktisch nachvollziehbar (klare Zustandsmaschine statt Blackbox-KI).

## Nicht-Ziele (MVP)

1. Kein komplexes Gruppenverhalten (Flanking, Kommunikation, Rollen).
2. Keine prozeduralen Animationen auf Charakter-Rig-Niveau.
3. Kein vollstaendiges Stealth-Soundsystem mit akustischer Ausbreitung.

## Spieler-Erlebnis

1. Standardzustand: Guards laufen erkennbare Patrouillenrouten.
2. Bei Sichtkontakt wechselt Guard in Alert/Chase; Druck steigt sofort.
3. Bei Sichtverlust sucht der Guard kurz im letzten bekannten Bereich.
4. Danach kehrt er zur Patrouille zurueck.

Das erzeugt Spannung ohne dauerhafte Dauerjagd ueber die ganze Karte.

## Kernmechanik

### 1) KI-Zustandsmaschine (pro Guard)

Zustaende:

1. `Patrol`: Lauf entlang vordefinierter Wegpunkte (Zellenliste).
2. `Alert`: Kurzer Uebergangszustand nach Entdeckung (optional 0.2-0.5s Reaktionszeit).
3. `Chase`: Verfolgung entlang Grid-Pfad zum letzten bekannten Spielerort.
4. `Search`: Lokales Absuchen in Radius um letzten Sichtpunkt.
5. `Return`: Rueckweg auf naechsten Patrouillen-Wegpunkt.

Zustandswechsel:

1. `Patrol -> Alert`: Spieler in Sichtkegel und line-of-sight frei.
2. `Alert -> Chase`: Reaktionszeit abgelaufen.
3. `Chase -> Patrol`: Spieler gefangen.
4. `Chase -> Search`: Sicht verloren fuer T Sekunden.
5. `Search -> Return`: Suchbudget (Zeit oder Schritte) verbraucht.
6. `Return -> Patrol`: Patrouillenweg wieder erreicht.

### 2) Sichtmodell im Maze

Der Sichttest ist zweistufig:

1. **FOV-Test** auf Zellvektor zum Spieler (Kegel, z. B. 70° halbwinkel-abhaengig).
2. **Line-of-sight-Test** auf Grid: Sichtstrahl wird zellweise geprueft; Wand zwischen zwei Zellen blockiert Sicht.

Warum so:

1. Passt zu bestehendem Zell-/Wandmodell (`Cell.HasWall`).
2. Liefert faire, erklaerbare Regeln an Ecken/Kreuzungen.

### 3) Bewegung und Update-Takt

1. Guards bewegen sich ebenfalls cell-aligned, jedoch mit eigener Schrittfrequenz in Sekunden.
2. Updates passieren in einem festen Tick (z. B. 6-10 Hz), nicht framegekoppelt.
3. Path-Recompute wird begrenzt (Cooldown), um Kosten in grossen Mazes zu kontrollieren.

### 4) Skalierung fuer grosse Mazes

1. Guard-Anzahl nicht linear mit Flaeche skalieren, sondern ueber Zonenbudget.
2. Nur Guards in relevanter Distanz zum Spieler laufen in voller Update-Qualitaet.
3. Entfernte Guards koennen im "coarse mode" laufen (selteneres Update, vereinfachtes Patrol-Sampling).
4. Globale harte Obergrenze fuer aktive Guards (MVP: 4-8 je nach Preset).

### 5) Fairness-Mechaniken

1. Startschutz: Keine Entdeckung in den ersten X Sekunden nach Run-Start.
2. Mindest-Spawnabstand zu Startzelle.
3. Guard-FOV enger als 180°, damit Ausweichen hinter Ecken moeglich bleibt.
4. Kurze "lost sight" Gnade (nicht sofort omniscient nach Ecke).

### 6) Verlustbedingung

1. Spieler verliert bei Zellkollision mit Guard.
2. Optional spaeter: "Near miss" oder Hitpoints, aber nicht im MVP.

## Systemarchitektur

Neue Komponenten (MVP):

1. `scripts/Gameplay/GuardState.cs`
2. `scripts/Gameplay/GuardPerception.cs`
3. `scripts/Gameplay/GuardNavigator.cs`
4. `scripts/Views/GuardCharacter3D.cs`
5. `scripts/Gameplay/GuardDirector.cs`

Verantwortlichkeiten:

1. `GuardDirector`: Orchestrierung aller Guards, Tick, Skalierung, Spawns.
2. `GuardState`: KI-Zustand und Runtime-Daten pro Guard.
3. `GuardPerception`: Sichtkegel + LOS auf Grid.
4. `GuardNavigator`: Pfad-/Schrittwahl fuer Patrol/Chase/Return.
5. `GuardCharacter3D`: Visualisierung und Zell-Animationsbewegung.

Integration in bestehende Schichten:

1. `Main.cs`: Start/Stop bei Manual-Play, Niederlage-Flow.
2. `MazeView3D.tscn`: Guard-Knoten-Container.
3. `Hud.tscn` / `Hud.cs`: Toggle + leichtes Feedback.

## Datenmodell (MVP)

`GuardState` (Vorschlag):

1. `int GuardId`
2. `Cell CurrentCell`
3. `Cell LastKnownPlayerCell`
4. `GuardMode Mode`
5. `List<Cell> PatrolRoute`
6. `int PatrolIndex`
7. `float StateTimer`
8. `float RepathCooldown`
9. `float MoveCooldown`

`GuardMode`:

1. `Patrol`
2. `Alert`
3. `Chase`
4. `Search`
5. `Return`

## HUD/UX-Anforderungen

1. Checkbox `Guards aktiv` (nur in 3D/Manual sinnvoll).
2. Optionale Preset-Wahl `Guard-Schwierigkeit` (Easy/Normal/Hard).
3. Kurzes Textfeedback: `Entdeckt!`, `Suche laeuft`, `Entkommen`.
4. Keine dauernd laute UI; Hinweise kurz und eindeutig.

## Telemetrie (didaktisch + balancing)

Erfassen pro Run:

1. Anzahl Entdeckungen.
2. Mittlere Chase-Dauer.
3. Zeit bis erste Entdeckung.
4. Distanz bei Niederlage.

Nutzen:

1. Presets datenbasiert statt gefuehlt balancen.
2. Schueler koennen Verhalten von Zustandsautomaten analysieren.

## Performance-Budget (MVP-Orientierung)

1. Guard-Logik-Tick-Zeit soll im Mittel klein bleiben (Richtwert < 2 ms bei typischen Szenen).
2. Keine pathfinding-Neuberechnung jedes Frame.
3. LOS-Pruefung capped pro Tick (Budget-Ansatz).

## Akzeptanzkriterien

1. Mindestens ein Guard patrouilliert sichtbar und wechselt reproduzierbar zwischen Patrol/Chase/Search/Return.
2. Sichtkontakt ist fuer den Spieler erklaerbar (keine "durch Wand gesehen"-Effekte).
3. In grossen Mazes bleibt Gameplay stabil (kein massiver Framerate-Einbruch durch Guard-Update).
4. Niederlage bei Kollision funktioniert deterministisch.
5. `dotnet build` bleibt gruen.

## Risiken und Gegenmassnahmen

1. Risiko: LOS-Logik an Ecken schwer korrekt.
	Gegenmassnahme: Erst Grid-LOS klar spezifizieren und mit Testfaellen absichern.
2. Risiko: Zu viele Guards machen Maze unfair.
	Gegenmassnahme: Harte Guard-Caps + Distanzbasierte Aktivierung.
3. Risiko: Chase fuehlt sich omniscient an.
	Gegenmassnahme: LastKnownCell + Search-Timer statt permanenter Spieler-Positionskenntnis.

## Open Questions

1. Soll ein Guard den Spieler nur von vorne sehen (FOV) oder auch bei kurzer Distanz 360°-Trigger haben?
2. Sollen Guards Tueren/Kreuzungen bevorzugen, um besser lesbare Patrouillen zu erzeugen?
3. Wann wird Mehr-Guard-Support freigeschaltet: direkt im MVP oder erst Phase 2?

*** Add File: c:\SourcesPrivate\Minu\maze-sascha\docs\superpowers\plans\2026-05-06-maze-patrol-guards-plan.md
# Maze School Project — Patrol Guards (Kandidat B) — Implementierungsplan

> **Fuer agentische Worker:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development` oder `superpowers:executing-plans` fuer task-by-task Umsetzung.
>
> **Fuer Schueler:** Jeder Task muss einzeln baubar bleiben (`dotnet build` gruen).
>
> **Spec:** `docs/superpowers/specs/2026-05-06-maze-patrol-guards-design.md`

**Goal:** Patrouillen-Waechter mit Sichtkegel in den 3D-Manual-Modus integrieren, inklusive Zustandsmaschine, fairer Verfolgung und skalierbarer Performance fuer grosse Mazes.

**Architektur:**

1. Gameplay-Kern als eigene C#-Klassen (`GuardDirector`, `GuardState`, `GuardPerception`, `GuardNavigator`).
2. Rendering separat in `GuardCharacter3D`.
3. `Main` orchestriert Mode-Lifecycle und Verlustbedingung.

---

## Phase 23 — Vertikaler Slice (1 Guard, Patrol -> Chase -> Lose)

Ziel: Schnell ein komplett spielbares Grundsystem liefern, noch ohne Search/Return-Feinschliff.

### Task 23.1: Guard-Basisklasse und Modus-Enum

**Files:**
- Create: `scripts/Gameplay/GuardState.cs`

- [ ] `GuardMode`-Enum mit `Patrol`, `Alert`, `Chase`.
- [ ] `GuardState` mit minimalen Laufzeitfeldern (`CurrentCell`, `Mode`, `PatrolRoute`, `PatrolIndex`).
- [ ] Daten bewusst Godot-unabhaengig halten (reine Logikklasse).

### Task 23.2: Minimal-Visualisierung in 3D

**Files:**
- Modify: `scenes/MazeView3D.tscn`
- Create: `scripts/Views/GuardCharacter3D.cs`

- [ ] `Guards`-Container als Kind von `MazeView3D` einfuegen.
- [ ] Einfache Mesh-Visualisierung fuer MVP (z. B. Capsule/Cuboid).
- [ ] API: `PlaceAtCell(Cell cell, float cellSize)` und `AnimateToCell(Cell target, float duration)`.

### Task 23.3: GuardDirector (ein Guard, Patrol und Chase)

**Files:**
- Create: `scripts/Gameplay/GuardDirector.cs`

- [ ] Tick-Loop (fixed rate) mit Guard-Update erstellen.
- [ ] Patrol: zyklisch entlang gegebener Route laufen.
- [ ] Entdeckung: FOV + LOS simplifiziert, bei Treffer zu `Chase`.
- [ ] Chase: naechsten Zellschritt Richtung Spieler berechnen.

### Task 23.4: Integration in Main (Manual-Play Lifecycle)

**Files:**
- Modify: `scripts/Main.cs`

- [ ] Bei Start Manual-Play GuardDirector initialisieren.
- [ ] Spielerzelle laufend an Director melden.
- [ ] Kollisionspruefung Guard/Spielerzelle -> Niederlage-Flow.
- [ ] Bei Stop/Reset sauber deaktivieren.

### Task 23.5: Build- und Smoke-Test

- [ ] `dotnet build` erfolgreich.
- [ ] Manual-Run: Guard patrouilliert, entdeckt, verfolgt, kann den Spieler fangen.

---

## Phase 24 — Voller Zustandsautomat (Search + Return) und Fairness

Ziel: KI weniger "allwissend" machen und Frust reduzieren.

### Task 24.1: Zustandsausbau in GuardState

**Files:**
- Modify: `scripts/Gameplay/GuardState.cs`

- [ ] `Search` und `Return` ergänzen.
- [ ] `LastKnownPlayerCell`, `StateTimer`, `RepathCooldown` einfuehren.

### Task 24.2: Perception-Modul entkoppeln

**Files:**
- Create: `scripts/Gameplay/GuardPerception.cs`

- [ ] FOV-Pruefung als pure Funktion.
- [ ] Grid-LOS mit Wandblocking (`HasWall`) implementieren.
- [ ] Kleine Testfaelle als kommentierte Beispiele im Code dokumentieren.

### Task 24.3: Navigator-Modul fuer Pfadschritte

**Files:**
- Create: `scripts/Gameplay/GuardNavigator.cs`

- [ ] Naechsten Schritt zum Ziel via Grid-Pfad liefern.
- [ ] Fallback bei Blockade/kein Pfad.
- [ ] Cooldown fuer Repath einhalten.

### Task 24.4: Fairness-Parameter in Director

**Files:**
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] Startschutzzeit.
- [ ] Mindestspawnabstand.
- [ ] Sichtverlust-Toleranz bevor `Search` endet.

### Task 24.5: HUD-Basisfeedback

**Files:**
- Modify: `scenes/Hud.tscn`
- Modify: `scripts/Hud/Hud.cs`

- [ ] Toggle `Guards aktiv` hinzufügen.
- [ ] Kurze Statusmeldungen (`Entdeckt`, `Suche`, `Entkommen`).

---

## Phase 25 — Skalierung fuer grosse Mazes + Multi-Guard

Ziel: Kandidat B robust fuer groessere Labyrinthe machen.

### Task 25.1: Guard-Budget und Spawnstrategie

**Files:**
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] Guard-Anzahl ueber Budget statt reine Flaechenformel steuern.
- [ ] Spawnpunkte pro Zone (nicht alle nahe Start/Goal).

### Task 25.2: Distanzbasierte Update-Qualitaet

**Files:**
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] Nahe Guards: voller Tick.
- [ ] Ferne Guards: reduzierter Tick/coarse mode.
- [ ] Harter Maximalwert fuer LOS-Checks pro Tick.

### Task 25.3: Mehrere GuardCharacter3D Instanzen

**Files:**
- Modify: `scripts/Views/GuardCharacter3D.cs`
- Modify: `scenes/MazeView3D.tscn`

- [ ] Instanzverwaltung fuer 2..N Guards.
- [ ] Kollision/State-Visualisierung pro Guard getrennt.

### Task 25.4: Schwierigkeitspresets

**Files:**
- Create: `scripts/Gameplay/GuardDifficulty.cs`
- Modify: `scripts/Hud/Hud.cs`
- Modify: `scenes/Hud.tscn`

- [ ] Easy/Normal/Hard als Parameterpakete.
- [ ] Mapping auf FOV, Suchdauer, Move-Interval, Guard-Cap.

### Task 25.5: Balancing-Notizen

**Files:**
- Create: `docs/superpowers/specs/2026-05-06-maze-patrol-guards-balance.md`

- [ ] Testmatrix fuer 15x15, 35x35, 75x75, 125x125.
- [ ] Erfolgsquote, Frustpunkte, durchschnittliche Chase-Dauer dokumentieren.

---

## Akzeptanzkriterien

1. Guards patrouillieren sichtbar und reagieren nachvollziehbar auf Sichtkontakt.
2. Guards verlieren den Spieler wieder plausibel und suchen lokal.
3. Kollision fuehrt deterministisch zu Niederlage.
4. Performance bleibt in grossen Mazes spielbar.
5. `dotnet build` bleibt gruen.

## Technische Leitplanken

1. Keine Vermischung von Render-Code und KI-Logik.
2. Keine framegebundene Chase-Logik ohne Tick-Limit.
3. Keine "Wallhack"-Sicht im finalen MVP.

## Empfehlung fuer die erste Umsetzung

1. Zuerst komplette Phase 23 liefern (spielbarer Slice).
2. Danach Phase 24 fuer Fairness/Lesbarkeit.
3. Phase 25 erst starten, wenn Slice stabil und nachvollziehbar ist.

