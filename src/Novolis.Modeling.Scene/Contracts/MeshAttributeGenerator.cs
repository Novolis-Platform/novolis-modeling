using System.Numerics;

namespace Novolis.Modeling;

/// <summary>Deterministic CPU generation of renderer-neutral mesh attributes.</summary>
public static class MeshAttributeGenerator
{
    public static Vector3[] GenerateNormals(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices)
    {
        ValidateMesh(vertices, indices);
        var normals = new Vector3[vertices.Count];
        for (var triangle = 0; triangle < indices.Count / 3; triangle++)
        {
            var offset = triangle * 3;
            var a = vertices[indices[offset]];
            var b = vertices[indices[offset + 1]];
            var c = vertices[indices[offset + 2]];
            var face = Vector3.Cross(b - a, c - a);
            if (!IsFinite(face) || face.LengthSquared() <= 1e-20f)
                continue;

            normals[indices[offset]] += face;
            normals[indices[offset + 1]] += face;
            normals[indices[offset + 2]] += face;
        }

        for (var i = 0; i < normals.Length; i++)
            normals[i] = NormalizeOrDefault(normals[i], Vector3.UnitY);
        return normals;
    }

    public static Vector2[] GeneratePlanarUvs(IReadOnlyList<Vector3> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        var uvs = new Vector2[vertices.Count];
        if (vertices.Count == 0)
            return uvs;

        var bounds = MeshBounds.FromVertices(vertices);
        var size = bounds.Size;
        var (uAxis, vAxis) = SelectProjection(size);
        var uExtent = Component(size, uAxis);
        var vExtent = Component(size, vAxis);

        for (var i = 0; i < vertices.Count; i++)
        {
            var u = NormalizeComponent(Component(vertices[i], uAxis), Component(bounds.Min, uAxis), uExtent);
            var v = NormalizeComponent(Component(vertices[i], vAxis), Component(bounds.Min, vAxis), vExtent);
            uvs[i] = new Vector2(u, v);
        }

        return uvs;
    }

    public static Vector2[] GenerateUvs(IReadOnlyList<Vector3> vertices) =>
        GeneratePlanarUvs(vertices);

    public static Vector2[] GenerateUVs(IReadOnlyList<Vector3> vertices) =>
        GeneratePlanarUvs(vertices);

    public static Vector2[] GenerateUvs(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        ValidateMesh(vertices, indices);
        return GeneratePlanarUvs(vertices);
    }

    public static Vector2[] GenerateUVs(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices) =>
        GenerateUvs(vertices, indices);

    public static Vector4[] GenerateTangents(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices,
        IReadOnlyList<Vector3> normals,
        IReadOnlyList<Vector2> uvs)
    {
        ValidateMesh(vertices, indices);
        ArgumentNullException.ThrowIfNull(normals);
        ArgumentNullException.ThrowIfNull(uvs);
        if (normals.Count != vertices.Count)
            throw new ArgumentException("Normal count must match vertex count.", nameof(normals));
        if (uvs.Count != vertices.Count)
            throw new ArgumentException("UV count must match vertex count.", nameof(uvs));

        var tangentSums = new Vector3[vertices.Count];
        var bitangentSums = new Vector3[vertices.Count];
        for (var triangle = 0; triangle < indices.Count / 3; triangle++)
        {
            var offset = triangle * 3;
            var i0 = indices[offset];
            var i1 = indices[offset + 1];
            var i2 = indices[offset + 2];
            var edge1 = vertices[i1] - vertices[i0];
            var edge2 = vertices[i2] - vertices[i0];
            var uv1 = uvs[i1] - uvs[i0];
            var uv2 = uvs[i2] - uvs[i0];
            var denominator = uv1.X * uv2.Y - uv2.X * uv1.Y;
            if (!float.IsFinite(denominator) || MathF.Abs(denominator) <= 1e-20f)
                continue;

            var reciprocal = 1f / denominator;
            var tangent = (edge1 * uv2.Y - edge2 * uv1.Y) * reciprocal;
            var bitangent = (edge2 * uv1.X - edge1 * uv2.X) * reciprocal;
            if (!IsFinite(tangent) || !IsFinite(bitangent))
                continue;

            tangentSums[i0] += tangent;
            tangentSums[i1] += tangent;
            tangentSums[i2] += tangent;
            bitangentSums[i0] += bitangent;
            bitangentSums[i1] += bitangent;
            bitangentSums[i2] += bitangent;
        }

        var tangents = new Vector4[vertices.Count];
        for (var i = 0; i < tangents.Length; i++)
        {
            var normal = NormalizeOrDefault(normals[i], Vector3.UnitY);
            var tangent = tangentSums[i] - normal * Vector3.Dot(normal, tangentSums[i]);
            tangent = NormalizeOrDefault(tangent, Perpendicular(normal));
            var handedness = Vector3.Dot(Vector3.Cross(normal, tangent), bitangentSums[i]) < 0f
                ? -1f
                : 1f;
            tangents[i] = new Vector4(tangent, handedness);
        }

        return tangents;
    }

