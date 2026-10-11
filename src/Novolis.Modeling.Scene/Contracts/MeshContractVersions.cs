namespace Novolis.Modeling;

/// <summary>Stable version labels used by modeling mesh metadata and cache records.</summary>
public static class MeshContractVersions
{
    /// <summary>Version of the renderer-neutral evaluated mesh contract.</summary>
    public const string Contract = "mesh-contract-v1";

    /// <summary>Version of deterministic attribute and primitive generation.</summary>
    public const string Mesher = "modeling-mesher-v1";

    /// <summary>Version of the modeling modifier stack evaluation.</summary>
    public const string Modifier = "modeling-modifier-stack-v1";

    /// <summary>Version of the canonical semantic hash encoding.</summary>
    public const string SemanticHash = "sha256-canonical-v1";
}
