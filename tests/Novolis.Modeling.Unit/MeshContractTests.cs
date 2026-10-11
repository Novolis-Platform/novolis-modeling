using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Modeling;

namespace Novolis.Modeling.Unit;

public sealed class MeshContractTests
{
    [Test]
    public async Task EvaluatedMesh_EmitsDeterministicAttributesAndMetadata()
    {
        var editable = PrimitiveMesher.Box(2f, 1f, 3f);
        var quality = new MeshQualityProfile
        {
            GenerateCurvature = true,
            GenerateCavity = true,
        };
        var slots = Enumerable.Repeat(2, editable.TriangleCount).ToArray();
        var first = EvaluatedMesh.FromEditable(
            Guid.NewGuid(),
            editable,
            Matrix4x4.Identity,
            quality,
            slots);
        var second = EvaluatedMesh.FromEditable(
            Guid.NewGuid(),
            editable,
            Matrix4x4.Identity,
            quality,
            slots);

        await Assert.That(first.Normals).IsNotNull();
        await Assert.That(first.Normals!.Length).IsEqualTo(first.Vertices.Length);
        await Assert.That(first.Tangents).IsNotNull();
        await Assert.That(first.UVs).IsNotNull();
        await Assert.That(first.MaterialSlots).IsNotNull();
        await Assert.That(first.Curvature).IsNotNull();
        await Assert.That(first.Cavity).IsNotNull();
        await Assert.That(first.Topology).IsNotNull();
        await Assert.That(first.Bounds.Size).IsEqualTo(new Vector3(2f, 1f, 3f));
        await Assert.That(first.Readiness.IsGeometryReady).IsTrue();
        await Assert.That(first.Readiness.IsAppearanceReady).IsTrue();
        await Assert.That(first.SemanticHash).IsEqualTo(second.SemanticHash);
        await Assert.That(first.GeometryHash).IsEqualTo(second.GeometryHash);
        await Assert.That(first.CacheKey).IsEqualTo(second.CacheKey);
        await Assert.That(first.CacheKey).IsEqualTo(first.Metadata.CacheKey.Value);
    }

    [Test]
    public async Task MeshAttributes_AreDeterministicAndTopologyIsSorted()
    {
        var mesh = PrimitiveMesher.Box(1f, 1f, 1f);
        var firstNormals = MeshAttributeGenerator.GenerateNormals(mesh.Vertices, mesh.Indices);
        var secondNormals = MeshAttributeGenerator.GenerateNormals(mesh.Vertices, mesh.Indices);
        var firstUvs = MeshAttributeGenerator.GeneratePlanarUvs(mesh.Vertices);
        var secondUvs = MeshAttributeGenerator.GeneratePlanarUvs(mesh.Vertices);
        var topology = MeshTopology.Build(mesh.VertexCount, mesh.Indices);

        await Assert.That(firstNormals.SequenceEqual(secondNormals)).IsTrue();
        await Assert.That(firstUvs.SequenceEqual(secondUvs)).IsTrue();
        await Assert.That(topology.Edges.SequenceEqual(topology.Edges.OrderBy(static edge => edge))).IsTrue();
        await Assert.That(topology.BoundaryEdges.Count).IsEqualTo(0);
        await Assert.That(topology.IsManifold).IsTrue();
        await Assert.That(
            topology.Adjacency.VertexNeighbors[0].SequenceEqual(
                topology.Adjacency.VertexNeighbors[0].OrderBy(static value => value))).IsTrue();
    }

