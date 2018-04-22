using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Random = System.Random;

public static class GeneratesWavyIslandMaps
{
    private class Bitmap
    {
        private const byte LiveBit = (1<<7);
        private const byte NeighbourCountBits = Byte.MaxValue - (1 << 7);
        public int Width;
        public int Height;
        public byte[] CellArray;

        public Bitmap(int width, int height, byte[] cellArray)
        {
            Width = width;
            Height = height;
            CellArray = cellArray;
        }

        public bool Get(int x, int y)
        {
            return (CellArray[y * Width + x] & LiveBit) != 0;
        }

        public byte GetNeighbourCount(int x, int y)
        {
            return (byte) (CellArray[y * Width + x] & NeighbourCountBits);
        }

        public void Set(int x, int y, bool live)
        {
            var current = CellArray[y * Width + x];
            if (((current & LiveBit) != 0) == live)
            {
                return; // state unchanged
            }

            var currentNeighbourCount = current & NeighbourCountBits;
            CellArray[y * Width + x] = (byte)((live ? LiveBit : 0) + currentNeighbourCount);

            SetNeighbourCounts(x, y, live);
        }

        private void SetNeighbourCounts(int x, int y, bool live)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            for (int nx = x - 1; nx <= x + 1; nx++)
            {
                if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                {
                    continue;
                }

                if (!live)
                {
                    CellArray[ny * Width + nx]--;
                }
                else
                {
                    CellArray[ny * Width + nx]++;
                }
            }
        }

