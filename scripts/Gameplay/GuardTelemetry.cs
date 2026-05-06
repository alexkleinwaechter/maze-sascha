using Maze.Model;

namespace Maze.Gameplay;

/// <summary>
/// Sammelt Run-Metriken fuer Balancing und Schueler-Analyse.
/// </summary>
public sealed class GuardTelemetry
{
    public int Detections { get; private set; }
    public int Escapes { get; private set; }
    public float ChaseTotalSeconds { get; private set; }
    public float TimeToFirstDetection { get; private set; } = -1f;
    public float SneakTimeSeconds { get; private set; }
    public int DefeatDistanceManhattan { get; private set; } = -1;
    public Cell DefeatCell { get; private set; }

    public void OnDetection(float runTimeSeconds)
    {
        Detections++;
        if (TimeToFirstDetection < 0f)
            TimeToFirstDetection = runTimeSeconds;
    }

    public void OnEscape() => Escapes++;

    public void OnChaseTick(float dt) => ChaseTotalSeconds += dt;

    public void OnSneakTick(float dt) => SneakTimeSeconds += dt;

    public void OnCaught(Cell cell)
    {
        DefeatCell = cell;
    }

    public void RecordDefeatDistance(int manhattan) => DefeatDistanceManhattan = manhattan;

    public void OnRunEnd()
    {
        // Hook fuer spaetere Datei-Serialisierung; aktuell nur Marker.
    }

    public string Summarize()
    {
        float avgChase = Detections > 0 ? ChaseTotalSeconds / Detections : 0f;
        return $"Detections={Detections} ChaseTotal={ChaseTotalSeconds:0.00}s AvgChase={avgChase:0.00}s "
             + $"FirstDetect={TimeToFirstDetection:0.00}s Sneak={SneakTimeSeconds:0.00}s "
             + $"DefeatDist={DefeatDistanceManhattan}";
    }
}
