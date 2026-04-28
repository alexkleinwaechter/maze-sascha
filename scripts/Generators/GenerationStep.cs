using Maze.Model;

namespace Maze.Generators;

/// <summary>
/// Beschreibt einen einzelnen Animationsschritt eines Generators.
/// Enthaelt genug Information fuer die Visualisierung, aber keine Render-Logik.
/// </summary>
public sealed record GenerationStep(
    /// <summary>Zelle, deren Zustand sich aendert (z. B. neue Carving-Zelle).</summary>
    Cell Cell,
    /// <summary>Optionaler Nachbar - wenn gesetzt, wird die Wand zwischen Cell und Neighbor entfernt.</summary>
    Cell Neighbor,
    /// <summary>Richtung von Cell -> Neighbor (fuer die Wandberechnung).</summary>
    Direction? RemoveWallTowards,
    /// <summary>Neuer Zellzustand fuer die Visualisierung.</summary>
    CellState NewState,
    /// <summary>Frei waehlbarer Beschreibungstext (z. B. fuer Debug/Stats).</summary>
    string Description
);
