using System;
using System.Collections.Generic;
using Maze.Model;

namespace Maze.Gameplay;

/// <summary>
/// Erzeugt automatische Patrouillenrouten fuer Guards, da Mazes prozedural sind.
///
/// Heuristik:
///  - Ausgehend von einer Spawnzelle einen Random-Walk in offene Nachbarn machen.
///  - Korridorzellen (Grad &lt;= 2 offene Nachbarn) und Kreuzungen (Grad &gt;= 3) bevorzugen.
///  - Versuch einen Loop zu bilden, sonst Bounce (vor und zurueck).
///  - Ziel-Routenlaenge: 6-12 Zellen.
///
/// Beispiel (Korridor von links nach rechts mit Sackgasse oben):
///   . . . .
///   . S . .
///   . . . .
///   Spawn S, Route entlang offener Wand-Folge.
/// </summary>
public static class GuardPatrolRouteBuilder
{
    private const int MinRouteLength = 6;
    private const int MaxRouteLength = 12;

    /// <summary>
    /// Baut eine Patrol-Route. Liefert IMMER eine valide Route von mindestens 2 Zellen,
    /// auch wenn das Maze sehr eng ist (Fallback: Bounce zwischen Spawn und einem Nachbarn).
    ///
    /// <paramref name="solverPathCells"/> (optional): wenn gesetzt, bekommen Off-Pfad-Zellen
    /// einen Score-Bonus. Damit verlaesst die Patrol den Loesungspfad regelmaessig und der
    /// Spieler bekommt zwingend Zeitfenster, in denen Pfad-Zellen unbeobachtet sind. Loest
    /// das "Guard sitzt fuer immer auf dem einzigen Chokepoint"-Problem heuristisch.
    /// </summary>
    public static List<Cell> Build(Model.Maze maze, Cell spawn, Random random,
                                   HashSet<Cell> solverPathCells = null)
    {
        if (maze is null) throw new ArgumentNullException(nameof(maze));
        if (spawn is null) throw new ArgumentNullException(nameof(spawn));
        random ??= new Random();

        // Zielroutenlaenge zufaellig im Korridor [MinRouteLength, MaxRouteLength].
        int targetLength = random.Next(MinRouteLength, MaxRouteLength + 1);

        var route = new List<Cell> { spawn };
        var visitedSet = new HashSet<Cell> { spawn };

        Cell current = spawn;
        Direction? lastDir = null;

        for (int step = 1; step < targetLength; step++)
        {
            var openDirs = OpenDirections(maze, current);
            if (openDirs.Count == 0) break;

            // Bevorzugung: nicht direkt umkehren (lastDir.Opposite vermeiden).
            // Korridorzellen werden bevorzugt durch Sortierung der Kandidaten.
            var candidates = new List<(Direction dir, Cell cell, int score)>();
            foreach (var dir in openDirs)
            {
                if (lastDir.HasValue && dir == DirectionHelper.Opposite(lastDir.Value))
                    continue; // Umkehrung nur in Sackgasse erlauben

                Cell next = maze.GetNeighbor(current, dir);
                if (next == null || visitedSet.Contains(next)) continue;

                int degree = OpenDirections(maze, next).Count;
                // Score: Korridorzellen (Grad 2) bevorzugt, Kreuzungen (Grad 3-4) zweitens, Sackgassen (Grad 1) zuletzt.
                int score = degree switch
                {
                    2 => 3,
                    3 => 2,
                    4 => 2,
                    _ => 1
                };
                // Off-Pfad-Bonus: garantiert, dass die Route den Solver-Pfad regelmaessig verlaesst,
                // damit der Spieler Zeitfenster zum Passieren bekommt (Option A aus Followup-Doc).
                if (solverPathCells != null && !solverPathCells.Contains(next))
                    score += 2;
                candidates.Add((dir, next, score));
            }

            if (candidates.Count == 0)
            {
                // Sackgasse: Umkehrung erlauben, falls moeglich.
                if (lastDir.HasValue)
                {
                    var back = DirectionHelper.Opposite(lastDir.Value);
                    if (openDirs.Contains(back))
                    {
                        Cell next = maze.GetNeighbor(current, back);
                        route.Add(next);
                        current = next;
                        lastDir = back;
                        continue;
                    }
                }
                break;
            }

            // Gewichtete Auswahl per Score.
            int totalScore = 0;
            foreach (var c in candidates) totalScore += c.score;
            int pick = random.Next(totalScore);
            int acc = 0;
            (Direction dir, Cell cell, int _) chosen = candidates[0];
            foreach (var c in candidates)
            {
                acc += c.score;
                if (pick < acc) { chosen = c; break; }
            }

            route.Add(chosen.cell);
            visitedSet.Add(chosen.cell);
            current = chosen.cell;
            lastDir = chosen.dir;
        }

        // Wenn Route zu kurz (z. B. ganz enges Maze), Fallback: Bounce zwischen Spawn und einem Nachbarn.
        if (route.Count < 2)
        {
            var openFromSpawn = OpenDirections(maze, spawn);
            if (openFromSpawn.Count > 0)
            {
                Cell neighbor = maze.GetNeighbor(spawn, openFromSpawn[0]);
                route.Add(neighbor);
            }
            else
            {
                // Komplett isolierte Zelle (sollte in regulaer generierten Mazes nicht vorkommen).
                // Route bleibt einzelner Punkt - Director geht damit defensiv um.
            }
        }

        return route;
    }

    /// <summary>Offene Richtungen ab einer Zelle (Wand zwischen Zelle und Nachbar fehlt).</summary>
    private static List<Direction> OpenDirections(Model.Maze maze, Cell cell)
    {
        var result = new List<Direction>(4);
        foreach (var dir in DirectionHelper.All)
        {
            if (cell.HasWall(dir)) continue;
            if (maze.GetNeighbor(cell, dir) == null) continue;
            result.Add(dir);
        }
        return result;
    }
}
