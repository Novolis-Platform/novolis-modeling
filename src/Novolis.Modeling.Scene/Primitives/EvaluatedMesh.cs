using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Modeling;

/// <summary>Evaluated triangle mesh ready for viewport / further ops.</summary>
public sealed class EvaluatedMesh
{
    public required Guid SourceId { get; init; }
    public required Vector3[] Vertices { get; init; }
    public required int[] Indices { get; init; }
    public required Matrix4x4 World { get; init; }

    /// <summary>Optional per-vertex unit normals.</summary>
    public Vector3[]? Normals { get; init; }

    /// <summary>Optional per-vertex tangent frames; W stores handedness.</summary>
    public Vector4[]? Tangents { get; init; }

    /// <summary>Optional per-vertex first UV channel.</summary>
    public Vector2[]? UVs { get; init; }

    /// <summary>Compatibility spelling for the first UV channel.</summary>
    public Vector2[]? Uvs
    {
        get => UVs;
        init => UVs = value;
    }

    /// <summary>Optional material slot per triangle.</summary>
    public int[]? MaterialSlots { get; init; }

    /// <summary>Compatibility alias for <see cref="MaterialSlots"/>.</summary>
    public int[]? MaterialSlotIndices
    {
        get => MaterialSlots;
        init => MaterialSlots = value;
    }

    /// <summary>Optional normalized per-vertex curvature estimate.</summary>
    public float[]? Curvature { get; init; }

    /// <summary>Optional normalized per-vertex cavity estimate.</summary>
    public float[]? Cavity { get; init; }

    /// <summary>Attributes declared by the evaluation that produced this mesh.</summary>
    public MeshAttributeSet Attributes { get; init; }

    /// <summary>Compatibility alias for <see cref="Attributes"/>.</summary>
    public MeshAttributeSet AttributeSet => Attributes;

    /// <summary>Compatibility alias for <see cref="Attributes"/>.</summary>
    public MeshAttributeSet MeshAttributes => Attributes;

    /// <summary>Optional indexed topology and adjacency.</summary>
    public MeshTopology? Topology { get; init; }

    /// <summary>Adjacency alias for the topology contract.</summary>
    public MeshAdjacency? Adjacency => Topology?.Adjacency;

    /// <summary>Local-space bounds for the evaluated positions.</summary>
    public MeshBounds Bounds { get; init; } = MeshBounds.Empty;

    /// <summary>Selected level of detail.</summary>
    public MeshLodLevel Lod { get; init; } = MeshLodLevel.Base;

    /// <summary>Compatibility alias for <see cref="Lod"/>.</summary>
    public MeshLodLevel LOD => Lod;

    /// <summary>Quality policy used to produce this mesh.</summary>
    public MeshQualityProfile Quality { get; init; } = MeshQualityProfile.Default;

    /// <summary>Compatibility alias for <see cref="Quality"/>.</summary>
    public MeshQualityProfile QualityProfile => Quality;

    /// <summary>Independent geometry and appearance readiness.</summary>
    public MeshReadiness Readiness { get; init; } = MeshReadiness.Empty;

    /// <summary>Deterministic evaluation metadata.</summary>
    public MeshEvaluationMetadata Metadata { get; init; } = MeshEvaluationMetadata.Empty;

    /// <summary>Full semantic hash, including present optional attributes.</summary>
    public string SemanticHash { get; init; } = string.Empty;

    /// <summary>Geometry-only hash used as the stable cache input.</summary>
    public string GeometryHash { get; init; } = string.Empty;

    /// <summary>Deterministic cache identity for this evaluation.</summary>
    public string CacheKey { get; init; } = string.Empty;

    public EditableMesh ToEditableMesh() => new(Vertices, Indices);

