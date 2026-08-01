using Prowl.Runtime;
using Prowl.Runtime.Terrain;
using Prowl.Vector;

public class NoiseMapTerrain : MonoBehaviour
{
    public TerrainComponent terrainComponent;

    public override void Start()
    {
        var noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        int res = terrainComponent.Data.Res.HeightmapResolution;

        for (int x = 0; x < res; x++)
        for (int z = 0; z < res; z++)
        {
            var xzPos = new Float2(x, z);

            float frequency = 1.1f;
            float height01 = (noise.GetNoise(xzPos.X * frequency, xzPos.Y * frequency) + 1f) * 0.5f;
            terrainComponent.Data.Res.SetHeight(x, z, height01);
        }
    }
}
