using System;
using System.Collections.Generic;
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

    public int PhysicsDistance = 1;

    public int RenderDistance = 4;

    private int ChunkSize = 16;

    Dictionary<Int2, OptimizedChunk> chunks = new();

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
        Int2 playerChunk = WorldToChunkPos(player.Position);

        for (int x = -RenderDistance; x <= RenderDistance; x++)
        {
            for (int z = -RenderDistance; z <= RenderDistance; z++)
            {
                Int2 chunkPos = new(playerChunk.X + x, playerChunk.Y + z);

                if (!chunks.ContainsKey(chunkPos))
                    LoadChunk(chunkPos);
            }
        }

        for (int x = -PhysicsDistance; x <= PhysicsDistance; x++)
        {
            for (int z = -PhysicsDistance; z <= PhysicsDistance; z++)
            {
                Int2 chunkPos = new(playerChunk.X + x, playerChunk.Y + z);

                if (chunks.TryGetValue(chunkPos, out var chunk))
                {
                    if (IsInsidePhysicsDistance(chunkPos, playerChunk))
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
    }

    private bool IsInsideRenderDistance(Int2 pos, Int2 playerChunk)
    {
        // if outside of the render distance
        var cardinalDistance = new Int2(pos.X - playerChunk.X, pos.Y - playerChunk.Y);
        if (cardinalDistance.X >  RenderDistance || 
            cardinalDistance.X < -RenderDistance || 
            cardinalDistance.Y >  RenderDistance || 
            cardinalDistance.Y < -RenderDistance)
        {
            return false;
        }
        return true;
    }

    private bool IsInsidePhysicsDistance(Int2 pos, Int2 playerChunk)
    {
        // if outside of the render distance
        var cardinalDistance = new Int2(pos.X - playerChunk.X, pos.Y - playerChunk.Y);
        if (cardinalDistance.X >  PhysicsDistance || 
            cardinalDistance.X < -PhysicsDistance || 
            cardinalDistance.Y >  PhysicsDistance || 
            cardinalDistance.Y < -PhysicsDistance)
        {
            return false;
        }
        return true;
    }

    private void LoadChunk(Int2 chunkPos)
    {
        var pos = new Float3(chunkPos.X * ChunkSize, 0, chunkPos.Y * ChunkSize);

        GameObject chunkGO = new($"Chunk_{chunkPos.X}_{chunkPos.Y}");
        OptimizedChunk chunk = chunkGO.AddComponent<OptimizedChunk>();
        chunk.Initialize(this, pos, material);
        chunk.Generate();

        chunks[chunkPos] = chunk;
        GameObject.Scene.Add(chunkGO);
    }

    private void UnloadChunk(Int2 chunkPos)
    {
        if (!chunks.TryGetValue(chunkPos, out OptimizedChunk? chunk))
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
