namespace Novolis.Modeling;

/// <summary>Canonical undirected edge identified by its sorted vertex indices.</summary>
public readonly record struct MeshEdge : IComparable<MeshEdge>
{
    public MeshEdge(int first, int second)
    {
        if (first < 0 || second < 0)
            throw new ArgumentOutOfRangeException(nameof(first), "Vertex indices must be non-negative.");

        A = System.Math.Min(first, second);
        B = System.Math.Max(first, second);
    }

    public int A { get; }

    public int B { get; }

    public int CompareTo(MeshEdge other)
    {
        var first = A.CompareTo(other.A);
        return first != 0 ? first : B.CompareTo(other.B);
    }
}
