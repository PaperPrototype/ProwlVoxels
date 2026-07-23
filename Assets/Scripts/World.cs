using System.Collections.Generic;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class World : MonoBehaviour
{
    public Transform player;
    public AssetRef<Material> material;

    public int RenderDistance = 4;

    private int ChunkSize = 16;

    Dictionary<Int2, OptimizedChunk> chunks = new();

    public override void Update()
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

        List<Int2>? toUnload = null;
        foreach (var pos in chunks.Keys)
        {
            int dx = pos.X - playerChunk.X;
            int dz = pos.Y - playerChunk.Y;
            if (dx > RenderDistance || dx < -RenderDistance || dz > RenderDistance || dz < -RenderDistance)
            {
                toUnload ??= [];
                toUnload.Add(pos);
            }
        }

        if (toUnload != null)
            foreach (var pos in toUnload)
                UnloadChunk(pos);
    }

    private void LoadChunk(Int2 chunkPos)
    {
        var pos = new Float3(chunkPos.X * ChunkSize, 0, chunkPos.Y * ChunkSize);

        GameObject chunkGO = new($"Chunk_{chunkPos.X}_{chunkPos.Y}");
        OptimizedChunk chunk = chunkGO.AddComponent<OptimizedChunk>();
        chunk.Initialize(this, pos, material);

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
