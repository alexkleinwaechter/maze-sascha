# Maze School Project - Patrol Guards Design (Kandidat B)

> **Status:** Spec, erstellt am 2026-05-06, Revision 2 nach Gameplay-Review.
> **Vorlaeufer:** `docs/superpowers/plans/2026-05-05-maze-first-person.md` (First-Person aktiv), `docs/superpowers/plans/2026-04-30-maze-gamification.md` (Manual Play, Explore Mode).
> **Naechster Schritt:** Implementierung ueber `docs/superpowers/plans/2026-05-06-maze-patrol-guards-plan.md`.

## Goal

Eine neue Gegenspieler-Mechanik fuer den 3D-Manual-Modus umsetzen: **Patrouillen-Waechter mit sichtbarem Sichtkegel**, die den Spieler entdecken, verfolgen und bei Sichtverlust in einen Suchmodus wechseln.

Wichtige Randbedingungen:

1. Funktioniert auch bei grossen Mazes (bis in den hohen zweistelligen oder dreistelligen Bereich).
2. Fuehlt sich fair an trotz eingeschraenkter Spielerbewegung (cell-aligned, keine High-Speed-Ausweichmanoever).
3. Bleibt didaktisch nachvollziehbar (klare Zustandsmaschine statt Blackbox-KI).
4. Stealth ist **lesbar**: Sichtkegel sichtbar, Guard-Zustand farblich kodiert, Detection-Audio.

## Nicht-Ziele (MVP)

1. Kein komplexes Gruppenverhalten (Flanking, Kommunikation, Rollen).
2. Keine prozeduralen Animationen auf Charakter-Rig-Niveau.
3. Kein vollstaendiges Stealth-Soundsystem mit akustischer Ausbreitung (nur einzelne Cue-Sounds, siehe Audio).
4. Keine handgepflegten Patrouillenrouten pro Maze (Mazes sind prozedural - Routen werden auto-generiert).

## Vorbedingungen (vor Phase 23 zu klaeren)

Diese Punkte muessen vor Implementierungsstart fixiert sein, sonst ist der Slice nicht spielbar:

1. **Niederlage-Flow:** Restart-/GameOver-Pfad in `Main.cs` muss vorhanden sein. Falls heute nicht implementiert, ist das Teil von Task 23.0 (nicht erst spaeter).
2. **Patrouillenrouten-Quelle:** Routen werden **auto-generiert** durch `GuardPatrolRouteBuilder` (siehe Patrouillenrouten-Generator).
3. **Geschwindigkeitsverhaeltnis Guard zu Spieler:** klar definiert pro Mode (siehe Bewegung).
4. **Spieler-Position waehrend Cell-Animation:** als Quellzelle definiert (siehe Sichtmodell).

## Spieler-Erlebnis

1. Standardzustand: Guards laufen erkennbare Patrouillenrouten, Spotlight am Boden zeigt Sichtkegel.
2. Sichtkegel und Guard-Mesh sind **mode-farblich kodiert** (gruen/gelb/rot/orange/blau).
3. Bei Sichtkontakt: Detection-Audio-Cue, kurzer Alert, dann Chase. Der Spieler weiss sofort, dass er entdeckt wurde.
4. Spieler kann durch **Schleichen** (Modifier-Taste) den Erkennungsradius des Guards halbieren - Tradeoff: 0.6x Bewegungsgeschwindigkeit.
5. Bei Sichtverlust sucht der Guard kurz im letzten bekannten Bereich (ab Phase 24; in Phase 23 vereinfacht via ChaseLossTimer).
6. Danach kehrt er zur Patrouille zurueck.

Ergebnis: Spannung ohne Dauerjagd, **aktive** Stealth-Optionen statt nur Rennen, klare visuelle und akustische Lesbarkeit.

## Kernmechanik

### 1) KI-Zustandsmaschine (pro Guard)

Zustaende:

1. `Patrol`: Lauf entlang auto-generierter Wegpunkte (Zellenliste).
2. `Alert`: Kurzer Uebergangszustand nach Entdeckung (0.3s Reaktionszeit).
3. `Chase`: Verfolgung entlang Grid-Pfad zum letzten bekannten Spielerort.
4. `Search`: Lokales Absuchen in Radius um letzten Sichtpunkt (Phase 24).
5. `Return`: Rueckweg auf naechsten Patrouillen-Wegpunkt (Phase 24).

Zustandswechsel:

