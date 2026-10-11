namespace Novolis.Modeling;

/// <summary>Truthful validation result for a mesh operation contract.</summary>
public sealed record MeshContractValidation
{
    public bool IsValid { get; init; }

    public MeshOperationContract Operation { get; init; } = new();

    public MeshAttributeSet AvailableAttributes { get; init; }

    public MeshReadiness Readiness { get; init; } = MeshReadiness.Empty;

    public MeshAttributeFlags MissingAttributes { get; init; }

    public bool ClaimsGeometryChange => Operation.ClaimsGeometryChange;

    public bool GeometryChanged => Operation.Kind == MeshOperationKind.Geometry;

    public bool AppearanceChanged => Operation.Kind == MeshOperationKind.Appearance;

    public IReadOnlyList<string> Errors { get; init; } = [];
}
