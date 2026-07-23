using System.Collections.Generic;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class Cube : MonoBehaviour
{
    public AssetRef<Material> material;

    private MeshRenderer _meshRenderer;

    public override void Start()
    {
        _meshRenderer = AddComponent<MeshRenderer>();
        _meshRenderer.Material = material;

        var mesh = new Mesh();

        var verts = new List<Float3>();
        var indices = new List<uint>();
        var uv = new List<Float2>();

        for (int face = 0; face < 6; face++)
        {
            indices.Add((uint)verts.Count + 0);
            indices.Add((uint)verts.Count + 1);
            indices.Add((uint)verts.Count + 2);
            indices.Add((uint)verts.Count + 2);
            indices.Add((uint)verts.Count + 1);
            indices.Add((uint)verts.Count + 3);

            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 0]]);
            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 1]]);
            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 2]]);
            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 3]]);

            uv.Add(new Float2(0, 0));
            uv.Add(new Float2(0, 1));
            uv.Add(new Float2(1, 0));
            uv.Add(new Float2(1, 1));
        }

        mesh.Vertices = verts.ToArray();
        mesh.Indices = indices.ToArray();
        mesh.UV = uv.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        _meshRenderer.Mesh = mesh;
    }
}
