using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Voxels;

public class World : MonoBehaviour
{
    public Transform player;
    public AssetRef<Material> material;
    public int RenderDistance = 4;

    private int ChunkSize = 16;
    private float PhysicsDistance = 1; // default, should not be modified
    private Dictionary<Int2, MoreOptimizedChunk> chunks = new();
    private Float3 initialPlayerPos;
    private bool firstRunIsLoaded = false;

    public override void OnGui(Paper paper)
    {
        using (paper.Column("123").Enter())
        {
            paper.Box("redbox")
                .Width(UnitValue.Pixels(100))
                .Height(UnitValue.Pixels(100))
                .BackgroundColor(Color.Red);

            paper.Box("greenbox")
                .Hovered.
                    BackgroundColor(Color.Blue)
                .End()
                .OnClick((_) => Debug.Log("Hello World"))
                .Width(UnitValue.Pixels(100))
                .Height(UnitValue.Pixels(100))
                .BackgroundColor(Color.Green);
        }
    }

    public override void Start()
    {
        initialPlayerPos = player.Position;
    }

    public override void Update()
    {
        Int2 playerChunk = WorldToChunkPos(player.Position);

        for (int x = -RenderDistance; x <= RenderDistance; x++)
        {
            for (int z = -RenderDistance; z <= RenderDistance; z++)
            {
                Int2 chunkPos = new(playerChunk.X + x, playerChunk.Y + z);

                // load chunk before doing anything else
                if (!chunks.ContainsKey(chunkPos))
                    LoadChunk(chunkPos);
                
                // enable or disable physics
                if (chunks.TryGetValue(chunkPos, out var chunk))
                {
                    if (IsInsidePhysicsDistance(chunkPos))
                    {
                        chunk.EnablePhysics(true);
                    }
                    else
                    {
                        chunk.EnablePhysics(false);
                    }
                }
            }
        }

        List<Int2>? toUnload = null;
        foreach (var pos in chunks.Keys)
        {
            // if outside of the render distance
            if (!IsInsideRenderDistance(pos, playerChunk))
            {
                toUnload ??= [];
                toUnload.Add(pos);
            }
        }

        if (toUnload != null)
            foreach (var pos in toUnload)
                UnloadChunk(pos);
        
        if (!firstRunIsLoaded)
        {
            player.Position = initialPlayerPos;
            firstRunIsLoaded = true;
        }
    }

    private bool IsInsideRenderDistance(Int2 chunkPos, Int2 playerChunk)
    {
        // if outside of the render distance
        var cardinalDistance = new Int2(chunkPos.X - playerChunk.X, chunkPos.Y - playerChunk.Y);
        if (cardinalDistance.X >  RenderDistance || 
            cardinalDistance.X < -RenderDistance || 
            cardinalDistance.Y >  RenderDistance || 
            cardinalDistance.Y < -RenderDistance)
        {
            return false;
        }
        return true;
    }

    private bool IsInsidePhysicsDistance(Int2 chunkPos)
    {
        // convert from chunk distance to meter distance
        var distanceInMeters = PhysicsDistance * ChunkSize;

        var centerChunkPos = new Float2((chunkPos.X * ChunkSize) + (ChunkSize * 0.5f), (chunkPos.Y * ChunkSize)+ (ChunkSize * 0.5f));

        // if outside of the render distance
        var cardinalDistance = new Float2(centerChunkPos.X - player.Position.X, centerChunkPos.Y  - player.Position.Z);
        if (cardinalDistance.X >  distanceInMeters || 
            cardinalDistance.X < -distanceInMeters || 
            cardinalDistance.Y >  distanceInMeters || 
            cardinalDistance.Y < -distanceInMeters)
        {
            return false;
        }
        return true;

        // return Float2.Distance((Float2)pos * ChunkSize, (Float2)playerChunk * ChunkSize) < PhysicsDistance * ChunkSize;
    }

    private void LoadChunk(Int2 chunkPos)
    {
        var pos = new Float3(chunkPos.X * ChunkSize, 0, chunkPos.Y * ChunkSize);

        GameObject chunkGO = new($"Chunk_{chunkPos.X}_{chunkPos.Y}");
        MoreOptimizedChunk chunk = chunkGO.AddComponent<MoreOptimizedChunk>();
        chunk.Initialize(this, pos, material);
        chunk.Generate();

        chunks[chunkPos] = chunk;
        GameObject.Scene.Add(chunkGO);
    }

    private void UnloadChunk(Int2 chunkPos)
    {
        if (!chunks.TryGetValue(chunkPos, out MoreOptimizedChunk? chunk))
            return;

        chunks.Remove(chunkPos);
        GameObject.Scene.Remove(chunk.GameObject);
        chunk.GameObject.Dispose();
    }

    private Int2 WorldToChunkPos(Float3 worldPos)
    {
        int x = worldPos.X >= 0 ? (int)(worldPos.X / ChunkSize) : (int)((worldPos.X - ChunkSize + 1) / ChunkSize);
        int z = worldPos.Z >= 0 ? (int)(worldPos.Z / ChunkSize) : (int)((worldPos.Z - ChunkSize + 1) / ChunkSize);
        return new Int2(x, z);
    }
}
