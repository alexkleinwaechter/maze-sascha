# Maze Patrol Guards - Balancing-Notizen

> **Status:** Initial-Draft, erstellt mit Phase-23/24/25-Implementation am 2026-05-06.
> **Spec:** `docs/superpowers/specs/2026-05-06-maze-patrol-guards-design.md`

## Aktive Defaults (Stand Implementation)

| Parameter                           | Wert                |
|-------------------------------------|---------------------|
| Detection-Reichweite                | 8 Zellen            |
| FOV-Halbwinkel                      | 70 Grad             |
| Tick-Rate                           | 8 Hz                |
| Patrol-Speed                        | 0.7x Spieler        |
| Chase-Speed                         | 1.15x Spieler       |
| Search-Speed                        | 0.8x Spieler        |
| Return-Speed                        | 1.0x Spieler        |
| Schleich-Speed                      | 0.6x Spieler        |
| Schleich-Reichweiten-Faktor         | 0.5x                |
| Alert-Reaktionszeit                 | 0.3s                |
| Sichtverlust-Gnade                  | 0.5s                |
| Suchbudget                          | 5s ODER 8 Schritte  |
| Mindestspawnabstand zur Startzelle  | 6 Zellen Manhattan  |
| Mindestabstand zwischen Spawns      | 5 Zellen Manhattan  |
| Startschutz                         | 2.0s                |
| Repath-Cooldown                     | 0.4s                |

## Difficulty-Presets (`GuardDifficultyPreset`)

| Preset | GuardCount | Detection-Reichweite | Sneak-Faktor |
|--------|------------|----------------------|--------------|
| Easy   | 1          | 6 Zellen             | 0.4x         |
| Normal | 2          | 8 Zellen             | 0.5x         |
| Hard   | 4          | 10 Zellen            | 0.6x         |

(Sneak-Faktor: kleiner = staerkere Reduktion durch Schleichen.)

## Geplante Testmatrix

Erfassung der Telemetriewerte aus `GuardTelemetry.Summarize()` jeweils nach 3-5 Runs pro Zelle der Matrix.

| Maze-Groesse | Easy | Normal | Hard |
|--------------|------|--------|------|
| 15x15        | -    | -      | -    |
| 35x35        | -    | -      | -    |
| 75x75        | -    | -      | -    |
| 125x125      | -    | -      | -    |

Zu erfassen pro Lauf:

- Erfolgsquote (Spieler erreicht Goal vs. Niederlage).
- Anzahl Entdeckungen (`Telemetry.Detections`).
- Mittlere Chase-Dauer (`Telemetry.ChaseTotalSeconds / Detections`).
- Time-to-First-Detection.
- Subjektiver Frustpunkt (qualitativ): "fair" / "knapp" / "unfair".

## Frueh erkennbare Stellschrauben

1. **Begegnungsdichte in grossen Mazes:** Bei 125x125 mit Hard (4 Guards) ist die Wahrscheinlichkeit, einen Guard zu sehen, immer noch sehr niedrig (~0.025% der Zellen). Spawn-auf-Loesungspfad-Heuristik ist da kritisch. Wenn das nicht reicht, GuardCount in Hard auf 6-8 erhoehen.
2. **Chase-Speed 1.15x:** Wirkt im engen Maze mit Sackgassen evtl. zu brutal, weil man nicht ausweichen kann. Kandidat zum Tunen, falls Frust auftritt.
3. **Schleich-Faktor 0.5:** Halbierte Reichweite ist deutlich, aber bei 8-Zellen-Default heisst das immer noch 4 Zellen. In sehr offenen Bereichen evtl. zu wenig Schutz.

## Frust-Kandidaten (zu testen)

- Spawn nahe am Goal: Spieler sieht Guard erst, wenn er fast da ist. Phase-25-Spawn auf BFS-Pfad mildert das, aber Goal-naehe-Cap waere zusaetzlicher Schutz.
- Schleichmodus + 1.15x Chase = nahezu unentkommbar, wenn Chase einmal triggert. Tradeoff vermutlich OK, weil Schleichen den Chase ja vermeiden soll.
- Patrouillenroute kreuzt Loesungsweg an Engstelle: macht den Maze evtl. unloesbar. Routen-Builder bevorzugt Korridorzellen, was das verstaerkt. Beobachten.

## Offene Punkte

- Audio-Assets fehlen noch: Director triggert `_detectAudio.Play()` und `_escapeAudio.Play()`, die `AudioStreamPlayer`-Knoten haben aber keinen Stream gesetzt. Muss noch befuellt werden (`assets/audio/guards/detect.ogg`, `escape.ogg`).
- Patrouillen-Bodenmarker (Phase 24.3) noch nicht implementiert - dezenter Decal/Pad pro Patrouillenzelle waere die nachvollziehbarkeitsfoerdernde Ergaenzung.
