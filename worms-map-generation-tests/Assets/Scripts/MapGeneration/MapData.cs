using System.Collections.Generic;

public class MapData
{
    public MapChunk[] MapChunks;
    public int Width;
    public int Height;
    public readonly int ChunkSize = 64;
    public int ChunksWide;
    public int ChunksHigh;

    public HashSet<int> ChangedChunkIndexes = new HashSet<int>();

    public void Init(int width, int height)
    {
        Width = width;
        Height = height;
        ChunksWide = width / ChunkSize + (width % ChunkSize > 0 ? 1 : 0);
        ChunksHigh = height / ChunkSize + (height % ChunkSize > 0 ? 1 : 0);

        MapChunks = new MapChunk[ChunksWide * ChunksHigh];
        for (int x = 0; x < ChunksWide; x++)
        for (int y = 0; y < ChunksHigh; y++)
        {
            int i = y * ChunksWide + x;
            MapChunks[i] = new MapChunk();
            MapChunks[i].Init(ChunkSize, ChunkSize, x * ChunkSize, y * ChunkSize);
        }
    }

    public bool Get(int x, int y)
    {
        var chunkX = x / ChunkSize;
        var chunkY = y / ChunkSize;
        var pixelX = x % ChunkSize;
        var pixelY = y % ChunkSize;
        return (MapChunks[chunkY * ChunksWide + chunkX].Chunk[pixelY] & 1UL << pixelX) > 0;
    }

    public void Set(int x, int y, bool value)
    {
        var chunkX = x / ChunkSize;
        var chunkY = y / ChunkSize;
        var pixelX = x % ChunkSize;
        var pixelY = y % ChunkSize;
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
    }
}