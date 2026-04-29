using System;
using Godot;

namespace Maze.UI;

/// <summary>
/// Panel mit Live-Statistiken fuer Generierung und Loesung.
/// </summary>
public partial class StatsPanel : PanelContainer
{
    private Label _timeLabel = null!;
    private Label _stepsLabel = null!;
    private Label _visitedLabel = null!;
    private Label _pathLabel = null!;
    private Label _memoryLabel = null!;

    public override void _Ready()
    {
        _timeLabel = GetNode<Label>("Margin/VBox/TimeLabel");
        _stepsLabel = GetNode<Label>("Margin/VBox/StepsLabel");
        _visitedLabel = GetNode<Label>("Margin/VBox/VisitedLabel");
        _pathLabel = GetNode<Label>("Margin/VBox/PathLabel");
        _memoryLabel = GetNode<Label>("Margin/VBox/MemoryLabel");
    }

    public void UpdateStats(TimeSpan elapsed, int steps, int visited, int pathLength, long memoryDeltaBytes)
    {
        _timeLabel.Text = $"Zeit:      {elapsed.TotalMilliseconds:F1} ms";
        _stepsLabel.Text = $"Schritte:  {steps}";
        _visitedLabel.Text = $"Besucht:   {visited}";
        _pathLabel.Text = $"Pfadlaenge: {pathLength}";
        _memoryLabel.Text = $"Speicher:  {memoryDeltaBytes / 1024.0:F1} KB";
    }
}