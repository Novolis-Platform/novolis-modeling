using System.Numerics;

namespace Novolis.Modeling;

/// <summary>Immutable set of attributes actually available on a mesh.</summary>
public readonly record struct MeshAttributeSet(MeshAttributeFlags Flags)
{
    /// <summary>Attributes needed to describe indexed triangle geometry.</summary>
    public const MeshAttributeFlags GeometryRequired =
        MeshAttributeFlags.Positions | MeshAttributeFlags.Indices;

    /// <summary>Attributes commonly consumed by appearance evaluation.</summary>
    public const MeshAttributeFlags AppearanceDefaults =
        MeshAttributeFlags.Normals | MeshAttributeFlags.UV0;

    /// <summary>All optional attributes owned by appearance evaluation.</summary>
    public const MeshAttributeFlags AppearanceAvailable =
        MeshAttributeFlags.Normals
        | MeshAttributeFlags.Tangents
        | MeshAttributeFlags.UV0
        | MeshAttributeFlags.MaterialSlots
        | MeshAttributeFlags.Curvature
        | MeshAttributeFlags.Cavity;

    public static MeshAttributeSet None => new(MeshAttributeFlags.None);

    public bool Has(MeshAttributeFlags attributes) => (Flags & attributes) == attributes;

    public bool HasAny(MeshAttributeFlags attributes) => (Flags & attributes) != 0;

    public MeshAttributeFlags Missing(MeshAttributeFlags required) => required & ~Flags;

    public static MeshAttributeSet From(EvaluatedMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var flags = MeshAttributeFlags.None;
        var vertices = mesh.Vertices;
        var indices = mesh.Indices;

        if (vertices is not null && AreFinite(vertices))
            flags |= MeshAttributeFlags.Positions;

        if (indices is not null
            && indices.Length % 3 == 0
            && AreValidIndices(indices, vertices?.Length ?? 0))
            flags |= MeshAttributeFlags.Indices;

        if (mesh.Normals is { Length: > 0 } normals
            && normals.Length == vertices?.Length
            && AreFinite(normals))
            flags |= MeshAttributeFlags.Normals;

        if (mesh.Tangents is { Length: > 0 } tangents
            && tangents.Length == vertices?.Length
            && AreFinite(tangents))
            flags |= MeshAttributeFlags.Tangents;

        if (mesh.UVs is { Length: > 0 } uvs
            && uvs.Length == vertices?.Length
            && AreFinite(uvs))
            flags |= MeshAttributeFlags.UV0;

        if (mesh.MaterialSlots is { Length: > 0 } materialSlots
            && materialSlots.Length == (indices?.Length ?? 0) / 3
            && materialSlots.All(static slot => slot >= 0))
            flags |= MeshAttributeFlags.MaterialSlots;

        if (mesh.Curvature is { Length: > 0 } curvature
            && curvature.Length == vertices?.Length
            && curvature.All(float.IsFinite))
            flags |= MeshAttributeFlags.Curvature;

        if (mesh.Cavity is { Length: > 0 } cavity
            && cavity.Length == vertices?.Length
            && cavity.All(float.IsFinite))
            flags |= MeshAttributeFlags.Cavity;

        if (!mesh.Bounds.IsEmpty && mesh.Bounds.IsFinite)
            flags |= MeshAttributeFlags.Bounds;

        if (mesh.Topology is { } topology
            && topology.VertexCount == (vertices?.Length ?? -1)
            && topology.TriangleCount == (indices?.Length ?? -1) / 3)
        {
            flags |= MeshAttributeFlags.Topology;
            flags |= MeshAttributeFlags.Adjacency;
        }

        return new MeshAttributeSet(flags);
    }

    public override string ToString() => Flags.ToString();

    private static bool AreValidIndices(IReadOnlyList<int> indices, int vertexCount) =>
        indices.All(index => index >= 0 && index < vertexCount);

    private static bool AreFinite(IReadOnlyList<Vector3> values) =>
        values.All(static value =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z));

    private static bool AreFinite(IReadOnlyList<Vector2> values) =>
        values.All(static value => float.IsFinite(value.X) && float.IsFinite(value.Y));

    private static bool AreFinite(IReadOnlyList<Vector4> values) =>
        values.All(static value =>
            float.IsFinite(value.X)
            && float.IsFinite(value.Y)
            && float.IsFinite(value.Z)
            && float.IsFinite(value.W));
}