1. `Patrol -> Alert`: Spieler in Sichtkegel und LOS frei.
2. `Alert -> Chase`: Reaktionszeit (0.3s) abgelaufen.
3. `Chase -> Patrol` (Phase 23, vereinfacht): ChaseLossTimer (3s ohne Sicht) abgelaufen ODER Spieler gefangen.
4. `Chase -> Search` (Phase 24): Sicht verloren laenger als 0.5s.
5. `Search -> Return`: Suchbudget (5s ODER 8 Schritte) verbraucht.
6. `Return -> Patrol`: Patrouillenweg wieder erreicht.

**Wichtig:** Phase 23 vereinfacht `Chase -> Patrol` per ChaseLossTimer, damit der Slice einen vollen Loop hat. Phase 24 ersetzt den Timer durch die saubere Search/Return-Pipeline.

### 2) Sichtmodell im Maze

Der Sichttest ist zweistufig:

1. **FOV-Test:** Guards blicken cell-aligned in eine Cardinal-Direction (N/S/O/W). Der FOV ist als +/- 70 Grad um diese Direction definiert. (Im 3D wird der Yaw waehrend Animation interpoliert; logisch zaehlt aber die diskrete Cardinal-Direction.)
2. **Line-of-sight-Test** auf Grid: Sichtstrahl wird zellweise geprueft; Wand zwischen zwei Zellen blockiert Sicht (`Cell.HasWall`).

**Spieler-Position fuer Sichttest:**

- Waehrend einer Cell-Animation gilt die **Quellzelle** (der Spieler ist erst "in" einer Zelle, wenn er dort angekommen ist).
- Diese Regel gilt symmetrisch fuer den Guard.
- Vermeidet "wieso hat er mich erwischt waehrend ich lief"-Frust.

**Reichweite:**

- Standard-Erkennungsreichweite: 8 Zellen.
- Schleichmodus halbiert die Reichweite des Guards auf 4 Zellen.

### 3) Bewegung und Update-Takt

1. Guards bewegen sich cell-aligned mit eigener Schrittfrequenz.
2. **Geschwindigkeitsverhaeltnis Guard zu Spieler:**
   - Patrol: 0.7x Spieler (gemaechlich, gut beobachtbar)
   - Chase: 1.15x Spieler (zwingt zur Reaktion, aber Entkommen ueber Routenwahl moeglich)
   - Search: 0.8x Spieler
   - Return: 1.0x Spieler
3. **Spieler-Schleichmodus:** Spieler 0.6x Normalgeschwindigkeit, Guard-Reichweite halbiert.
4. Updates passieren in einem festen Tick von 8 Hz, nicht framegekoppelt.
5. Path-Recompute wird ueber `RepathCooldown` (z. B. 0.4s) begrenzt.

### 4) Patrouillenrouten-Generator

Da Mazes prozedural sind, werden Routen **automatisch** zur Spawn-Zeit erzeugt:

1. **Heuristik:** Aus jeder Spawnzelle eine zyklische Route von 6-12 Zellen erzeugen.
2. **Bevorzugung von Engstellen:** Korridorzellen (Grad <= 2 offene Nachbarn) und Kreuzungen (Grad >= 3) werden bevorzugt eingebaut. Damit liegen Patrouillen auf spielmechanisch relevanten Wegen.
3. **Lage am Loesungsweg (Phase 25):** Spawn bevorzugt auf oder nahe dem BFS-Pfad Start -> Goal, damit der Spieler in grossen Mazes ueberhaupt Guards begegnet.
4. **Form:** Loop wo moeglich (Hin- und Rueckweg unterschiedlich), sonst Bounce (vor und zurueck).
5. **Validierung:** Route muss vollstaendig durch offene Waende verbunden sein.

### 5) Skalierung fuer grosse Mazes

1. Guard-Anzahl nicht linear mit Flaeche skalieren, sondern ueber Zonenbudget.
2. Nur Guards in relevanter Distanz zum Spieler laufen in voller Update-Qualitaet (8 Hz).
3. Entfernte Guards laufen im "coarse mode" (2 Hz, vereinfachtes Patrol-Sampling).
4. Globale harte Obergrenze fuer aktive Guards (MVP: 4-8 je nach Preset).
5. Spawn-Strategie: bevorzugt auf BFS-Pfad Start->Goal (siehe Patrouillenrouten-Generator).

### 6) Fairness-Mechaniken

1. **Startschutz:** Keine Entdeckung in den ersten 2.0s nach Run-Start.
2. **Mindest-Spawnabstand zur Startzelle: 6 Zellen Manhattan (gilt bereits ab Phase 23).**
3. Guard-FOV +/- 70 Grad, Ausweichen hinter Ecken bleibt moeglich.
4. **Kurze "lost sight"-Gnade von 0.5s** (nicht sofort omniscient nach Ecke).
5. **Schleichmodus** als aktive Spielerfaehigkeit.
6. **ChaseLossTimer (Phase 23): 3s** ohne Sicht -> zurueck zu Patrol. In Phase 24 ersetzt durch Search/Return-Pipeline.