        public void Set(int i, bool live)
        {
            Set(i % Width, i/Width, live);
        }
    }

    public static IEnumerator<MapData> GenerateMap(WavyIslandMapGenerationOptions options)
    {
        Debug.Log("GeneratesWavyIslandMaps GenerateMap start with seed " + options.Seed);

        var timer = Stopwatch.StartNew();

        var noiseData = RandomFillMap(options);
        yield return null;

        var fillTime = timer.Lap();

        // TODO: display noise map/bitmap somehow?

        var bitmap = ThresholdMap(noiseData, options);
        var tmpMap = new Bitmap(options.Width, options.Height, new byte[options.Width * options.Height]);

        var thresholdTime = timer.Lap();
        yield return null;

        PickIslands(ref bitmap, ref tmpMap);

        var islandsTime = timer.Lap();
        yield return null;

        for (var i = 0; i < options.DilatePasses; i++)
        {
            Dilate(ref bitmap, ref tmpMap, options);

            yield return null;
        }

        var dilateTime = timer.Lap();

        for (var i = 0; i < options.SmoothPasses; i++)
        {
            Smooth(ref bitmap, ref tmpMap, options);

            yield return null;
        }

        var smoothTime = timer.Lap();

        var map = ToMapData(bitmap);

        var toMapDataTime = timer.Lap();

        Debug.LogFormat(
            "GeneratesWavyIslandMaps GenerateMap done fillTime {0} thresholdTime {1} islandsTime {2} dilateTime {3} smoothTime {4} toMapDataTime {5}",
            fillTime, thresholdTime, islandsTime, dilateTime, smoothTime, toMapDataTime);
        yield return map;
    }

    private static MapData ToMapData(Bitmap bitmap)
    {
        var mapData = new MapData();
        mapData.Init(bitmap.Width, bitmap.Height);
        for (int y = 0; y < bitmap.Height; y++)
        for (int x = 0; x < bitmap.Width; x++)
        {
            mapData.Set(x, y, bitmap.Get(x,y));
        }
        return mapData;
    }

    static byte[] RandomFillMap(WavyIslandMapGenerationOptions options)
    {
        var perlinSeed = new Random(options.Seed).Next();
        var perlinXOffset = perlinSeed & 0xFF;
        var perlinYOffset = perlinSeed >> 15;

        var noiseData = new byte[options.Width * options.Height];

        for (var y = 0; y < options.Height; y++)
        for (var x = 0; x < options.Width; x++)
        {
            if (y == 0)
            {
                noiseData[y*options.Width + x] = 255;
            }
            else
            {
                var perlin = Mathf.PerlinNoise(
                    perlinXOffset + options.PerlinScale * (x / 1.0f),
                    perlinYOffset + options.PerlinScale * (y / 1.0f));

                // apply "turbulence" to the perlin noise
                perlin = Mathf.Abs(perlin - 0.5f) * 2;

                noiseData[y * options.Width + x] = (byte)(perlin * 255);
            }
        }

        return noiseData;
    }

    private static void Swap(ref Bitmap map, ref Bitmap tmpMap)
    {
        var swap = map;
        map = tmpMap;
        tmpMap = swap;
    }

    static Bitmap ThresholdMap(byte[] noiseData, WavyIslandMapGenerationOptions options)
    {
        var threshold = (byte)(options.PerlinThreshold * 255);

        var bitArray = new byte[options.Width * options.Height];

        var thresholdMap = new Bitmap(options.Width, options.Height, bitArray);

        for (int i = 0; i < options.Width * options.Height; i++)
        {
            thresholdMap.Set(i, noiseData[i] > threshold);
        }

        return thresholdMap;
    }

    static void FloodFill(Bitmap sourceMap, Bitmap targetMap, bool sourceValue, bool targetValue, int startx, int starty)
    {
        if (startx < 0 || startx >= sourceMap.Width || starty < 0 || starty >= sourceMap.Width)
        {
            Debug.LogWarning("FloodFill off the edge of the map");
            return;
        }
        if (sourceMap.Width != targetMap.Width || sourceMap.Height != targetMap.Height)
        {
            Debug.LogWarning("maps different sizes");
            return;
        }
        if (sourceMap.Get(startx, starty) != sourceValue)
        {
            //Debug.Log("FloodFill sourceMap at " + startx + "," + starty + " is not " + sourceValue);
            return;
        }
        if (targetMap.Get(startx, starty) == targetValue)
        {
            //Debug.Log("FloodFill targetMap at " + startx + "," + starty + " is already " + targetValue);
            return;
        }

        targetMap.Set(startx, starty, targetValue);

        var q = new Queue<Coordinate>();
        q.Enqueue(Coord(startx, starty));
        while (q.Any())
        {
            // TODO this probably won't be significantly slow, but if it is try optimisation from wp:
            // Most practical implementations use a loop for the west and east directions as an optimization to avoid the overhead of stack or queue management

            var next = q.Dequeue();
            var x = next.X;
            var y = next.Y;
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x + 1, y);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x - 1, y);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x, y + 1);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x, y - 1);
        }
    }

    private static void Fill(Bitmap sourceMap, Bitmap targetMap, bool sourceValue, bool targetValue, Queue<Coordinate> q, int x, int y)
    {
        if (x < 0 || x >= sourceMap.Width || y < 0 || y >= sourceMap.Height) { return; }
        if (sourceMap.Get(x, y) == sourceValue && targetMap.Get(x, y) != targetValue)
        {
            targetMap.Set(x, y, targetValue);
            q.Enqueue(Coord(x, y));
        }
    }

    private struct Coordinate
    {
        public int X;
        public int Y;
    }

    private static Coordinate Coord(int x, int y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static void Dilate(ref Bitmap map, ref Bitmap tmpMap, WavyIslandMapGenerationOptions options)
    {
        map.CellArray.CopyTo(tmpMap.CellArray, 0);

        for (var y = 0; y < options.Height; y++)
        for (var x = 0; x < options.Width; x++)
        {
            var currentlyLive = map.Get(x, y);
            var neighbourWallTiles = map.GetNeighbourCount(x, y);
            if (!currentlyLive && neighbourWallTiles > 1)
            {
                tmpMap.Set(x, y, true);
            }
        }

        Swap(ref map, ref tmpMap);
    }

    private static void Smooth(ref Bitmap map, ref Bitmap tmpMap, WavyIslandMapGenerationOptions options)
    {
        map.CellArray.CopyTo(tmpMap.CellArray, 0);

        for (var y = 0; y < options.Height; y++)
        for (var x = 0; x < options.Width; x++)
        {
            var neighbourWallTiles = map.GetNeighbourCount(x, y);
            if (neighbourWallTiles > 4)
            {
                tmpMap.Set(x, y, true);
            }
            else if (neighbourWallTiles < 4)
            {
                tmpMap.Set(x, y, false);
            }
        }

        Swap(ref map, ref tmpMap);
    }

    static void PickIslands(ref Bitmap map, ref Bitmap tmpMap)
    {
        var fillLineHeights = new[] { 20, 50, 100, 200 };
        foreach (var fillLineHeight in fillLineHeights)
        {
            for (var x = 0; x < map.Width; x++)
            {
                FloodFill(map, tmpMap, true, true, x, fillLineHeight);
            }
        }

        Swap(ref map, ref tmpMap);
    }
}