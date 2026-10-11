namespace Novolis.Modeling;

/// <summary>Deterministic vertex and triangle adjacency for indexed triangle geometry.</summary>
public sealed class MeshAdjacency
{
    private MeshAdjacency(
        int vertexCount,
        int triangleCount,
        IReadOnlyList<MeshEdge> edges,
        IReadOnlyList<MeshEdge> boundaryEdges,
        IReadOnlyList<MeshEdge> nonManifoldEdges,
        IReadOnlyList<IReadOnlyList<int>> vertexNeighbors,
        IReadOnlyList<IReadOnlyList<int>> triangleNeighbors)
    {
        VertexCount = vertexCount;
        TriangleCount = triangleCount;
        Edges = edges;
        BoundaryEdges = boundaryEdges;
        NonManifoldEdges = nonManifoldEdges;
        VertexNeighbors = vertexNeighbors;
        TriangleNeighbors = triangleNeighbors;
    }

    public int VertexCount { get; }

    public int TriangleCount { get; }

    public IReadOnlyList<MeshEdge> Edges { get; }

    public IReadOnlyList<MeshEdge> BoundaryEdges { get; }

    public IReadOnlyList<MeshEdge> NonManifoldEdges { get; }

    public IReadOnlyList<IReadOnlyList<int>> VertexNeighbors { get; }

    public IReadOnlyList<IReadOnlyList<int>> TriangleNeighbors { get; }

    public bool IsClosed => BoundaryEdges.Count == 0 && NonManifoldEdges.Count == 0;

    public static MeshAdjacency Build(int vertexCount, IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        if (vertexCount < 0)
            throw new ArgumentOutOfRangeException(nameof(vertexCount));
        if (indices.Count % 3 != 0)
            throw new ArgumentException("Index count must be a multiple of three.", nameof(indices));

        var vertexNeighbors = Enumerable.Range(0, vertexCount)
            .Select(static _ => new HashSet<int>())
            .ToArray();
        var triangleNeighbors = Enumerable.Range(0, indices.Count / 3)
            .Select(static _ => new HashSet<int>())
            .ToArray();
        var edgeTriangles = new Dictionary<MeshEdge, List<int>>();

        for (var triangle = 0; triangle < indices.Count / 3; triangle++)
        {
            var offset = triangle * 3;
            var a = ValidateIndex(indices[offset], vertexCount);
            var b = ValidateIndex(indices[offset + 1], vertexCount);
            var c = ValidateIndex(indices[offset + 2], vertexCount);

            AddNeighbor(vertexNeighbors, a, b);
            AddNeighbor(vertexNeighbors, b, c);
            AddNeighbor(vertexNeighbors, c, a);
            AddEdge(edgeTriangles, new MeshEdge(a, b), triangle);
            AddEdge(edgeTriangles, new MeshEdge(b, c), triangle);
            AddEdge(edgeTriangles, new MeshEdge(c, a), triangle);
        }

        foreach (var triangles in edgeTriangles.Values)
        {
            var ordered = triangles.Distinct().OrderBy(static triangle => triangle).ToArray();
            for (var i = 0; i < ordered.Length; i++)
            {
                for (var j = i + 1; j < ordered.Length; j++)
                {
                    triangleNeighbors[ordered[i]].Add(ordered[j]);
                    triangleNeighbors[ordered[j]].Add(ordered[i]);
                }
            }
        }

        var edges = edgeTriangles.Keys.OrderBy(static edge => edge).ToArray();
        var boundaryEdges = edgeTriangles
            .Where(static pair => pair.Value.Distinct().Count() == 1)
            .Select(static pair => pair.Key)
            .OrderBy(static edge => edge)
            .ToArray();
        var nonManifoldEdges = edgeTriangles
            .Where(static pair => pair.Value.Distinct().Count() > 2)
            .Select(static pair => pair.Key)
            .OrderBy(static edge => edge)
            .ToArray();

        return new MeshAdjacency(
            vertexCount,
            indices.Count / 3,
            edges,
            boundaryEdges,
            nonManifoldEdges,
            vertexNeighbors
                .Select(static neighbors => (IReadOnlyList<int>)neighbors.OrderBy(static value => value).ToArray())
                .ToArray(),
            triangleNeighbors
                .Select(static neighbors => (IReadOnlyList<int>)neighbors.OrderBy(static value => value).ToArray())
                .ToArray());
    }

    public static MeshAdjacency Empty(int vertexCount = 0)
    {
        if (vertexCount < 0)
            throw new ArgumentOutOfRangeException(nameof(vertexCount));

        return Build(vertexCount, Array.Empty<int>());
    }

    private static void AddNeighbor(HashSet<int>[] neighbors, int first, int second)
    {
        if (first == second)
            return;
        neighbors[first].Add(second);
        neighbors[second].Add(first);
    }

    private static void AddEdge(Dictionary<MeshEdge, List<int>> edges, MeshEdge edge, int triangle)
    {
        if (!edges.TryGetValue(edge, out var triangles))
            edges[edge] = triangles = [];
        triangles.Add(triangle);
    }

    private static int ValidateIndex(int index, int vertexCount)
    {
        if (index < 0 || index >= vertexCount)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is outside {vertexCount} vertices.");
        return index;
    }
}
