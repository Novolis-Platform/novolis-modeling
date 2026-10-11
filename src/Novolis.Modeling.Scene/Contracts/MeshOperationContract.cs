namespace Novolis.Modeling;

/// <summary>Declared requirements and stage ownership for a mesh operation.</summary>
public sealed record MeshOperationContract
{
    public string Name { get; init; } = "operation";

    public MeshOperationKind Kind { get; init; } = MeshOperationKind.Geometry;

    public MeshAttributeFlags RequiredAttributes { get; init; } = MeshAttributeSet.GeometryRequired;

    /// <summary>
    /// Indicates whether the operation is allowed to report a geometry change.
    /// Appearance-only contracts must leave this false.
    /// </summary>
    public bool ClaimsGeometryChange { get; init; }

    public bool IsAppearanceOnly => Kind == MeshOperationKind.Appearance;

    public static MeshOperationContract AppearanceOnly(
        string name,
        MeshAttributeFlags requiredAppearance = MeshAttributeSet.AppearanceDefaults) =>
        new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? "appearance" : name,
            Kind = MeshOperationKind.Appearance,
            RequiredAttributes = MeshAttributeSet.GeometryRequired | requiredAppearance,
            ClaimsGeometryChange = false,
        };

    public static MeshOperationContract Geometry(
        string name,
        MeshAttributeFlags required = MeshAttributeFlags.None) =>
        new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? "geometry" : name,
            Kind = MeshOperationKind.Geometry,
            RequiredAttributes = MeshAttributeSet.GeometryRequired | required,
            ClaimsGeometryChange = true,
        };
}
