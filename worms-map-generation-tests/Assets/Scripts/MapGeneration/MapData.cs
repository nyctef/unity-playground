using System.Collections.Generic;
using UnityEngine;

public class MapData
{
    private const int ChunkOffset = MapChunk.ChunkSize - 1;
    public MapChunk[] MapChunks;
    public int Width;
    public int Height;
    public int ChunksWide;
    public int ChunksHigh;

    // TODO HashSet is overkill here - this could probably be an int array or a bitmap or something
    public readonly HashSet<int> ChangedChunkIndexes = new HashSet<int>();

    public void Init(int width, int height)
    {
        Width = width;
        Height = height;
        ChunksWide = width / ChunkOffset + (width % ChunkOffset > 0 ? 1 : 0);
        ChunksHigh = height / ChunkOffset + (height % ChunkOffset > 0 ? 1 : 0);

        MapChunks = new MapChunk[ChunksWide * ChunksHigh];
        for (var x = 0; x < ChunksWide; x++)
        for (var y = 0; y < ChunksHigh; y++)
        {
            var chunkIndex = y * ChunksWide + x;
            MapChunks[chunkIndex] = new MapChunk();
            Debug.Log("Init new chunk at " + x * ChunkOffset + ", " + y * ChunkOffset);
            MapChunks[chunkIndex].Init(x * ChunkOffset, y * ChunkOffset);
        }
    }

    public bool Get(int x, int y)
    {
        var chunkX = x / ChunkOffset;
        var chunkY = y / ChunkOffset;
        var pixelX = x % ChunkOffset;
        var pixelY = y % ChunkOffset;
        var chunkIndex = chunkY * ChunksWide + chunkX;
        return (MapChunks[chunkIndex].Chunk[pixelY] & 1UL << pixelX) > 0;
    }

    public void Set(int x, int y, bool value)
    {
        var chunkX = x / ChunkOffset;
        var chunkY = y / ChunkOffset;
        var pixelX = x % ChunkOffset;
        var pixelY = y % ChunkOffset;
        SetOnChunk(value, chunkX, chunkY, pixelX, pixelY);

        if (pixelX == 0 && chunkX > 0)
        {
            SetOnChunk(value, chunkX - 1, chunkY, 63, pixelY);
        }
        if (pixelX == ChunkOffset && chunkX < ChunksWide - 1)
        {
            SetOnChunk(value, chunkX + 1, chunkY, 0, pixelY);
        }
        if (pixelY == 0 && chunkY > 0)
        {
            SetOnChunk(value, chunkX, chunkY - 1, pixelX, 63);
        }
        if (pixelY == ChunkOffset && chunkY < ChunksHigh - 1)
        {
            SetOnChunk(value, chunkX, chunkY + 1, pixelX, 0);
        }
    }

    private int SetOnChunk(bool value, int chunkX, int chunkY, int pixelX, int pixelY)
    {
        var chunkIndex = chunkY * ChunksWide + chunkX;
        ChangedChunkIndexes.Add(chunkIndex);
        if (value)
        {
            MapChunks[chunkIndex].Chunk[pixelY] |= 1UL << pixelX;
        }
        else
        {
            MapChunks[chunkIndex].Chunk[pixelY] &= ~(1UL << pixelX);
        }
        return chunkIndex;
    }
}