    [Test]
    public async Task QualityProfile_CanOmitAppearanceAndTopologyTruthfully()
    {
        var mesh = EvaluatedMesh.FromEditable(
            Guid.NewGuid(),
            PrimitiveMesher.Box(1f, 1f, 1f),
            Matrix4x4.Identity,
            new MeshQualityProfile
            {
                GenerateNormals = false,
                GenerateTangents = false,
                GenerateUvs = false,
                BuildTopology = false,
                BuildAdjacency = false,
            });

        await Assert.That(mesh.Normals).IsNull();
        await Assert.That(mesh.UVs).IsNull();
        await Assert.That(mesh.Topology).IsNull();
        await Assert.That(mesh.Readiness.IsGeometryReady).IsTrue();
        await Assert.That(mesh.Readiness.Appearance).IsEqualTo(MeshReadinessState.Unavailable);

        var validation = MeshContractValidator.ValidateAppearanceOnly(mesh);
        await Assert.That(validation.IsValid).IsFalse();
        await Assert.That(validation.Readiness.IsGeometryReady).IsTrue();
        await Assert.That(validation.Readiness.IsAppearanceReady).IsFalse();
        await Assert.That(
            (validation.MissingAttributes & MeshAttributeFlags.Normals) == MeshAttributeFlags.Normals)
            .IsTrue();
        await Assert.That(
            (validation.MissingAttributes & MeshAttributeFlags.UV0) == MeshAttributeFlags.UV0)
            .IsTrue();
    }

    [Test]
    public async Task AppearanceOnlyContract_CannotClaimGeometry()
    {
        var operation = MeshOperationContract.AppearanceOnly("material")
            with { ClaimsGeometryChange = true };
        var available = new MeshAttributeSet(
            MeshAttributeSet.GeometryRequired | MeshAttributeSet.AppearanceDefaults);
        var validation = MeshContractValidator.Validate(operation, available);

        await Assert.That(validation.IsValid).IsFalse();
        await Assert.That(validation.ClaimsGeometryChange).IsTrue();
        await Assert.That(validation.Errors.Any(error => error.Contains("geometry change", StringComparison.OrdinalIgnoreCase)))
            .IsTrue();
    }

    [Test]
    public async Task LodAndSemanticIdentity_ChangeOnlyWhenQualityChanges()
    {
        var sphere = PrimitiveMesher.Sphere(1f, 12);
        var baseMesh = EvaluatedMesh.FromEditable(
            Guid.NewGuid(),
            sphere,
            Matrix4x4.Identity);
        var lodMesh = EvaluatedMesh.FromEditable(
            Guid.NewGuid(),
            sphere,
            Matrix4x4.Identity,
            new MeshQualityProfile
            {
                Lod = MeshLodLevel.Create(1, 12),
            });

        await Assert.That(lodMesh.Lod.Level).IsEqualTo(1);
        await Assert.That(lodMesh.Indices.Length / 3).IsLessThanOrEqualTo(12);
        await Assert.That(lodMesh.GeometryHash).IsNotEqualTo(baseMesh.GeometryHash);
        await Assert.That(lodMesh.CacheKey).IsNotEqualTo(baseMesh.CacheKey);
    }

    [Test]
    public async Task SceneEvaluator_UsesConfiguredMeshQuality()
    {
        var evaluator = new SceneEvaluator
        {
            MeshOptions = new MeshEvaluationOptions
            {
                Quality = new MeshQualityProfile
                {
                    GenerateNormals = false,
                    GenerateTangents = false,
                    GenerateUvs = false,
                },
            },
        };
        evaluator.Bind(SceneDocument.CreatePrimitiveStage("Quality"));

        await Assert.That(evaluator.Cache.EvaluatedMeshes).IsNotEmpty();
        await Assert.That(evaluator.Cache.EvaluatedMeshes.All(mesh => mesh.Normals is null)).IsTrue();
        await Assert.That(evaluator.Cache.EvaluatedMeshes.All(mesh => mesh.UVs is null)).IsTrue();
    }

    [Test]
    public async Task Look_changes_reuse_the_evaluated_mesh_cache()
    {
        var evaluator = new SceneEvaluator();
        evaluator.Bind(SceneDocument.CreatePrimitiveStage("Cache"));
        var first = evaluator.Cache;

        evaluator.InvalidateLook();
        var second = evaluator.Cache;

        await Assert.That(second.EvaluatedMeshes).IsSameReferenceAs(first.EvaluatedMeshes);
        await Assert.That(second.MeshGeneration).IsEqualTo(first.MeshGeneration);
        await Assert.That(second.LookGeneration).IsGreaterThan(first.LookGeneration);
    }
}
