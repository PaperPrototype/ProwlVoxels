using Prowl.Runtime;
using Prowl.Vector;

public static class VoxelTables
{
     // all 8 possible vertices for a voxel
    public static readonly Float3[] Vertices = new Float3[8]
    {
        new Float3(0.0f, 0.0f, 0.0f),
        new Float3(1.0f, 0.0f, 0.0f),
        new Float3(1.0f, 1.0f, 0.0f),
        new Float3(0.0f, 1.0f, 0.0f),
        new Float3(0.0f, 0.0f, 1.0f),
        new Float3(1.0f, 0.0f, 1.0f),
        new Float3(1.0f, 1.0f, 1.0f),
        new Float3(0.0f, 1.0f, 1.0f),
    };

    // vertices to build a quad for each side of a voxel
    public static readonly int[,] QuadVerticesIndex = new int[6, 4]
    {
        // quad order
        // right, left, up, down, front, back

        // 0 1 2 2 1 3 <- triangle numbers

        // quads
        {1, 2, 5, 6}, // right quad
        {4, 7, 0, 3}, // left quad

        {3, 7, 2, 6}, // up quad
        {1, 5, 0, 4}, // down quad

        {5, 6, 4, 7}, // front quad
        {0, 3, 1, 2}, // back quad
    };
}
