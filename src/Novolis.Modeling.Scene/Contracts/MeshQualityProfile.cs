namespace Novolis.Modeling;

/// <summary>Deterministic attribute, topology, and LOD policy for mesh evaluation.</summary>
public sealed record MeshQualityProfile
{
    public static MeshQualityProfile Default { get; } = new();

    public string Name { get; init; } = "default";

    public MeshLodLevel Lod { get; init; } = MeshLodLevel.Base;

    public bool GenerateNormals { get; init; } = true;

    public bool GenerateTangents { get; init; } = true;

    public bool GenerateUvs { get; init; } = true;

    public bool GenerateUVs => GenerateUvs;

    public bool GenerateCurvature { get; init; }

    public bool GenerateCavity { get; init; }

    public bool BuildTopology { get; init; } = true;

    public bool BuildAdjacency { get; init; } = true;

    public bool IncludeMaterialSlots { get; init; } = true;

    public MeshAttributeFlags RequestedAttributes
    {
        get
        {
            var attributes = MeshAttributeFlags.Positions
                             | MeshAttributeFlags.Indices
                             | MeshAttributeFlags.Bounds;
            if (GenerateNormals)
                attributes |= MeshAttributeFlags.Normals;
            if (GenerateTangents && GenerateUvs)
                attributes |= MeshAttributeFlags.Tangents;
            if (GenerateUvs)
                attributes |= MeshAttributeFlags.UV0;
            if (GenerateCurvature)
                attributes |= MeshAttributeFlags.Curvature;
            if (GenerateCavity)
                attributes |= MeshAttributeFlags.Cavity;
            if (BuildTopology && BuildAdjacency)
                attributes |= MeshAttributeFlags.Topology | MeshAttributeFlags.Adjacency;
            if (IncludeMaterialSlots)
                attributes |= MeshAttributeFlags.MaterialSlots;
            return attributes;
        }
    }

    public string StableKey =>
        string.Join(
            ';',
            $"name:{Name}",
            Lod.StableKey,
            $"normals:{GenerateNormals}",
            $"tangents:{GenerateTangents}",
            $"uvs:{GenerateUvs}",
            $"curvature:{GenerateCurvature}",
            $"cavity:{GenerateCavity}",
            $"topology:{BuildTopology}",
            $"adjacency:{BuildAdjacency}",
            $"materials:{IncludeMaterialSlots}");
}
