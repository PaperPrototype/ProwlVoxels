using System.Collections.Generic;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;
using Silk.NET.Vulkan;

public class OptimizedChunk : MonoBehaviour
{
    public AssetRef<Material> material;

    private int ChunkSize = 16;

    private MeshRenderer _meshRenderer;

    public override void Start()
    {
        var noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        bool IsAir(float x, float y, float z, int face)
        {
            float frequency = 1.1f;
            var offset = new Float3(x * frequency, y * frequency, z * frequency) + VoxelTables.Offsets[face];
            return noise.GetNoise(offset.X, offset.Y, offset.Z) > 0;
        }

        _meshRenderer = AddComponent<MeshRenderer>();
        _meshRenderer.Material = material;

        var mesh = new Mesh
        {
            IndexFormat = IndexFormat.UInt32
        };

        var verts = new List<Float3>();
        var indices = new List<uint>();
        var uv = new List<Float2>();

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    var offset = new Float3(x, y, z);
                    for (int face = 0; face < 6; face++)
                    {
                        if (IsAir(x, y, z, face))
                        {
                            indices.Add((uint)verts.Count + 0);
                            indices.Add((uint)verts.Count + 1);
                            indices.Add((uint)verts.Count + 2);
                            indices.Add((uint)verts.Count + 2);
                            indices.Add((uint)verts.Count + 1);
                            indices.Add((uint)verts.Count + 3);

                            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVertices[face, 0]] + offset);
                            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVertices[face, 1]] + offset);
                            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVertices[face, 2]] + offset);
                            verts.Add(VoxelTables.Vertices[VoxelTables.QuadVertices[face, 3]] + offset);

                            uv.Add(new Float2(0, 0));
                            uv.Add(new Float2(0, 1));
                            uv.Add(new Float2(1, 0));
                            uv.Add(new Float2(1, 1));
                        }
                    }
                }
            }
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