### 7) Verlustbedingung

1. Spieler verliert bei Zellkollision mit Guard.
2. Optional spaeter: "Near miss" oder Hitpoints, aber nicht im MVP.

## Visuelle und akustische Lesbarkeit (Pflicht im MVP)

Stealth lebt davon, dass der Spieler den Zustand des Gegners "lesen" kann. Diese Anteile sind im MVP **nicht optional** - sie sind Teil der Fairness, nicht reine UI-Politur.

1. **Sichtkegel als Spotlight:** Jeder Guard hat einen `SpotLight3D` als Kind, ausgerichtet entlang seiner aktuellen Cardinal-FacingDirection. Halbwinkel ~70 Grad, Reichweite proportional zur Erkennungsreichweite. Der Spieler sieht den Lichtkegel auf Boden und Waenden.
2. **Mode-Farbcode** (Material des Mesh + Spotlight-Tint):
   - Patrol: gruen
   - Alert: gelb (kurze 0.3s Phase)
   - Chase: rot
   - Search: orange (Phase 24)
   - Return: blau (Phase 24)
3. **Detection-Audio-Cues (Pflicht):**
   - Detection-Sting auf `Patrol -> Alert`.
   - Entkommen-Cue auf `Chase -> Patrol` bzw. `Chase -> Search`.
4. **Patrouillen-Hinweis (ab Phase 24):** Dezente Bodenmarkierung (Decal oder leicht andere Farbe) auf Patrouillenzellen, damit der Spieler die Routinen erkennen und planen kann.

## Spielerfaehigkeit: Schleichmodus

1. **Eingabe:** Modifier-Taste (Shift) waehrend Bewegungseingabe.
2. **Effekt:** Spielerbewegung 0.6x, Guard-Erkennungsreichweite halbiert.
3. **HUD:** kleines Schleich-Icon, wenn aktiv.
4. **Implementierung:** Speed-Modifier in `PlayerCharacter3D`, `IsSneaking`-Property fuer `GuardPerception`.

## Systemarchitektur

Neue Komponenten (MVP):

1. `scripts/Gameplay/GuardState.cs`
2. `scripts/Gameplay/GuardPerception.cs`
3. `scripts/Gameplay/GuardNavigator.cs`
4. `scripts/Gameplay/GuardPatrolRouteBuilder.cs`
5. `scripts/Gameplay/GuardDirector.cs`
6. `scripts/Gameplay/GuardTelemetry.cs`
7. `scripts/Views/GuardCharacter3D.cs`

Verantwortlichkeiten:

1. `GuardDirector`: Orchestrierung aller Guards, Tick, Skalierung, Spawns, Audio-Triggern.
2. `GuardState`: KI-Zustand und Runtime-Daten pro Guard.
3. `GuardPerception`: Sichtkegel + LOS auf Grid, beruecksichtigt Schleichmodus.
4. `GuardNavigator`: Pfad-/Schrittwahl fuer Patrol/Chase/Search/Return.
5. `GuardPatrolRouteBuilder`: Auto-Generator fuer Patrouillenrouten (Engstellen-Heuristik).
6. `GuardCharacter3D`: Visualisierung (Mesh + Spotlight), Mode-Farbcode, Cell-Animation.
7. `GuardTelemetry`: Sammeln und Logging der Run-Metriken.

Integration in bestehende Schichten:

1. `Main.cs`: Start/Stop bei Manual-Play, Niederlage-Flow.
2. `MazeView3D.tscn`: Guard-Knoten-Container, Audio-Player.
3. `Hud.tscn` / `Hud.cs`: Toggle + Statusfeedback + Schleich-Icon.
4. `PlayerCharacter3D.cs`: Schleichmodus-Speed-Modifier + `IsSneaking`-Property.

## Datenmodell (MVP)

`GuardState`:

1. `int GuardId`
2. `Cell CurrentCell`
3. `Direction FacingDirection`
4. `Cell LastKnownPlayerCell`
5. `GuardMode Mode`
6. `List<Cell> PatrolRoute`
7. `int PatrolIndex`
8. `float StateTimer`
9. `float RepathCooldown`
10. `float MoveCooldown`
11. `float ChaseLossTimer`

`GuardMode`:

1. `Patrol`
2. `Alert`
3. `Chase`
4. `Search`
5. `Return`

(Alle Modes ab Phase 23 im Enum vorhanden, damit Phase 24 ohne Enum-Erweiterung auskommt.)

## HUD/UX-Anforderungen

