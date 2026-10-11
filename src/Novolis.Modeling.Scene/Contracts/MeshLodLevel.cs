namespace Novolis.Modeling;

/// <summary>Stable quality-independent level-of-detail selection.</summary>
public readonly record struct MeshLodLevel
{
    public MeshLodLevel(int level, float screenCoverage = 1f, int targetTriangleCount = 0)
    {
        if (level < 0)
            throw new ArgumentOutOfRangeException(nameof(level));
        if (!float.IsFinite(screenCoverage) || screenCoverage < 0f)
            throw new ArgumentOutOfRangeException(nameof(screenCoverage));
        if (targetTriangleCount < 0)
            throw new ArgumentOutOfRangeException(nameof(targetTriangleCount));

        Level = level;
        ScreenCoverage = screenCoverage;
        TargetTriangleCount = targetTriangleCount;
    }

    public int Level { get; }

    public float ScreenCoverage { get; }

    public int TargetTriangleCount { get; }

    public bool IsBase => Level == 0;

    public string StableKey =>
        $"lod:{Level};coverage:{ScreenCoverage:R};triangles:{TargetTriangleCount}";

    public static MeshLodLevel Base => new(0);

    public static MeshLodLevel Create(int level, int targetTriangleCount) =>
        new(level, screenCoverage: 1f, targetTriangleCount);
}
