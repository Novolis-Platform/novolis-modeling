using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Novolis.Modeling;

/// <summary>Creates canonical SHA-256 identities for geometry and evaluated attributes.</summary>
public static class MeshSemanticHasher
{
    public static string ComputeGeometry(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(MeshContractVersions.SemanticHash);
            writer.Write("geometry");
            WriteVectors(writer, vertices);
            WriteIndices(writer, indices);
        }

        return Hash(stream);
    }

    public static string Compute(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<int> indices,
        IReadOnlyList<Vector3>? normals = null,
        IReadOnlyList<Vector4>? tangents = null,
        IReadOnlyList<Vector2>? uvs = null,
        IReadOnlyList<int>? materialSlots = null,
        IReadOnlyList<float>? curvature = null,
        IReadOnlyList<float>? cavity = null)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(MeshContractVersions.SemanticHash);
            writer.Write("evaluated");
            WriteVectors(writer, vertices);
            WriteIndices(writer, indices);
            WriteOptionalVectors(writer, normals);
            WriteOptionalVector4s(writer, tangents);
            WriteOptionalVector2s(writer, uvs);
            WriteOptionalInts(writer, materialSlots);
            WriteOptionalFloats(writer, curvature);
            WriteOptionalFloats(writer, cavity);
        }

        return Hash(stream);
    }

    public static string Compute(EvaluatedMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        return Compute(
            mesh.Vertices,
            mesh.Indices,
            mesh.Normals,
            mesh.Tangents,
            mesh.UVs,
            mesh.MaterialSlots,
            mesh.Curvature,
            mesh.Cavity);
    }

    private static void WriteVectors(BinaryWriter writer, IReadOnlyList<Vector3> values)
    {
        writer.Write(values.Count);
        foreach (var value in values)
        {
            WriteFloat(writer, value.X);
            WriteFloat(writer, value.Y);
            WriteFloat(writer, value.Z);
        }
    }

    private static void WriteIndices(BinaryWriter writer, IReadOnlyList<int> values)
    {
        writer.Write(values.Count);
        foreach (var value in values)
            writer.Write(value);
    }

    private static void WriteOptionalVectors(BinaryWriter writer, IReadOnlyList<Vector3>? values)
    {
        writer.Write(values is not null);
        if (values is not null)
            WriteVectors(writer, values);
    }

    private static void WriteOptionalVector4s(BinaryWriter writer, IReadOnlyList<Vector4>? values)
    {
        writer.Write(values is not null);
        if (values is null)
            return;

        writer.Write(values.Count);
        foreach (var value in values)
        {
            WriteFloat(writer, value.X);
            WriteFloat(writer, value.Y);
            WriteFloat(writer, value.Z);
            WriteFloat(writer, value.W);
        }
    }

    private static void WriteOptionalVector2s(BinaryWriter writer, IReadOnlyList<Vector2>? values)
    {
        writer.Write(values is not null);
        if (values is null)
            return;

        writer.Write(values.Count);
        foreach (var value in values)
        {
            WriteFloat(writer, value.X);
            WriteFloat(writer, value.Y);
        }
    }

    private static void WriteOptionalInts(BinaryWriter writer, IReadOnlyList<int>? values)
    {
        writer.Write(values is not null);
        if (values is null)
            return;

        WriteIndices(writer, values);
    }

    private static void WriteOptionalFloats(BinaryWriter writer, IReadOnlyList<float>? values)
    {
        writer.Write(values is not null);
        if (values is null)
            return;

        writer.Write(values.Count);
        foreach (var value in values)
            WriteFloat(writer, value);
    }

    private static void WriteFloat(BinaryWriter writer, float value) =>
        writer.Write(BitConverter.SingleToInt32Bits(value));

    private static string Hash(MemoryStream stream) =>
        Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0, checked((int)stream.Length))))
            .ToLowerInvariant();
}
