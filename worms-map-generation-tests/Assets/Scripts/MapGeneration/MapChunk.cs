using System;
using System.Collections;

/// <summary>
/// A 64x64 chunk of map data
/// </summary>
public class MapChunk
{
// TODO: should these be structs or classes? Does it matter?
    public UInt64[] Chunk;

    public int XOffset;
    public int YOffset;

    public void Init(int x, int y)
    {
        XOffset = x;
        YOffset = y;
        Chunk = new UInt64[64];
    }

    public const int ChunkSize = 64;
}