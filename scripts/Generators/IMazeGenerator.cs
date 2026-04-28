using System.Collections.Generic;

namespace Maze.Generators;

/// <summary>
/// Gemeinsame Schnittstelle aller Erstellungsalgorithmen.
/// Liefert eine Sequenz von GenerationStep-Objekten - eines pro Animationsschritt.
/// </summary>
public interface IMazeGenerator
{
    /// <summary>Eindeutige ID, die im HUD-OptionButton verwendet wird.</summary>
    string Id { get; }

    /// <summary>Lesbarer Anzeigename.</summary>
    string Name { get; }

    /// <summary>
    /// Generiert das uebergebene Labyrinth in-place und liefert die Schritte.
    /// Implementierungen verwenden System.Random als Quelle fuer Zufall.
    /// </summary>
    IEnumerable<GenerationStep> Generate(Model.Maze maze, System.Random random);
}
