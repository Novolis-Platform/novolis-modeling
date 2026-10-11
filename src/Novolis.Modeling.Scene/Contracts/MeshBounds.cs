using System.Numerics;

namespace Novolis.Modeling;

/// <summary>Deterministic local-space axis-aligned bounds for an evaluated mesh.</summary>
public readonly record struct MeshBounds
{
    public MeshBounds(Vector3 min, Vector3 max, bool isEmpty = false)
    {
        if (!isEmpty && (!AreFinite(min) || !AreFinite(max)))
            throw new ArgumentException("Bounds must contain finite coordinates.");

        Min = min;
        Max = max;
        IsEmpty = isEmpty;
    }

    public Vector3 Min { get; }

    public Vector3 Max { get; }

    public bool IsEmpty { get; }

    public bool IsFinite =>
        AreFinite(Min) && AreFinite(Max);

    public Vector3 Center => IsEmpty ? Vector3.Zero : (Min + Max) * 0.5f;

    public Vector3 Size => IsEmpty ? Vector3.Zero : Max - Min;

    public float Radius => IsEmpty ? 0f : Size.Length() * 0.5f;

    public static MeshBounds Empty => new(Vector3.Zero, Vector3.Zero, isEmpty: true);

    public static MeshBounds FromVertices(IReadOnlyList<Vector3>? vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        if (vertices.Count == 0)
            return Empty;

        var min = vertices[0];
        var max = vertices[0];
        for (var i = 1; i < vertices.Count; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        return new MeshBounds(min, max);
    }

    public static MeshBounds Union(MeshBounds left, MeshBounds right)
    {
        if (left.IsEmpty)
            return right;
        if (right.IsEmpty)
            return left;
        return new MeshBounds(Vector3.Min(left.Min, right.Min), Vector3.Max(left.Max, right.Max));
    }

    public bool Contains(Vector3 point) =>
        !IsEmpty
        && point.X >= Min.X && point.X <= Max.X
        && point.Y >= Min.Y && point.Y <= Max.Y
        && point.Z >= Min.Z && point.Z <= Max.Z;

    private static bool AreFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
