using System;
using System.Collections.Generic;
using Godot;

namespace projectblacklodge.world;

[Tool]
public partial class WorldLoader : Node3D {
    [Export]
    public int NodeX {
        get => _nodeX;
        set {
            _nodeX = value;
            GenerateTerrain();
        }
    }

    [Export]
    public int NodeZ {
        get => _nodeZ;
        set {
            _nodeZ = value;
            GenerateTerrain();
        }
    }

    [Export]
    public float NodeSpacing {
        get => _nodeSpacing;
        set {
            _nodeSpacing = value;
            GenerateTerrain();
        }
    }

    [Export]
    public float HeightScale {
        get => _heightScale;
        set {
            _heightScale = value;
            GenerateTerrain();
        }
    }

    [Export]
    public StandardMaterial3D TerrainMaterial {
        get => _terrainMaterial;
        set => UpdateResource(ref _terrainMaterial, value, GenerateTerrain);
    }

    [Export(PropertyHint.File, "*.png,*.jpg,*.jpeg,*.bmp")]
    public Texture2D HeightMapTexture {
        get => _heightMapTexture;
        set => UpdateResource(ref _heightMapTexture, value, GenerateTerrain);
    }

    private bool _isReady = false; //This seems hacky.

    private int _nodeX = 32;
    private int _nodeZ = 32;
    private float _nodeSpacing = 1.0f;
    private float _heightScale = 1.0f;
    private StandardMaterial3D _terrainMaterial;
    private Texture2D _heightMapTexture;

    //Child Nodes:
    private MeshInstance3D _meshInstance;
    private StaticBody3D _staticBody;
    private CollisionShape3D _collisionShape;

    public override void _Ready() {
        _meshInstance = new MeshInstance3D();
        _staticBody = new StaticBody3D();
        _collisionShape = new CollisionShape3D();

        _staticBody.AddChild(_collisionShape);
        AddChild(_meshInstance);
        AddChild(_staticBody);

        if (TerrainMaterial == null) {
            TerrainMaterial = new StandardMaterial3D();
            TerrainMaterial.AlbedoColor = new Color(0.2f, 0.5f, 0.2f);
        }

        _isReady = true;
        GenerateTerrain();
    }

    public override void _Process(double delta) {
    }

    private void GenerateTerrain() {
        if (!_isReady) {
            GD.PrintErr("Attempted to generate a terrain but not ready.");
            return;
        }

        GD.Print("Generating terrain...");

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var mesh = new ArrayMesh();
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();

        Image heightMapImage = null;
        if (HeightMapTexture != null) {
            heightMapImage = HeightMapTexture.GetImage();
        }
        else {
            GD.Print("No heightmap image found.");
        }

        var effectiveWidth = heightMapImage?.GetWidth() ?? NodeZ + 1;
        var effectiveHeight = heightMapImage?.GetHeight() ?? NodeX + 1;

        for (var x = 0; x <= NodeX; x++) {
            for (var z = 0; z <= NodeZ; z++) {
                var y = 0f;

                if (heightMapImage != null) {
                    var pixelX = Mathf.Min(x, effectiveWidth - 1);
                    var pixelZ = Mathf.Min(z, effectiveHeight - 1);
                    var color = heightMapImage.GetPixel(pixelX, pixelZ);
                    y = color.R * _heightScale;
                }

                vertices.Add(new Vector3(x * NodeSpacing, y, z * NodeSpacing));
                uvs.Add(new Vector2((float)x / NodeX, (float)z / NodeZ));
            }
        }

        for (var x = 0; x < NodeX; x++) {
            for (var z = 0; z < NodeZ; z++) {
                var v0 = z * (NodeZ + 1) + x;
                var v1 = v0 + 1;
                var v2 = (z + 1) * (NodeZ + 1) + x;
                var v3 = v2 + 1;

                surfaceTool.AddIndex(v0);
                surfaceTool.AddIndex(v2);
                surfaceTool.AddIndex(v1);

                surfaceTool.AddIndex(v1);
                surfaceTool.AddIndex(v2);
                surfaceTool.AddIndex(v3);
            }
        }

        for (var i = 0; i < vertices.Count; i++) {
            surfaceTool.SetUV(uvs[i]);
            surfaceTool.AddVertex(vertices[i]);
        }

        surfaceTool.GenerateNormals();
        surfaceTool.Commit(mesh);

        mesh.SurfaceSetMaterial(0, TerrainMaterial);
        _meshInstance.SetMesh(mesh);

        _collisionShape.SetShape(mesh.CreateTrimeshShape());

        GD.Print("Terrain generated.");
    }

    private static void UpdateResource<T>(ref T backingField, T newValue, Action onChanged) where T : Resource
    {
        if (backingField == newValue) return;
        if (backingField != null) backingField.Changed -= onChanged;
        backingField = newValue;
        if (backingField != null) backingField.Changed += onChanged;
        onChanged();
    }
}