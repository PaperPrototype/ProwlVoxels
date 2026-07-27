using System.Collections.Generic;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class SimpleQuad : MonoBehaviour
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


        /*
        (1,1,0) ─────────── (1,1,1)
          |  \               |
          |     \            |
          |        \         |
          |           \      |
          |              \   |
        (1,0,0) ─────────── (1,0,1)
        */
        verts.Add(new Float3(1.0f, 0.0f, 0.0f));
        verts.Add(new Float3(1.0f, 1.0f, 0.0f));
        verts.Add(new Float3(1.0f, 0.0f, 1.0f));
        verts.Add(new Float3(1.0f, 1.0f, 1.0f));

        indices.AddRange([0, 1, 2]);
        indices.AddRange([2, 1, 3]);

        mesh.Vertices = verts.ToArray();
        mesh.Indices = indices.ToArray();
        mesh.UV = uv.ToArray();

        uv.Add(new Float2(0, 0));
        uv.Add(new Float2(0, 1));
        uv.Add(new Float2(1, 0));
        uv.Add(new Float2(1, 1));

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        _meshRenderer.Mesh = mesh;
    }
}