1. Checkbox `Guards aktiv` (nur in 3D/Manual sinnvoll).
2. Optionale Preset-Wahl `Guard-Schwierigkeit` (Easy/Normal/Hard, ab Phase 25).
3. Kurzes Textfeedback: `Entdeckt!`, `Suche laeuft`, `Entkommen`.
4. Schleichmodus-Indikator (Icon).
5. Keine dauernd laute UI; Hinweise kurz und eindeutig.
6. **Im 3D-View selbst:** Sichtkegel-Spotlight + Mode-Farbcode am Guard-Mesh sind Pflicht-UX (siehe Lesbarkeit).

## Telemetrie (didaktisch + balancing)

Erfassen pro Run via `GuardTelemetry` (Phase 24):

1. Anzahl Entdeckungen.
2. Mittlere Chase-Dauer und Chase-Gesamtdauer.
3. Zeit bis erste Entdeckung.
4. Distanz bei Niederlage (Manhattan zwischen Spielerstart und Niederlage-Zelle).
5. Schleichmodus-Anteil an Spielzeit.

Output:

- Konsolenlog am Run-Ende.
- Optional spaeter: in Datei serialisieren fuer Schueler-Analyse.

Nutzen:

1. Presets datenbasiert statt gefuehlt balancen.
2. Schueler koennen Verhalten von Zustandsautomaten analysieren.

## Performance-Budget (MVP-Orientierung)

1. Guard-Logik-Tick-Zeit soll im Mittel klein bleiben (Richtwert < 2 ms bei typischen Szenen).
2. Keine pathfinding-Neuberechnung jedes Frame; `RepathCooldown` ~0.4s.
3. LOS-Pruefung capped pro Tick (Budget-Ansatz).
4. Spotlight-Cones nur fuer Guards im Aktiv-Update (kein Spotlight im Coarse-Mode).

## Akzeptanzkriterien

1. Mindestens ein Guard patrouilliert sichtbar (mit Spotlight) und wechselt reproduzierbar zwischen Modes.
2. Sichtkegel ist visuell als Spotlight am Boden erkennbar; Mode-Farbcode greift.
3. Sichtkontakt ist fuer den Spieler erklaerbar (keine "durch Wand gesehen"-Effekte).
4. Detection-Audio-Cue ist hoerbar.
5. Schleichmodus reduziert Erkennung nachvollziehbar.
6. Mindestspawnabstand und Startschutz greifen ab Phase 23.
7. Phase 23: Loop schliesst sich (Chase endet via ChaseLossTimer, wenn Spieler entkommt).
8. Phase 24: Search/Return-Pipeline laeuft sauber.
9. In grossen Mazes bleibt Gameplay stabil (kein massiver Framerate-Einbruch durch Guard-Update).
10. Niederlage bei Kollision funktioniert deterministisch.
11. `dotnet build` bleibt gruen.

## Risiken und Gegenmassnahmen

1. **Risiko:** LOS-Logik an Ecken schwer korrekt.
   *Gegenmassnahme:* Grid-LOS klar spezifizieren und mit kommentierten Testfaellen absichern.
2. **Risiko:** Zu viele Guards machen Maze unfair.
   *Gegenmassnahme:* Harte Guard-Caps + distanzbasierte Aktivierung + Mindestspawnabstand.
3. **Risiko:** Chase fuehlt sich omniscient an.
   *Gegenmassnahme:* `LastKnownPlayerCell` + Search-Timer statt permanenter Spieler-Positionskenntnis.
4. **Risiko:** Spieler trifft in grossen Mazes nie auf Guards.
   *Gegenmassnahme:* Spawn auf BFS-Pfad Start->Goal gewichten (Phase 25).
5. **Risiko:** Spieler versteht Detection-Logik nicht.
   *Gegenmassnahme:* Spotlight-Sichtkegel + Mode-Farbcode + Detection-Audio sind MVP-Pflicht.
6. **Risiko:** Cell-Animation laesst Spieler "zwischen Zellen" stehen, Detection wirkt willkuerlich.
   *Gegenmassnahme:* Spielerposition fuer Sichttest = Quellzelle waehrend Animation (in Spec fix verankert).

## Geklaerte (frueher offene) Fragen

1. Sichtkegel: NUR FOV im MVP, kein 360-Grad-Trigger - waere unfair bei langsamer Spielerbewegung und schwer kommunizierbar.
2. Patrouillen ueber Kreuzungen/Korridore: ja, ueber Engstellen-Heuristik in `GuardPatrolRouteBuilder`.
3. Mehr-Guard-Support: Phase 25 (nach stabilem Slice und Fairness-Pass in Phase 24).
