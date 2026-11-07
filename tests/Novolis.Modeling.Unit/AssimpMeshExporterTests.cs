using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Modeling.Unit;

public sealed class AssimpMeshExporterTests
{
    private static bool AssimpNativeAvailable => OperatingSystem.IsWindows();

    [Test]
    public async Task Export_writes_glb_for_triangle()
    {
        if (!AssimpNativeAvailable)
        {
            await Assert.That(AssimpMeshExporter.IsSupportedExtension(".glb")).IsTrue();
            return;
        }

        var mesh = new EditableMesh(
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            [0, 1, 2]);
        var path = Path.Combine(Path.GetTempPath(), "novolis-assimp-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            AssimpMeshExporter.Export(path, mesh, "tri");
            await Assert.That(File.Exists(path)).IsTrue();
            await Assert.That(new FileInfo(path).Length).IsGreaterThan(32);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
