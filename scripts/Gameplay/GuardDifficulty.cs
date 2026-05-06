namespace Maze.Gameplay;

/// <summary>
/// Schwierigkeitspresets fuer Guards (Phase 25). MVP: Easy/Normal/Hard als Parameterpakete.
/// Konkretes Mapping liegt im <see cref="GuardDirector"/> ueber GuardCount-Parameter und
/// kann spaeter (nach Bedarf) in Direktor-Felder durchgereicht werden.
/// </summary>
public enum GuardDifficulty
{
    Easy,
    Normal,
    Hard
}

public sealed class GuardDifficultyPreset
{
    public GuardDifficulty Difficulty { get; init; } = GuardDifficulty.Normal;
    public int GuardCount { get; init; } = 1;
    public float DetectionRangeCells { get; init; } = 8f;
    public float SneakDetectionFactor { get; init; } = 0.5f;

    public static GuardDifficultyPreset For(GuardDifficulty d) => d switch
    {
        GuardDifficulty.Easy => new GuardDifficultyPreset
        {
            Difficulty = GuardDifficulty.Easy,
            GuardCount = 1,
            DetectionRangeCells = 6f,
            SneakDetectionFactor = 0.4f
        },
        GuardDifficulty.Hard => new GuardDifficultyPreset
        {
            Difficulty = GuardDifficulty.Hard,
            GuardCount = 4,
            DetectionRangeCells = 10f,
            SneakDetectionFactor = 0.6f
        },
        _ => new GuardDifficultyPreset
        {
            Difficulty = GuardDifficulty.Normal,
            GuardCount = 2,
            DetectionRangeCells = 8f,
            SneakDetectionFactor = 0.5f
        }
    };
}
