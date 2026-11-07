using Assimp;
using Novolis.Math.Geometry;
using AssimpVector = Assimp.Vector3D;

namespace Novolis.Modeling;

/// <summary>
/// Exports triangle meshes to FBX, glTF/GLB, and other Assimp-supported formats.
/// Pair of <see cref="AssimpMeshImporter"/>; native Assimp runtime required.
/// </summary>
public static class AssimpMeshExporter
{
    /// <summary>Common interchange extensions Assimp can typically write.</summary>
    public static readonly string[] CommonExtensions =
    [
        ".fbx", ".obj", ".gltf", ".glb", ".dae", ".stl", ".ply",
    ];

    /// <summary>Writes one mesh; format is inferred from <paramref name="path"/> extension.</summary>
    public static void Export(string path, EditableMesh mesh, string objectName = "mesh") =>
        Export(path, [(objectName, mesh)]);

    /// <summary>Writes one mesh; format is inferred from <paramref name="path"/> extension.</summary>
    public static void Export(string path, TriangleMesh mesh, string objectName = "mesh") =>
        Export(path, EditableMesh.FromTriangleMesh(mesh), objectName);

    /// <summary>Writes named parts as separate meshes under one root node.</summary>
    public static void Export(string path, IEnumerable<(string Name, EditableMesh Mesh)> parts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(parts);

        var formatId = ResolveFormatId(path)
            ?? throw new NotSupportedException(
                $"No Assimp export format for extension '{Path.GetExtension(path)}'.");

        var scene = BuildScene(parts);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var ctx = new AssimpContext();
        if (!ctx.ExportFile(scene, path, formatId))
            throw new InvalidOperationException($"Assimp failed to export '{path}' as '{formatId}'.");
    }

    /// <summary>True when Assimp lists an export format for this path/extension.</summary>
    public static bool IsSupportedExtension(string? pathOrExtension)
    {
        if (string.IsNullOrWhiteSpace(pathOrExtension))
            return false;
        try
        {
            return ResolveFormatId(
                pathOrExtension.Contains('.')
                    ? pathOrExtension
                    : "mesh" + (pathOrExtension.StartsWith('.') ? pathOrExtension : "." + pathOrExtension))
                is not null;
        }
        catch
        {
            return false;
        }
    }

    private static Scene BuildScene(IEnumerable<(string Name, EditableMesh Mesh)> parts)
    {
        var scene = new Scene
        {
            RootNode = new Node("Root"),
        };
        scene.Materials.Add(new Material { Name = "DefaultMaterial" });

        foreach (var (name, mesh) in parts)
        {
            ArgumentNullException.ThrowIfNull(mesh);
            if (mesh.VertexCount == 0 || mesh.TriangleCount == 0)
                continue;

            var assimpMesh = new Mesh(Sanitize(name), PrimitiveType.Triangle)
            {
                MaterialIndex = 0,
            };
            foreach (var v in mesh.Vertices)
                assimpMesh.Vertices.Add(new AssimpVector(v.X, v.Y, v.Z));

            var idx = mesh.Indices;
            for (var i = 0; i + 2 < idx.Count; i += 3)
            {
                var face = new Face();
                face.Indices.Add(idx[i]);
                face.Indices.Add(idx[i + 1]);
                face.Indices.Add(idx[i + 2]);
                assimpMesh.Faces.Add(face);
            }

            var meshIndex = scene.Meshes.Count;
            scene.Meshes.Add(assimpMesh);

            var child = new Node(Sanitize(name));
            child.MeshIndices.Add(meshIndex);
            scene.RootNode.Children.Add(child);
        }

        if (scene.MeshCount == 0)
            throw new InvalidOperationException("Assimp export requires at least one non-empty mesh.");

        return scene;
    }

    private static string? ResolveFormatId(string path)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext))
            return null;

        using var ctx = new AssimpContext();
        var formats = ctx.GetSupportedExportFormats() ?? [];
        var want = ext.TrimStart('.').ToLowerInvariant();

        ExportFormatDescription? Prefer(Func<ExportFormatDescription, bool> pred) =>
            formats.FirstOrDefault(pred);

        var fromCatalog = want switch
        {
            "glb" => Prefer(f => f.FileExtension.Equals("glb", StringComparison.OrdinalIgnoreCase)
                                 || f.FormatId.Contains("glb", StringComparison.OrdinalIgnoreCase))
                ?.FormatId,
            "gltf" => Prefer(f => f.FileExtension.Equals("gltf", StringComparison.OrdinalIgnoreCase)
                                  || f.FormatId.Contains("gltf", StringComparison.OrdinalIgnoreCase))
                ?.FormatId,
            "fbx" => Prefer(f => f.FormatId.Contains("fbx", StringComparison.OrdinalIgnoreCase))
                ?.FormatId,
            "dae" => Prefer(f => f.FileExtension.Equals("dae", StringComparison.OrdinalIgnoreCase)
                                 || f.FormatId.Contains("collada", StringComparison.OrdinalIgnoreCase))
                ?.FormatId,
            _ => Prefer(f => f.FileExtension.Equals(want, StringComparison.OrdinalIgnoreCase))
                ?.FormatId,
        };
        if (fromCatalog is not null)
            return fromCatalog;

        // Assimp builds often omit FBX export (SDK license). Try known ids before giving up.
        return want switch
        {
            "fbx" => FirstWorkingExportId(ctx, ["fbx", "fbxa", "fbxb"]),
            "glb" => FirstWorkingExportId(ctx, ["glb2", "glb"]),
            "gltf" => FirstWorkingExportId(ctx, ["gltf2", "gltf"]),
            "dae" => FirstWorkingExportId(ctx, ["collada", "dae"]),
            "obj" => FirstWorkingExportId(ctx, ["obj"]),
            "stl" => FirstWorkingExportId(ctx, ["stlb", "stl"]),
            _ => null,
        };
    }

    private static string? FirstWorkingExportId(AssimpContext ctx, IEnumerable<string> candidates)
    {
        var supported = ctx.GetSupportedExportFormats() ?? [];
        foreach (var id in candidates)
        {
            if (supported.Any(f => f.FormatId.Equals(id, StringComparison.OrdinalIgnoreCase)))
                return supported.First(f => f.FormatId.Equals(id, StringComparison.OrdinalIgnoreCase)).FormatId;
        }

        return null;
    }

    private static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "mesh";
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray();
        return new string(chars);
    }
}