    public static Vector4[] GenerateTangents(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices)
    {
        var normals = GenerateNormals(vertices, indices);
        var uvs = GeneratePlanarUvs(vertices);
        return GenerateTangents(vertices, indices, normals, uvs);
    }

    public static float[] GenerateCurvature(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices,
        IReadOnlyList<Vector3>? normals = null)
    {
        ValidateMesh(vertices, indices);
        var vertexNormals = normals ?? GenerateNormals(vertices, indices);
        if (vertexNormals.Count != vertices.Count)
            throw new ArgumentException("Normal count must match vertex count.", nameof(normals));

        var sums = new float[vertices.Count];
        var counts = new int[vertices.Count];
        for (var triangle = 0; triangle < indices.Count / 3; triangle++)
        {
            var offset = triangle * 3;
            var i0 = indices[offset];
            var i1 = indices[offset + 1];
            var i2 = indices[offset + 2];
            var face = Vector3.Cross(vertices[i1] - vertices[i0], vertices[i2] - vertices[i0]);
            if (!IsFinite(face) || face.LengthSquared() <= 1e-20f)
                continue;

            face = Vector3.Normalize(face);
            AddCurvature(sums, counts, i0, vertexNormals[i0], face);
            AddCurvature(sums, counts, i1, vertexNormals[i1], face);
            AddCurvature(sums, counts, i2, vertexNormals[i2], face);
        }

        var curvature = new float[vertices.Count];
        for (var i = 0; i < curvature.Length; i++)
            curvature[i] = counts[i] == 0
                ? 0f
                : System.Math.Clamp(sums[i] / counts[i], 0f, 1f);
        return curvature;
    }

    public static float[] GenerateCavity(IReadOnlyList<float> curvature)
    {
        ArgumentNullException.ThrowIfNull(curvature);
        var cavity = new float[curvature.Count];
        for (var i = 0; i < cavity.Length; i++)
            cavity[i] = float.IsFinite(curvature[i])
                ? System.Math.Clamp(curvature[i], 0f, 1f)
                : 0f;
        return cavity;
    }

    private static void AddCurvature(
        float[] sums,
        int[] counts,
        int vertex,
        Vector3 vertexNormal,
        Vector3 faceNormal)
    {
        var normal = NormalizeOrDefault(vertexNormal, Vector3.UnitY);
        sums[vertex] +=
            (1f - System.Math.Clamp(Vector3.Dot(normal, faceNormal), -1f, 1f)) * 0.5f;
        counts[vertex]++;
    }

    private static (int U, int V) SelectProjection(Vector3 size)
    {
        // The thinnest axis is treated as the surface normal. Ties resolve XZ, XY, then YZ.
        if (size.Y <= size.X && size.Y <= size.Z)
            return (0, 2);
        if (size.Z <= size.X && size.Z <= size.Y)
            return (0, 1);
        return (1, 2);
    }

    private static float NormalizeComponent(float value, float minimum, float extent) =>
        extent <= 1e-20f
            ? 0.5f
            : System.Math.Clamp((value - minimum) / extent, 0f, 1f);

    private static float Component(Vector3 value, int axis) =>
        axis switch
        {
            0 => value.X,
            1 => value.Y,
            _ => value.Z,
        };

    private static Vector3 Perpendicular(Vector3 normal)
    {
        var axis = MathF.Abs(normal.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;
        return NormalizeOrDefault(Vector3.Cross(axis, normal), Vector3.UnitZ);
    }

    private static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback)
    {
        if (!IsFinite(value) || value.LengthSquared() <= 1e-20f)
            return fallback;
        return Vector3.Normalize(value);
    }

    private static void ValidateMesh(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        if (indices.Count % 3 != 0)
            throw new ArgumentException("Index count must be a multiple of three.", nameof(indices));
        for (var i = 0; i < indices.Count; i++)
        {
            if (indices[i] < 0 || indices[i] >= vertices.Count)
                throw new ArgumentOutOfRangeException(nameof(indices), $"Index {indices[i]} is outside the vertex array.");
        }
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
