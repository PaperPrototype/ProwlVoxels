using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prowl.Editor.Core;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Voxels;

public class MoreOptimizedChunk : MonoBehaviour
{
    public AssetRef<Material> material;

    private static int ChunkSize = 16;

    private MeshRenderer? meshRenderer;

    private readonly List<BoxCollider> boxColliders = new();
    private List<(Float3 Center, Float3 Size)> colliderBoxes = new();
    private bool physicsEnabled = false;

    private Float3 position;

    // private byte[,,] voxels = new byte[ChunkSize, ChunkSize, ChunkSize];

    private Mesh mesh = new Mesh { IndexFormat = IndexFormat.UInt32 };

    private bool isGenerated = false;

    public void Initialize(World world, Float3 _position, AssetRef<Material> _material)
    {
        material = _material;
        position = _position;

        GameObject.Transform.Parent = world.Transform;
        GameObject.Transform.Position = position;
    }

    public void EnablePhysics(bool enabled)
    {
        if (enabled == physicsEnabled) return;
        physicsEnabled = enabled;

        if (enabled)
        {
            foreach (var box in colliderBoxes)
            {
                BoxCollider collider = AddComponent<BoxCollider>();
                collider.Center = box.Center;
                collider.Size = box.Size;
                boxColliders.Add(collider);
            }
        }
        else
        {
            GameObject.RemoveAll<BoxCollider>();
            boxColliders.Clear();
        }
    }

    /// <summary>
    /// Greedily merges solid voxels into the fewest axis-aligned boxes: expand each unclaimed solid
    /// voxel along X, then Y (while the whole X-run stays solid), then Z (while the whole X*Y slab
    /// stays solid), matching greedy-meshing but for colliders instead of render quads.
    /// </summary>
    private static List<(Float3 Center, Float3 Size)> BuildColliderBoxes(byte[,,] voxels)
    {
        var boxes = new List<(Float3, Float3)>();
        bool[,,] merged = new bool[ChunkSize, ChunkSize, ChunkSize];

        bool RowIsSolid(int x, int y, int z, int dx)
        {
            for (int ix = 0; ix < dx; ix++)
                if (merged[x + ix, y, z] || voxels[x + ix, y, z] == 0)
                    return false;
            return true;
        }

        bool SlabIsSolid(int x, int y, int z, int dx, int dy)
        {
            for (int iy = 0; iy < dy; iy++)
                if (!RowIsSolid(x, y + iy, z, dx))
                    return false;
            return true;
        }

        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < ChunkSize; y++)
        for (int z = 0; z < ChunkSize; z++)
        {
            if (merged[x, y, z] || voxels[x, y, z] == 0)
                continue;

            int dx = 1;
            while (x + dx < ChunkSize && !merged[x + dx, y, z] && voxels[x + dx, y, z] != 0)
                dx++;

            int dy = 1;
            while (y + dy < ChunkSize && RowIsSolid(x, y + dy, z, dx))
                dy++;

            int dz = 1;
            while (z + dz < ChunkSize && SlabIsSolid(x, y, z + dz, dx, dy))
                dz++;

            for (int ix = 0; ix < dx; ix++)
            for (int iy = 0; iy < dy; iy++)
            for (int iz = 0; iz < dz; iz++)
                merged[x + ix, y + iy, z + iz] = true;

            var size = new Float3(dx, dy, dz);
            var center = new Float3(x, y, z) + size * 0.5f;
            boxes.Add((center, size));
        }

        return boxes;
    }

    public override void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (isGenerated) return;

        var noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        byte[,,] voxels = new byte[ChunkSize, ChunkSize, ChunkSize];

        bool IsNoiseAir(int x, int y, int z)
        {
            float frequency = 1.1f;
            var offset = (new Float3(x, y, z) + position) * frequency;

            // var gradient = y / ChunkSize;

            // var noise01 = (noise.GetNoise(offset.X, offset.Y, offset.Z) + 1) * 0.5; 
            // return noise01 < 0.5f;

            var noise01 = (noise.GetNoise(offset.X, offset.Z) + 1) * 0.5; 

            var terrainHeight = noise01 * ChunkSize;
            return y > terrainHeight;
        }

        bool IsAir(int x, int y, int z)
        {
            if (x < ChunkSize && x >= 0 &&
                y < ChunkSize && y >= 0 &&
                z < ChunkSize && z >= 0)
            {
                return voxels[x, y, z] == 0;
            }
            else
            {
                // return IsNoiseAir(x, y, z);
                return true;
            }
        }

        bool IsNeighborAir(float x, float y, float z, int face)
        {
            var pos = new Float3(x, y, z) + VoxelTables.Offsets[face];
            return IsAir((int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    if (IsNoiseAir(x, y, z))
                    {
                        voxels[x, y, z] = 0; // Air
                    }
                    else
                    {
                        voxels[x, y, z] = 1; // Dirt
                    }
                }
            }
        }

        colliderBoxes = BuildColliderBoxes(voxels);

        var verts = new List<Float3>();
        var indices = new List<uint>();
        var uv = new List<Float2>();

        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < ChunkSize; y++)
        for (int z = 0; z < ChunkSize; z++)
        {
            var offset = new Float3(x, y, z);

            if (!IsAir(x, y, z))
            {
                for (int face = 0; face < 6; face++)
                {
                    if (IsNeighborAir(x, y, z, face))
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

        if (verts.Count == 0) return;

        mesh.Vertices = verts.ToArray();
        mesh.Indices = indices.ToArray();
        mesh.UV = uv.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        meshRenderer = AddComponent<MeshRenderer>();
        meshRenderer.Material = material;
        meshRenderer.Mesh = mesh;

        isGenerated = true;
    }
}
