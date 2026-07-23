using System.Collections.Generic;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

public class World : MonoBehaviour
{
    public AssetRef<Material> material;

    private int ChunkSize = 16;

    Dictionary<Int2, Chunk> chunks = new();

    public override void Start()
    {
    }
}
