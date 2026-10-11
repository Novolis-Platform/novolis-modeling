namespace Novolis.Modeling;

/// <summary>Stable, renderer-neutral metadata emitted with an evaluated mesh.</summary>
public sealed record MeshEvaluationMetadata
{
    public static MeshEvaluationMetadata Empty { get; } = new();

    public string ContractVersion { get; init; } = MeshContractVersions.Contract;

    public string MesherVersion { get; init; } = MeshContractVersions.Mesher;

    public string ModifierVersion { get; init; } = MeshContractVersions.Modifier;

    public string GeometryHash { get; init; } = string.Empty;

    public string SemanticHash { get; init; } = string.Empty;

    public MeshCacheKey CacheKey { get; init; }

    public MeshLodLevel Lod { get; init; } = MeshLodLevel.Base;

    public MeshQualityProfile Quality { get; init; } = MeshQualityProfile.Default;
}
