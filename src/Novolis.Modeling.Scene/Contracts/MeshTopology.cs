namespace Novolis.Modeling;

/// <summary>Triangle topology plus deterministic adjacency information.</summary>
public sealed class MeshTopology
{
    private MeshTopology(int vertexCount, int triangleCount, MeshAdjacency adjacency)
    {
        VertexCount = vertexCount;
        TriangleCount = triangleCount;
        Adjacency = adjacency;
    }

    public int VertexCount { get; }

    public int TriangleCount { get; }

    public MeshAdjacency Adjacency { get; }

    public IReadOnlyList<MeshEdge> Edges => Adjacency.Edges;

    public IReadOnlyList<MeshEdge> BoundaryEdges => Adjacency.BoundaryEdges;

    public IReadOnlyList<MeshEdge> NonManifoldEdges => Adjacency.NonManifoldEdges;

    public bool IsClosed => Adjacency.IsClosed;

    public bool IsManifold => Adjacency.NonManifoldEdges.Count == 0;

    public static MeshTopology Build(int vertexCount, IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        var adjacency = MeshAdjacency.Build(vertexCount, indices);
        return new MeshTopology(vertexCount, indices.Count / 3, adjacency);
    }

    public static MeshTopology Empty(int vertexCount = 0) =>
        Build(vertexCount, Array.Empty<int>());
}
