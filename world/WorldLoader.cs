using System;
using Godot;

namespace projectblacklodge.world;

[Tool]
public partial class WorldLoader : Node3D {
    private TerrainData _terrainData;

    private MeshInstance3D _meshInstance;
    private StaticBody3D _staticBody;
    private CollisionShape3D _collisionShape;

    [Export]
    public Resource TerrainData {
        get => _terrainData;
        set {
            _terrainData = (TerrainData)value;
            if (value is not TerrainData data || _terrainData == data) return;
            _terrainData = data;
            BuildMeshFromData();
        }
    }

    [Export] public StandardMaterial3D TerrainMaterial { get; set; }

#if TOOLS
    [ExportGroup("Import")] [Export] public Texture2D HeightMapTexture;
    [Export] public float HeightScale { get; set; } = 25.0f;

    [Export]
    public bool ImportFromTexture { //Cheat to make a 'button'
        get => false;
        set {
            if (!value) return;
            NotifyPropertyListChanged();
            GenerateMeshFromHeightMap();
        }
    }
#endif

    public override void _Ready() {
        GenerateRequiredNodes();

        if (TerrainData != null) BuildMeshFromData();
    }

    private void GenerateRequiredNodes() {
        _meshInstance = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        if (_meshInstance == null) {
            _meshInstance = new MeshInstance3D { Name = "MeshInstance3D" };
            AddChild(_meshInstance, true);
            _meshInstance.Owner = Owner ?? this;
            GD.Print(Owner.Name);
        }

        _staticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (_staticBody == null) {
            _staticBody = new StaticBody3D { Name = "StaticBody3D" };
            AddChild(_staticBody, true);
            _staticBody.Owner = Owner ?? this;
        }

        _collisionShape = _staticBody.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (_collisionShape == null) {
            _collisionShape = new CollisionShape3D { Name = "CollisionShape3D" };
            _staticBody.AddChild(_collisionShape, true);
            _collisionShape.Owner = Owner ?? this;
        }
    }

    private void BuildMeshFromData() {
        if (_terrainData?.Vertices == null) {
            GD.Print("No terrain data found.");
            return;
        }

        GenerateRequiredNodes();

        GD.Print("Building mesh from TerrainData resource...");

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var vertices = _terrainData.Vertices;
        var sizeX = _terrainData.SizeX;
        var sizeZ = _terrainData.SizeZ;

        for (var i = 0; i < vertices.Length; i++) {
            surfaceTool.SetUV(new Vector2((float)(i % sizeX) / (sizeX - 1), (float)i / sizeX) / (sizeZ - 1));
            surfaceTool.AddVertex(vertices[i]);
        }

        for (var x = 0; x < sizeX - 1; x++) {
            for (var z = 0; z < sizeZ - 1; z++) {
                var v0 = z * sizeX + x;         // Top-Left
                var v1 = v0 + 1;                // Top-Right
                var v2 = (z + 1) * sizeX + x;   // Bottom-Left
                var v3 = v2 + 1;                // Bottom-Right

                // First triangle (Top-Left, Top-Right, Bottom-Left)
                surfaceTool.AddIndex(v0);
                surfaceTool.AddIndex(v1);
                surfaceTool.AddIndex(v2);

                // Second triangle (Top-Right, Bottom-Right, Bottom-Left)
                surfaceTool.AddIndex(v1);
                surfaceTool.AddIndex(v3);
                surfaceTool.AddIndex(v2);
            }
        }

        surfaceTool.GenerateNormals();

        var mesh = surfaceTool.Commit();

        if (TerrainMaterial is null) {
            TerrainMaterial = new StandardMaterial3D();
            TerrainMaterial.AlbedoColor = new Color(0.2f, 0.2f, 0.2f);
        }

        mesh.SurfaceSetMaterial(0, TerrainMaterial);
        _meshInstance.Mesh = mesh;
        _collisionShape.Shape = mesh.CreateTrimeshShape();

        GD.Print("Mesh built.");
    }

    public void UpdateVertexPosition(int index, Vector3 newPosition) {
        if (_terrainData?.Vertices == null) {
            GD.Print("No terrain data found.");
            return;
        }

        if (_meshInstance == null) {
            GD.PushError("Mesh instance not initialized.");
            return;
        }

        if (_meshInstance.Mesh is not ArrayMesh mesh) {
            GD.PushError("Bad mesh type for _meshInstance, expected ArrayMesh.");
            return;
        }

        _terrainData.Vertices[index] = newPosition;

        var arrays = mesh.SurfaceGetArrays(0);
        var vertexArray = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        vertexArray[index] = newPosition;
        arrays[(int)Mesh.ArrayType.Vertex] = vertexArray;

        mesh.ClearSurfaces();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, TerrainMaterial);

        _collisionShape.Shape = mesh.CreateTrimeshShape();
    }

#if TOOLS
    private void GenerateMeshFromHeightMap() {
        if (HeightMapTexture == null) {
            GD.PushError("No HeightMapTexture assigned. Cannot generate terrain data.");
            return;
        }
        GD.Print("Generating terrain data...");

        var image = HeightMapTexture.GetImage();
        var newTerrainData = new TerrainData(
            image.GetWidth(),
            image.GetHeight(),
            new Vector3[image.GetWidth() * image.GetHeight()]
        );

        var minHeight = float.MaxValue;
        for (var x = 0; x < image.GetWidth(); x++) {
            for (var z = 0; z < image.GetHeight(); z++) {
                // We only need to check the red channel (R) as it's a grayscale value.
                var currentVal = image.GetPixel(x, z).R;
                if (currentVal < minHeight) {
                    minHeight = currentVal;
                }
            }
        }

        for (var x = 0; x < newTerrainData.SizeX; x++) {
            for (var z = 0; z < newTerrainData.SizeZ; z++) {
                var color = image.GetPixel(x, z);
                var y = (color.R - minHeight) * HeightScale;
                newTerrainData.Vertices[z * newTerrainData.SizeX + x] = new Vector3(x, y, z);
            }
        }

        TerrainData = newTerrainData;


        BuildMeshFromData();
    }
#endif
}