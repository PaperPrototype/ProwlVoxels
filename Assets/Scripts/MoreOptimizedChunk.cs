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
using Silk.NET.Vulkan;

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
    private Float3 halfExtents = (new Float3(ChunkSize, ChunkHeight, ChunkSize) * 0.5f);
    private Float3 center => position + halfExtents;

    private bool isGenerated = false;

    private byte[,,] voxels = new byte[ChunkSize, ChunkHeight, ChunkSize];

    private Mesh mesh = new Mesh { IndexFormat = IndexFormat.UInt32 };


    public void Initialize(World world, Float3 _position, AssetRef<Material> _material)
    {
        material = _material;
        position = _position;

        GameObject.Transform.Parent = world.Transform;
        GameObject.Transform.Position = position;

        var rigidbody = AddComponent<Rigidbody3D>();
        rigidbody.MotionType = Jitter2.Dynamics.MotionType.Static;
        rigidbody.AffectedByGravity = false;

        meshRenderer = AddComponent<MeshRenderer>();
        meshRenderer.Material = material;
    }

    public void RaycastUpdateBlock(Ray ray, byte voxel)
    {
        if (mesh.Raycast(ray, out var distance, out var normal))
        {   
            var pos = ray.Origin + Float3.Normalize(ray.Direction) * distance;
            var index = Maths.FloorToInt(pos - (normal * 0.5f));
            if (IsInsideBounds(index.X, index.Y, index.Z)) 
            {
                Debug.Log("IsInside bounds");
                voxels[index.X, index.Y, index.Z] = voxel;
                CalcMesh();
                boxelCollider?.Set(colliderShapes.ToArray());
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Voxel position ({index.X}, {index.Y}, {index.Z}) is outside chunk bounds.");
            }
        }
    }

    public void UpdateBlock(int x, int y, int z, byte voxel)
    {
        if (IsInsideBounds(x, y, z)) 
        {
            Debug.Log("IsInside bounds");
            voxels[x, y, z] = voxel;
            CalcMesh();
            boxelCollider?.Set(colliderShapes.ToArray());
        }
        else
        {
            throw new ArgumentOutOfRangeException($"Voxel position ({x}, {y}, {z}) is outside chunk bounds.");
        }
    }

    public override void DrawGizmos()
    {
        Debug.DrawWireCube(center, halfExtents, Color.Blue);
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

    private void CalcNoise()
    {
        var noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

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

    }

    private void CalcMesh()
    {
        colliderShapes.Clear();

        bool IsAir(int x, int y, int z)
        {
            if (IsInsideBounds(x, y, z))
            {
                return voxels[x, y, z] == 0;
            }
            else
            {
                return true;
            }
        }

        bool IsNeighborAir(float x, float y, float z, int face)
        {
            var pos = new Float3(x, y, z) + VoxelTables.Offsets[face];
            return IsAir((int)pos.X, (int)pos.Y, (int)pos.Z);
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

        mesh.Clear();

        mesh.Vertices = verts.ToArray();
        mesh.Indices = indices.ToArray();
        mesh.UV = uv.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        meshRenderer.Mesh = mesh;
    }

    bool IsInsideBounds(int x, int y, int z)
    {
        if (x < ChunkSize && x >= 0 &&
            y < ChunkHeight && y >= 0 &&
            z < ChunkSize && z >= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Generate()
    {
        if (isGenerated) return;

        CalcNoise();
        CalcMesh();

        isGenerated = true;
    }
}