    public static EvaluatedMesh FromEditable(
        Guid sourceId,
        EditableMesh mesh,
        Matrix4x4 world,
        MeshEvaluationOptions? options = null,
        IReadOnlyList<int>? materialSlots = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        options ??= MeshEvaluationOptions.Default;
        var quality = options.Quality ?? MeshQualityProfile.Default;
        var working = ApplyLod(mesh, quality.Lod);
        var vertices = working.Vertices.ToArray();
        var indices = working.Indices.ToArray();
        var normals = quality.GenerateNormals
            ? MeshAttributeGenerator.GenerateNormals(vertices, indices)
            : null;
        var uvs = quality.GenerateUvs
            ? MeshAttributeGenerator.GeneratePlanarUvs(vertices)
            : null;
        var tangents = quality.GenerateTangents && normals is not null && uvs is not null
            ? MeshAttributeGenerator.GenerateTangents(vertices, indices, normals, uvs)
            : null;
        var curvature = quality.GenerateCurvature
            ? MeshAttributeGenerator.GenerateCurvature(vertices, indices, normals)
            : null;
        var cavity = quality.GenerateCavity
            ? MeshAttributeGenerator.GenerateCavity(curvature ?? MeshAttributeGenerator.GenerateCurvature(vertices, indices, normals))
            : null;
        var slots = quality.IncludeMaterialSlots && materialSlots is not null
            ? CopyMaterialSlots(materialSlots, indices.Length / 3)
            : null;
        var bounds = MeshBounds.FromVertices(vertices);
        var topology = quality.BuildTopology && quality.BuildAdjacency
            ? MeshTopology.Build(vertices.Length, indices)
            : null;
        var attributes = BuildAttributes(
            vertices,
            indices,
            normals,
            tangents,
            uvs,
            slots,
            curvature,
            cavity,
            bounds,
            topology);
        var readiness = MeshReadiness.From(attributes);
        var geometryHash = MeshSemanticHasher.ComputeGeometry(vertices, indices);
        var semanticHash = MeshSemanticHasher.Compute(
            vertices,
            indices,
            normals,
            tangents,
            uvs,
            slots,
            curvature,
            cavity);
        var cacheKey = MeshCacheKey.Create(geometryHash, quality, attributes);
        var metadata = new MeshEvaluationMetadata
        {
            GeometryHash = geometryHash,
            SemanticHash = semanticHash,
            CacheKey = cacheKey,
            Lod = quality.Lod,
            Quality = quality,
        };

        return new EvaluatedMesh
        {
            SourceId = sourceId,
            Vertices = vertices,
            Indices = indices,
            World = world,
            Normals = normals,
            Tangents = tangents,
            UVs = uvs,
            MaterialSlots = slots,
            Curvature = curvature,
            Cavity = cavity,
            Attributes = attributes,
            Topology = topology,
            Bounds = bounds,
            Lod = quality.Lod,
            Quality = quality,
            Readiness = readiness,
            Metadata = metadata,
            SemanticHash = semanticHash,
            GeometryHash = geometryHash,
            CacheKey = cacheKey.Value,
        };
    }

    public static EvaluatedMesh FromEditable(
        Guid sourceId,
        EditableMesh mesh,
        Matrix4x4 world,
        MeshQualityProfile quality,
        IReadOnlyList<int>? materialSlots = null) =>
        FromEditable(
            sourceId,
            mesh,
            world,
            new MeshEvaluationOptions { Quality = quality },
            materialSlots);

    private static EditableMesh ApplyLod(EditableMesh mesh, MeshLodLevel lod)
    {
        if (lod.TargetTriangleCount <= 0 || mesh.TriangleCount <= lod.TargetTriangleCount)
            return mesh;

        var reduced = Novolis.Math.Geometry.MeshLod.Decimate(
            mesh.ToTriangleMesh(),
            lod.TargetTriangleCount);
        return EditableMesh.FromTriangleMesh(reduced);
    }

    private static int[]? CopyMaterialSlots(IReadOnlyList<int> materialSlots, int triangleCount)
    {
        ArgumentNullException.ThrowIfNull(materialSlots);
        if (materialSlots.Count != triangleCount)
            return null;

        var copy = materialSlots.ToArray();
        if (copy.Any(static slot => slot < 0))
            throw new ArgumentException("Material slots must be non-negative.", nameof(materialSlots));
        return copy;
    }

    private static MeshAttributeSet BuildAttributes(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices,
        IReadOnlyList<Vector3>? normals,
        IReadOnlyList<Vector4>? tangents,
        IReadOnlyList<Vector2>? uvs,
        IReadOnlyList<int>? materialSlots,
        IReadOnlyList<float>? curvature,
        IReadOnlyList<float>? cavity,
        MeshBounds bounds,
        MeshTopology? topology)
    {
        var flags = MeshAttributeFlags.Positions | MeshAttributeFlags.Indices;
        if (!bounds.IsEmpty)
            flags |= MeshAttributeFlags.Bounds;
        if (normals is not null && normals.Count > 0 && normals.Count == vertices.Count)
            flags |= MeshAttributeFlags.Normals;
        if (tangents is not null && tangents.Count > 0 && tangents.Count == vertices.Count)
            flags |= MeshAttributeFlags.Tangents;
        if (uvs is not null && uvs.Count > 0 && uvs.Count == vertices.Count)
            flags |= MeshAttributeFlags.UV0;
        if (materialSlots is not null && materialSlots.Count > 0 && materialSlots.Count == indices.Count / 3)
            flags |= MeshAttributeFlags.MaterialSlots;
        if (curvature is not null && curvature.Count > 0 && curvature.Count == vertices.Count)
            flags |= MeshAttributeFlags.Curvature;
        if (cavity is not null && cavity.Count > 0 && cavity.Count == vertices.Count)
            flags |= MeshAttributeFlags.Cavity;
        if (topology is not null)
            flags |= MeshAttributeFlags.Topology | MeshAttributeFlags.Adjacency;
        return new MeshAttributeSet(flags);
    }
}
