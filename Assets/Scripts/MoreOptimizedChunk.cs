using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using Prowl.Editor.Core;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Voxels;

public class MoreOptimizedChunk : MonoBehaviour
{
    public AssetRef<Material> material;

    private static int ChunkSize = 16;

    private static int ChunkHeight = 256;

    private MeshRenderer? meshRenderer;
    private BoxelCollider? boxelCollider;

    private List<RigidBodyShape> colliderShapes = new();

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
        if (enabled)
        {
            if (boxelCollider is null)
            {
                boxelCollider = AddComponent<BoxelCollider>();
                boxelCollider.Set(colliderShapes.ToArray());
            }
        }
        else
        {
            if (boxelCollider is not null)
            {
                RemoveComponent<BoxelCollider>(boxelCollider);
                boxelCollider = null;
            }
        }
    }

    public override void Start()
    {
        Generate();
    }

    private void AddColliderShape(int x, int y, int z)
    {
        var boxShape = new BoxShape(1f, 1f, 1f);
        var center = new JVector(x + 0.5f, y + 0.5f, z + 0.5f);
        colliderShapes.Add(new TransformedShape(boxShape, center));
    }

    public void Generate()
    {
        if (isGenerated) return;

        var noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        byte[,,] voxels = new byte[ChunkSize, ChunkHeight, ChunkSize];

        bool IsNoiseAir(int x, int y, int z)
        {
            float frequency = 1.1f;
            var offset = (new Float3(x, y, z) + position) * frequency;
            
            float secondaryFrequencyFrequency = 0.1f;
            float secondaryFrequency = (float)((noise.GetNoise(offset.X * secondaryFrequencyFrequency, offset.Z * secondaryFrequencyFrequency) + 1) * 0.5f); // 0 to 1
            secondaryFrequency = (secondaryFrequency + 0.5f) * 2f; // 0.5 to 1.5
            offset *= secondaryFrequency;

            // bottom terrain
            var noise01 = (noise.GetNoise(offset.X + 1000, offset.Z) + 1) * 0.5; 
            var noise01_idk = ((noise.GetNoise((offset.X - 1000) * 0.01f, (offset.Z) * 0.1f) + 1) * 0.5) * 2f; 
            var terrainMaxHeight = 16;
            var terrainHeight = noise01 * noise01_idk * terrainMaxHeight;
            if (y < terrainHeight) return false;
            // return y > terrainHeight;

            // caves
            var caves = (noise.GetNoise(offset.X, offset.Y * 0.001f, offset.Z) + 1) * 0.5; // 0 to 1
            if (caves < 0.5f) return true;
    
            // floating islands
            var noise01FloatingIslandsTop = (noise.GetNoise(offset.X + 1000, offset.Z) + 1) * 0.5; // 0 to 1 * 16
            var noise01FloatingIslandsBottom = (noise.GetNoise(offset.X - 82348, offset.Z + 100) + 1) * 0.5; // 0 to 1
            var floatingIslandsHeight = 50;
            var floatIslandsDepth = 25;
            var bottomHeight = (noise01FloatingIslandsTop * floatIslandsDepth) + floatingIslandsHeight;
            var topHeight = (noise01FloatingIslandsBottom * floatIslandsDepth) + floatingIslandsHeight;
            if (offset.Y > bottomHeight && offset.Y < topHeight)
            {
                return false; // dirt
            }

            return true;
        }

        bool IsAir(int x, int y, int z)
        {
            if (x < ChunkSize && x >= 0 &&
                y < ChunkHeight && y >= 0 &&
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
        for (int y = 0; y < ChunkHeight; y++)
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

        var verts = new List<Float3>();
        var indices = new List<uint>();
        var uv = new List<Float2>();

        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < ChunkHeight; y++)
        for (int z = 0; z < ChunkSize; z++)
        {
            var offset = new Float3(x, y, z);

            if (!IsAir(x, y, z))
            {
                bool isSurface = false;

                for (int face = 0; face < 6; face++)
                {
                    if (IsNeighborAir(x, y, z, face))
                    {
                        isSurface = true;

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

                if (isSurface)
                {
                    AddColliderShape(x, y, z);
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
