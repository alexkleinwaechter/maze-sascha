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
