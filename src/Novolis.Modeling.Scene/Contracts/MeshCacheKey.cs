namespace Novolis.Modeling;

/// <summary>Deterministic cache identity for a modeling mesh evaluation.</summary>
public readonly record struct MeshCacheKey
{
    public MeshCacheKey(
        string geometryHash,
        string qualityKey,
        MeshAttributeFlags attributes,
        MeshLodLevel lod)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(geometryHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(qualityKey);
        GeometryHash = geometryHash;
        QualityKey = qualityKey;
        Attributes = attributes;
        Lod = lod;
    }

    public string GeometryHash { get; }

    public string QualityKey { get; }

    public MeshAttributeFlags Attributes { get; }

    public MeshLodLevel Lod { get; }

    public string Value =>
        string.Join(
            '|',
            MeshContractVersions.Contract,
            MeshContractVersions.Mesher,
            MeshContractVersions.Modifier,
            GeometryHash,
            QualityKey,
            $"attributes:{(int)Attributes}",
            Lod.StableKey);

    public static MeshCacheKey Create(
        string geometryHash,
        MeshQualityProfile quality,
        MeshAttributeSet attributes)
    {
        ArgumentNullException.ThrowIfNull(quality);
        return new MeshCacheKey(
            geometryHash,
            quality.StableKey,
            attributes.Flags,
            quality.Lod);
    }

    public override string ToString() => Value;
}
