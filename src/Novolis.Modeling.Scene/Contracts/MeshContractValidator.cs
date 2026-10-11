namespace Novolis.Modeling;

/// <summary>Validates mesh operation claims against data that is actually present.</summary>
public static class MeshContractValidator
{
    public static MeshContractValidation Validate(
        MeshOperationContract operation,
        EvaluatedMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(mesh);
        return Validate(operation, MeshAttributeSet.From(mesh));
    }

    public static MeshContractValidation Validate(
        EvaluatedMesh mesh,
        MeshOperationContract operation) =>
        Validate(operation, mesh);

    public static MeshContractValidation Validate(
        MeshOperationContract operation,
        MeshAttributeSet available)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var missing = available.Missing(operation.RequiredAttributes);
        var errors = new List<string>();
        if (missing != MeshAttributeFlags.None)
        {
            errors.Add(
                $"Operation '{operation.Name}' is missing mesh attributes: {missing}.");
        }

        if (operation.IsAppearanceOnly && operation.ClaimsGeometryChange)
        {
            errors.Add(
                $"Appearance-only operation '{operation.Name}' cannot claim a geometry change.");
        }

        if (operation.Kind == MeshOperationKind.Geometry && !operation.ClaimsGeometryChange)
        {
            errors.Add(
                $"Geometry operation '{operation.Name}' must declare its geometry change.");
        }

        var appearanceRequirements = operation.RequiredAttributes & MeshAttributeSet.AppearanceAvailable;
        var readiness = MeshReadiness.From(
            available,
            appearanceRequirements == MeshAttributeFlags.None
                ? MeshAttributeSet.AppearanceDefaults
                : appearanceRequirements);

        return new MeshContractValidation
        {
            IsValid = errors.Count == 0,
            Operation = operation,
            AvailableAttributes = available,
            Readiness = readiness,
            MissingAttributes = missing,
            Errors = errors,
        };
    }

    public static MeshContractValidation ValidateAppearanceOnly(
        EvaluatedMesh mesh,
        MeshAttributeFlags requiredAppearance = MeshAttributeSet.AppearanceDefaults) =>
        Validate(
            MeshOperationContract.AppearanceOnly("appearance-only", requiredAppearance),
            mesh);

    public static MeshContractValidation ValidateAppearanceOnly(
        MeshAttributeSet available,
        MeshAttributeFlags requiredAppearance = MeshAttributeSet.AppearanceDefaults) =>
        Validate(
            MeshOperationContract.AppearanceOnly("appearance-only", requiredAppearance),
            available);
}
