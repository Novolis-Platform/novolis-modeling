namespace Novolis.Modeling;

/// <summary>Evaluation inputs shared by mesh stacks and direct editable-mesh conversion.</summary>
public sealed record MeshEvaluationOptions
{
    public static MeshEvaluationOptions Default { get; } = new();

    public MeshQualityProfile Quality { get; init; } = MeshQualityProfile.Default;

    public string StableKey => Quality.StableKey;
}
