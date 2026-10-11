namespace Novolis.Modeling;

/// <summary>Separates geometry readiness from appearance attribute readiness.</summary>
public sealed record MeshReadiness
{
    public MeshReadinessState Geometry { get; init; }

    public MeshReadinessState Appearance { get; init; }

    public MeshAttributeSet AvailableAttributes { get; init; }

    public MeshAttributeFlags MissingGeometryAttributes { get; init; }

    public MeshAttributeFlags MissingAppearanceAttributes { get; init; }

    public bool IsGeometryReady => Geometry == MeshReadinessState.Ready;

    public bool IsAppearanceReady => Appearance == MeshReadinessState.Ready;

    public bool IsReady => IsGeometryReady && IsAppearanceReady;

    public static MeshReadiness Empty { get; } = From(MeshAttributeSet.None);

    public static MeshReadiness From(
        MeshAttributeSet available,
        MeshAttributeFlags requiredAppearance = MeshAttributeSet.AppearanceDefaults)
    {
        var missingGeometry = available.Missing(MeshAttributeSet.GeometryRequired);
        var missingAppearance = available.Missing(requiredAppearance);
        return new MeshReadiness
        {
            Geometry = StateFor(available.Flags & MeshAttributeSet.GeometryRequired, missingGeometry),
            Appearance = StateFor(available.Flags & requiredAppearance, missingAppearance),
            AvailableAttributes = available,
            MissingGeometryAttributes = missingGeometry,
            MissingAppearanceAttributes = missingAppearance,
        };
    }

    private static MeshReadinessState StateFor(
        MeshAttributeFlags available,
        MeshAttributeFlags missing)
    {
        if (missing == MeshAttributeFlags.None)
            return MeshReadinessState.Ready;
        return available == MeshAttributeFlags.None
            ? MeshReadinessState.Unavailable
            : MeshReadinessState.Partial;
    }
}
