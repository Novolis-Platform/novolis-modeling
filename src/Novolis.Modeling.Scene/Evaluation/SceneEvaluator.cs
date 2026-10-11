using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Modeling;

/// <summary>Staged evaluator with narrow invalidation.</summary>
public sealed class SceneEvaluator
{
    private SceneDocument? _document;
    private LookCache _cache = new();
    private int _meshGeneration;
    private int _lookGeneration;
    private int _builtMeshGeneration = -1;
    private int _builtLookGeneration = -1;
    private MeshEvaluationOptions _meshOptions = MeshEvaluationOptions.Default;
    private Dictionary<Guid, Matrix4x4> _worlds = [];
    private IReadOnlyList<EvaluatedNode> _meshNodes = [];
    private IReadOnlyList<EvaluatedMesh> _evaluatedMeshes = [];
    private IReadOnlyList<EvaluatedNode> _lights = [];
    private IReadOnlyList<EvaluatedNode> _cameras = [];
    private IReadOnlyList<EvaluatedNode> _materials = [];

    public LookCache Cache => EnsureBuilt();

    /// <summary>Advances when mesh topology / transforms that affect mesh eval change.</summary>
    public int MeshGeneration => _meshGeneration;

    /// <summary>Advances when lights/cameras/materials change.</summary>
    public int LookGeneration => _lookGeneration;

    /// <summary>Quality and attribute policy used for the next mesh evaluation.</summary>
    public MeshEvaluationOptions MeshOptions
    {
        get => _meshOptions;
        set
        {
            _meshOptions = value ?? throw new ArgumentNullException(nameof(value));
            InvalidateMesh();
        }
    }

    public void Bind(SceneDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        InvalidateAll();
    }

    public void InvalidateMesh() => _meshGeneration++;

    public void InvalidateLook() => _lookGeneration++;

    public void InvalidateAll()
    {
        _meshGeneration++;
        _lookGeneration++;
    }

    public void NotifyNodeChanged(SceneNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node is LightNode or CameraNode or MaterialNode)
            InvalidateLook();
        else
            InvalidateAll();
    }

    private LookCache EnsureBuilt()
    {
        if (_document is null)
            return _cache;

        var meshChanged = _builtMeshGeneration != _meshGeneration;
        var lookChanged = _builtLookGeneration != _lookGeneration;
        if (!meshChanged && !lookChanged)
            return _cache;

        if (meshChanged)
        {
            _worlds = BuildWorldMatrices(_document);
            _meshNodes = EvaluateNodes<MeshNode>(_document, _worlds);
            _evaluatedMeshes = MeshStackEvaluator.EvaluateDocument(
                _document,
                _worlds,
                _meshOptions);
        }

        if (meshChanged || lookChanged)
        {
            _lights = EvaluateNodes<LightNode>(_document, _worlds);
            _cameras = EvaluateNodes<CameraNode>(_document, _worlds);
            _materials = EvaluateNodes<MaterialNode>(_document, _worlds);
            _meshNodes ??= [];
        }

        _cache = new LookCache
        {
            Meshes = _meshNodes,
            EvaluatedMeshes = _evaluatedMeshes,
            Lights = _lights,
            Cameras = _cameras,
            Materials = _materials,
            MeshGeneration = _meshGeneration,
            LookGeneration = _lookGeneration,
        };
        _builtMeshGeneration = _meshGeneration;
        _builtLookGeneration = _lookGeneration;
        return _cache;
    }

    private static IReadOnlyList<EvaluatedNode> EvaluateNodes<TNode>(
        SceneDocument document,
        IReadOnlyDictionary<Guid, Matrix4x4> worlds)
        where TNode : SceneNode
    {
        var result = new List<EvaluatedNode>();
        foreach (var node in document.Nodes)
        {
            if (node is not TNode ||
                !node.Visible ||
                !worlds.TryGetValue(node.Id, out var world))
            {
                continue;
            }

            result.Add(
                new EvaluatedNode
                {
                    Source = node,
                    WorldMatrix = world,
                    WorldPosition = Vector3.Transform(Vector3.Zero, world),
                });
        }

        return result;
    }

    internal static Dictionary<Guid, Matrix4x4> BuildWorldMatrices(SceneDocument doc)
    {
        var local = doc.Nodes.ToDictionary(n => n.Id, n => n.Transform.ToMatrix());
        var world = new Dictionary<Guid, Matrix4x4>();
        var visiting = new HashSet<Guid>();

        Matrix4x4 Resolve(Guid id)
        {
            if (world.TryGetValue(id, out var cached))
                return cached;
            if (!visiting.Add(id))
                return Matrix4x4.Identity;

            var node = doc.Find(id);
            if (node is null)
            {
                visiting.Remove(id);
                return Matrix4x4.Identity;
            }

            var parent = node.ParentId is { } pid ? Resolve(pid) : Matrix4x4.Identity;
            var m = local[id] * parent;
            world[id] = m;
            visiting.Remove(id);
            return m;
        }

        foreach (var n in doc.Nodes)
            Resolve(n.Id);
        return world;
    }
}
