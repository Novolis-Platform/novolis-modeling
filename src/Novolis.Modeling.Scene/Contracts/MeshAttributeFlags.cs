namespace Novolis.Modeling;

/// <summary>Renderer-neutral mesh data and derived-data capabilities.</summary>
[Flags]
public enum MeshAttributeFlags
{
    None = 0,
    Positions = 1 << 0,
    Indices = 1 << 1,
    Normals = 1 << 2,
    Tangents = 1 << 3,
    UV0 = 1 << 4,
    UVs = UV0,
    Uvs = UV0,
    MaterialSlots = 1 << 5,
    Curvature = 1 << 6,
    Cavity = 1 << 7,
    Bounds = 1 << 8,
    Topology = 1 << 9,
    Adjacency = 1 << 10,
}
