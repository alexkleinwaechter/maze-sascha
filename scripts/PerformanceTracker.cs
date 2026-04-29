using System;
using System.Diagnostics;

namespace Maze;

/// <summary>
/// Misst Laufzeit, Schrittanzahl und Speicherdelta fuer einen Algorithmus-Lauf.
/// </summary>
public sealed class PerformanceTracker
{
    private readonly Stopwatch _stopwatch = new();
    private long _memoryBefore;

    public TimeSpan Elapsed => _stopwatch.Elapsed;
    public int Steps { get; private set; }
    public int VisitedCells { get; private set; }
    public int PathLength { get; private set; }
    public long ManagedMemoryDeltaBytes { get; private set; }

    public void Start()
    {
        Steps = 0;
        VisitedCells = 0;
        PathLength = 0;
        ManagedMemoryDeltaBytes = 0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _memoryBefore = GC.GetTotalMemory(false);
        _stopwatch.Restart();
    }

    public void TickStep() => Steps++;

    public void IncrementVisited() => VisitedCells++;

    public void SetPathLength(int length) => PathLength = length;

    public void Stop()
    {
        _stopwatch.Stop();
        long after = GC.GetTotalMemory(false);
        ManagedMemoryDeltaBytes = after - _memoryBefore;
    }
}