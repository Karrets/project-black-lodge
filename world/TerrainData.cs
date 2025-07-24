using Godot;

namespace projectblacklodge.world;

[Tool]
public partial class TerrainData : Resource {
    public TerrainData() : this(0, 0, []) {}

    public TerrainData(int sizeX, int sizeZ, Vector3[] vertices) {
        SizeX = sizeX;
        SizeZ = sizeZ;
        Vertices = vertices;
    }

    [Export]
    public int SizeX { get; set; }

    [Export]
    public int SizeZ { get; set; }

    [Export]
    public Vector3[] Vertices { get; set; }
}