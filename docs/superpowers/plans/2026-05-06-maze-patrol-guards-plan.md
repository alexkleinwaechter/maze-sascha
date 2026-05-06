# Maze School Project - Patrol Guards (Kandidat B) - Implementierungsplan

> **Fuer agentische Worker:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development` oder `superpowers:executing-plans` fuer task-by-task Umsetzung.
>
> **Fuer Schueler:** Jeder Task muss einzeln baubar bleiben (`dotnet build` gruen).
>
> **Spec:** `docs/superpowers/specs/2026-05-06-maze-patrol-guards-design.md`

**Goal:** Patrouillen-Waechter mit sichtbarem Sichtkegel in den 3D-Manual-Modus integrieren, inklusive Zustandsmaschine, fairer Verfolgung, Schleichmodus, Audio, auto-generierten Patrouillenrouten und skalierbarer Performance.

**Architektur:**

1. Gameplay-Kern als eigene C#-Klassen (`GuardDirector`, `GuardState`, `GuardPerception`, `GuardNavigator`, `GuardPatrolRouteBuilder`, `GuardTelemetry`).
2. Rendering separat in `GuardCharacter3D` (Mesh + `SpotLight3D` + Mode-Farbcode).
3. `Main` orchestriert Mode-Lifecycle und Verlustbedingung.
4. `PlayerCharacter3D` exponiert `IsSneaking` fuer den Schleichmodus.

---

## Phase 23 - Vertikaler Slice (1 Guard, voller Loop, sichtbar/hoerbar/fair)

Ziel: Komplett spielbares, loop-faehiges Grundsystem inklusive Sichtkegel-Visualisierung, Audio, Schleichmodus, Mindestspawnabstand und ChaseLossTimer. Search/Return werden vereinfacht ueber den Timer abgebildet (Feinschliff in Phase 24).

### Task 23.0: Niederlage-Flow sicherstellen (Vorbedingung)

**Files:**
- Modify (falls noetig): `scripts/Main.cs`, `scripts/Hud/Hud.cs`, `scenes/Hud.tscn`

- [ ] Pruefen, ob Game-Over/Restart-Flow im Manual-Play existiert.
- [ ] Falls nicht: minimaler Restart-Pfad (Hud-Overlay "Run verloren - Neustart?" + Reset-Hook).
- [ ] API: `Main.HandleManualDefeat(string reason)` als zentraler Eintrittspunkt fuer Niederlage.
- [ ] `dotnet build` gruen.

### Task 23.1: Guard-Basisklasse und Modus-Enum

**Files:**
- Create: `scripts/Gameplay/GuardState.cs`

- [ ] `GuardMode`-Enum mit `Patrol`, `Alert`, `Chase`, `Search`, `Return` (alle, damit Phase 24 ohne Enum-Erweiterung auskommt).
- [ ] `GuardState` mit Laufzeitfeldern aus Spec-Datenmodell:
      `GuardId`, `CurrentCell`, `FacingDirection`, `LastKnownPlayerCell`, `Mode`,
      `PatrolRoute`, `PatrolIndex`, `StateTimer`, `RepathCooldown`, `MoveCooldown`, `ChaseLossTimer`.
- [ ] Daten bewusst Godot-unabhaengig halten (reine Logikklasse).

### Task 23.2: Patrouillenrouten-Generator (Auto-Gen)

**Files:**
- Create: `scripts/Gameplay/GuardPatrolRouteBuilder.cs`

- [ ] Eingang: `Maze`-Instanz, Spawnzelle.
- [ ] Erzeugt 6-12-Zellen-Route ueber bevorzugt Korridorzellen (Grad <= 2) und Kreuzungen.
- [ ] Loop wo moeglich, sonst Bounce.
- [ ] Validierung: alle Schritte durch offene Waende verbunden (`Cell.HasWall == false`).
- [ ] Kommentierte Beispiele/Testfaelle im Code (kleines 5x5 Maze).

### Task 23.3: 3D-Visualisierung mit Spotlight und Mode-Farbcode

**Files:**
- Modify: `scenes/MazeView3D.tscn`
- Create: `scripts/Views/GuardCharacter3D.cs`

- [ ] `Guards`-Container als Kind von `MazeView3D`.
- [ ] Mesh-Visualisierung (Capsule oder Cuboid).
- [ ] `SpotLight3D` als Kind, ausgerichtet entlang FacingDirection. Halbwinkel ~70 Grad, Reichweite proportional zur Erkennungsreichweite (8 Zellen Standard).
- [ ] API:
      `PlaceAtCell(Cell cell, float cellSize)`,
      `AnimateToCell(Cell target, float duration)`,
      `SetFacing(Direction d)`,
      `SetModeColor(GuardMode m)` (Patrol gruen, Alert gelb, Chase rot, Search orange, Return blau).
- [ ] Material und Spotlight-Tint folgen dem Mode-Farbcode.

### Task 23.4: GuardPerception-Modul (Sicht, LOS, Schleichmodus)

**Files:**
- Create: `scripts/Gameplay/GuardPerception.cs`

- [ ] FOV-Pruefung als pure Funktion: ist `(playerCell, guardCell, guardFacing)` im Halbwinkel +/- 70 Grad um Cardinal Direction?
- [ ] Grid-LOS mit Wandblocking (`Cell.HasWall`) zellweise.
- [ ] Erkennungsreichweite 8 Zellen (Default), halbiert bei `IsSneaking`.
- [ ] Spielerposition waehrend Cell-Animation: Quellzelle gilt als "wo der Spieler ist".
- [ ] Kommentierte Testfaelle im Code (gerader Korridor, Kreuzung, Wand zwischen Zellen, Schleichen).

### Task 23.5: GuardDirector (1 Guard, voller Loop)

**Files:**
- Create: `scripts/Gameplay/GuardDirector.cs`

- [ ] Tick-Loop fest auf 8 Hz (`Timer`-Knoten oder akkumulierter `_PhysicsProcess`).
- [ ] Spawn: Mindestspawnabstand 6 Zellen Manhattan zur Spielerstart-Zelle.
- [ ] Startschutz 2.0s nach Run-Start: keine Detection.
- [ ] Patrol: zyklisch entlang `PatrolRoute` mit Patrol-Speed (0.7x Spieler).
- [ ] Detection ueber `GuardPerception`: Patrol -> Alert (Reaktionszeit 0.3s).
- [ ] Alert -> Chase: Chase-Speed 1.15x Spieler, naechster Schritt Richtung `LastKnownPlayerCell` via einfacher BFS-Schritt.
- [ ] **ChaseLossTimer:** wenn 3s ohne Sicht, Chase -> Patrol (Phase-23-Vereinfachung; in Phase 24 ersetzt durch Search-Pipeline).
- [ ] FacingDirection wird beim Schritt aktualisiert; `GuardCharacter3D.SetFacing` aufrufen.

### Task 23.6: Schleichmodus in PlayerCharacter3D

**Files:**
- Modify: `scripts/Views/PlayerCharacter3D.cs`
- Modify: `scripts/Hud/Hud.cs`, `scenes/Hud.tscn`

- [ ] Eingabe: Modifier-Taste (Shift) detektieren, nur im Manual-Modus aktiv.
- [ ] Speed-Multiplikator 0.6x auf Cell-Animationsdauer.
- [ ] `IsSneaking` als oeffentliche Property.
- [ ] HUD-Indikator (kleines Icon, sichtbar nur wenn aktiv).

### Task 23.7: Audio-Cues (Detection / Entkommen)

**Files:**
- Modify: `scenes/MazeView3D.tscn`
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] Zwei `AudioStreamPlayer3D` (oder 2D, je nach Audio-Setup) am Guard-Container.
- [ ] Detection-Sting beim Wechsel `Patrol -> Alert`.
- [ ] Entkommen-Cue beim Wechsel `Chase -> Patrol` (via ChaseLossTimer).
- [ ] Platzhalter-Sounds OK (einfacher Synth-Stinger), Asset-Pfade unter `assets/audio/guards/` dokumentieren.

### Task 23.8: Integration in Main (Manual-Play Lifecycle)

**Files:**
- Modify: `scripts/Main.cs`

- [ ] Bei Start Manual-Play: `GuardDirector` initialisieren, Spawn pruefen, Routen ueber `GuardPatrolRouteBuilder` bauen.
- [ ] Spielerzelle und `IsSneaking` laufend an Director melden.
- [ ] Kollisionspruefung Guard/Spielerzelle -> `Main.HandleManualDefeat("guard")`.
- [ ] Bei Stop/Reset sauber deaktivieren.

### Task 23.9: Build- und Smoke-Test

- [ ] `dotnet build` erfolgreich.
- [ ] Manual-Run im Editor: Guard patrouilliert (gruener Spotlight), entdeckt (Wechsel auf gelb 0.3s, dann rot, Detection-Sting hoerbar), verfolgt, fangen funktioniert.
- [ ] Wegrennen funktioniert: nach 3s ohne Sicht kehrt Guard zur Patrouille zurueck (Entkommen-Cue hoerbar).
- [ ] Schleichmodus reduziert Erkennung sichtbar (Spotlight-Reichweite halbiert).
- [ ] Spawn nicht direkt neben Spielerstart.

---

## Phase 24 - Search/Return-Pipeline, Lesbarkeit, Telemetrie

Ziel: KI weniger "allwissend" machen, Patrouillen fuer Spieler lesbar, Run-Daten erfassen.

### Task 24.1: GuardNavigator-Modul fuer Pfadschritte

**Files:**
- Create: `scripts/Gameplay/GuardNavigator.cs`
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] Naechster Schritt zu Zielzelle via Grid-BFS.
- [ ] Fallback bei Blockade/kein Pfad (random offener Nachbar).
- [ ] Cooldown fuer Repath via `RepathCooldown` (Default 0.4s).
- [ ] Director benutzt Navigator fuer Chase/Search/Return.

### Task 24.2: Search- und Return-Verhalten

**Files:**
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] `Chase -> Search` bei Sichtverlust > 0.5s (loest ChaseLossTimer aus Phase 23 ab).
- [ ] Search: Random-Walk in Radius 3 oder kurze BFS-Tour um `LastKnownPlayerCell`, Search-Speed 0.8x.
- [ ] Search-Budget: 5s ODER 8 Schritte.
- [ ] `Search -> Return`: Pfad zur naechsten Patrol-Zelle, Return-Speed 1.0x.
- [ ] `Return -> Patrol`: Patrol-Zelle erreicht, weiter im Cycle.
- [ ] Mode-Farbcode (orange/blau) folgt automatisch.

### Task 24.3: Patrouillen-Sichtbarkeit fuer den Spieler

**Files:**
- Modify: `scripts/Views/MazeView3D.cs`
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] Dezenter Bodenmarker auf Patrouillenzellen (Decal, Partikel oder leicht abweichende Farbe).
- [ ] Sichtbar nur, wenn Guard mindestens einmal vorbeigekommen ist (Setting/Toggle moeglich).
- [ ] Per HUD-Toggle ein-/ausschaltbar.

### Task 24.4: HUD-Statusfeedback

**Files:**
- Modify: `scenes/Hud.tscn`
- Modify: `scripts/Hud/Hud.cs`

- [ ] Toggle `Guards aktiv` hinzufuegen.
- [ ] Statusmeldungen: `Entdeckt`, `Suche laeuft`, `Entkommen`.
- [ ] Toggle fuer Patrouillen-Bodenmarker.

### Task 24.5: Telemetrie

**Files:**
- Create: `scripts/Gameplay/GuardTelemetry.cs`
- Modify: `scripts/Gameplay/GuardDirector.cs`, `scripts/Main.cs`

- [ ] Counter erfassen: Entdeckungen, Chase-Gesamt-/Mittelwert, Time-to-First-Detection, Schleichmodus-Anteil, Distanz bei Niederlage (Manhattan).
- [ ] Reset bei Run-Start, Dump als Konsolenlog bei Run-Ende.
- [ ] Optional: in JSON-Datei serialisieren (default off).

### Task 24.6: Build- und Spieltest

- [ ] `dotnet build` erfolgreich.
- [ ] Spieltest: Guard verliert Spieler nach Sichtverlust, sucht lokal (orange), kehrt dann zur Route zurueck (blau).
- [ ] Telemetrie-Werte plausibel im Konsolenlog.
- [ ] Patrouillen-Bodenmarker erscheinen erwartungsgemaess.

---

## Phase 25 - Skalierung, Multi-Guard, Difficulty

Ziel: Kandidat B robust fuer groessere Labyrinthe machen.

### Task 25.1: Mehrere Guards (Director + View)

**Files:**
- Modify: `scripts/Gameplay/GuardDirector.cs`
- Modify: `scripts/Views/GuardCharacter3D.cs`
- Modify: `scenes/MazeView3D.tscn`

- [ ] Instanzverwaltung fuer 2..N Guards in Director und View.
- [ ] Kollisions-/State-Visualisierung pro Guard getrennt.
- [ ] Audio-Cues guard-spezifisch positioniert.

### Task 25.2: Spawn-Strategie (BFS-Pfad-Gewichtung)

**Files:**
- Modify: `scripts/Gameplay/GuardDirector.cs`
- Optional: `scripts/Gameplay/GuardPatrolRouteBuilder.cs`

- [ ] Spawnpunkte werden bevorzugt auf oder nahe dem BFS-Pfad Start->Goal gewaehlt (Begegnungswahrscheinlichkeit erhoehen).
- [ ] Mindestabstand zwischen Guard-Spawns (verhindert Cluster).
- [ ] Mindestabstand zur Spielerstart-Zelle weiterhin gewaehrleistet.

### Task 25.3: Distanzbasierte Update-Qualitaet

**Files:**
- Modify: `scripts/Gameplay/GuardDirector.cs`

- [ ] Nahe Guards (z. B. <= 20 Zellen): voller Tick (8 Hz), Spotlight aktiv.
- [ ] Ferne Guards: reduzierter Tick (2 Hz), coarse mode, Spotlight optional aus.
- [ ] Harter Maximalwert fuer LOS-Checks pro Tick.

### Task 25.4: Schwierigkeitspresets

**Files:**
- Create: `scripts/Gameplay/GuardDifficulty.cs`
- Modify: `scripts/Hud/Hud.cs`
- Modify: `scenes/Hud.tscn`

- [ ] Easy/Normal/Hard als Parameterpakete.
- [ ] Mapping auf: FOV, Detection-Reichweite, Suchdauer, Move-Speed-Multiplikatoren, Guard-Cap, Schleich-Erkennungsfaktor.
- [ ] Default = Normal, Easy als initial empfohlen (Tooltip im HUD).

### Task 25.5: Balancing-Notizen

**Files:**
- Create: `docs/superpowers/specs/2026-05-06-maze-patrol-guards-balance.md`

- [ ] Testmatrix fuer 15x15, 35x35, 75x75, 125x125.
- [ ] Erfolgsquote, Frustpunkte, durchschnittliche Chase-Dauer pro Preset.
- [ ] Telemetriedaten aus Phase 24 als Grundlage.

---

## Akzeptanzkriterien

1. Guards patrouillieren sichtbar (mit Spotlight) und reagieren nachvollziehbar auf Sichtkontakt.
2. Mode-Farbcode (gruen/gelb/rot/orange/blau) ist erkennbar.
3. Detection-Audio-Cue ist hoerbar.
4. Schleichmodus reduziert Erkennung nachvollziehbar.
5. Phase 23: Loop schliesst sich (Chase endet via ChaseLossTimer).
6. Phase 24: Guards verlieren Spieler plausibel und suchen lokal, dann zurueck zur Route.
7. Kollision fuehrt deterministisch zu Niederlage (Niederlage-Flow vorhanden ab Phase 23).
8. Mindestspawnabstand und Startschutz greifen ab Phase 23.
9. Telemetrie-Daten werden ab Phase 24 erfasst.
10. Performance bleibt in grossen Mazes spielbar.
11. `dotnet build` bleibt gruen.

## Technische Leitplanken

1. Keine Vermischung von Render-Code und KI-Logik.
2. Keine framegebundene Chase-Logik ohne Tick-Limit.
3. Keine "Wallhack"-Sicht im finalen MVP.
4. Spotlight-Cone, Mode-Farbcode und Detection-Audio sind nicht-optional - sie sind Teil der Fairness, nicht reine UI-Politur.
5. Spielerposition fuer Sichttest: Quellzelle waehrend Cell-Animation (konsistent in Director und Perception umgesetzt).

## Empfehlung fuer die erste Umsetzung

1. Phase 23 vollstaendig liefern - das ist jetzt ein voller Loop mit Visuals, Audio, Schleichmodus und ChaseLossTimer.
2. Phase 24 fuer Search/Return + Telemetrie + Patrouillen-Lesbarkeit.
3. Phase 25 erst starten, wenn Spielgefuehl in Phase 24 ueber Telemetrie nachgemessen ist.
