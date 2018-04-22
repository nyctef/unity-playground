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
        public int Width;
        public int Height;
        public BitArray BitArray;

        public Bitmap(int width, int height, BitArray bitArray)
        {
            Width = width;
            Height = height;
            BitArray = bitArray;
        }

        public bool Get(int x, int y)
        {
            return BitArray[y * Width + x];
        }

        public void Set(int x, int y, bool value)
        {
            BitArray[y * Width + x] = value;
        }
    }

    public static IEnumerator<MapData> GenerateMap(WavyIslandMapGenerationOptions options)
    {
        Debug.Log("GeneratesWavyIslandMaps GenerateMap start with seed " + options.Seed);

        var timer = Stopwatch.StartNew();

        var noiseData = RandomFillMap(options);

        var fillTime = timer.Lap();

        // TODO: display noise map/bitmap somehow?

        var bitmap = ThresholdMap(noiseData, options);

        var thresholdTime = timer.Lap();
        yield return null;

        var tmpMap = new Bitmap(options.Width, options.Height, new BitArray(options.Width * options.Height));

        PickIslands(ref bitmap, ref tmpMap);

        var islandsTime = timer.Lap();
        yield return null;

        Profiler.BeginSample("Dilate");
        for (var i = 0; i < options.DilatePasses; i++)
        {
            Dilate(ref bitmap, ref tmpMap, true, options);

            yield return null;
        }
        Profiler.EndSample();

        var dilateTime = timer.Lap();

        Profiler.BeginSample("Smooth");
        for (var i = 0; i < options.SmoothPasses; i++)
        {
            Smooth(ref bitmap, ref tmpMap, true, options);

            yield return null;
        }
        Profiler.EndSample();

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
            mapData.Set(x, y, bitmap.BitArray[bitmap.Width*y+x]);
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

        var bitArray = new BitArray(options.Width * options.Height);

        for (int i = 0; i < options.Width * options.Height; i++)
        {
            bitArray[i] = noiseData[i] > threshold;
        }

        return new Bitmap(options.Width, options.Height, bitArray);
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

    static void Dilate(ref Bitmap map, ref Bitmap tmpMap, bool targetValue, WavyIslandMapGenerationOptions options)
    {
        for (var y = 0; y < options.Height; y++)
        for (var x = 0; x < options.Width; x++)
        {
            var neighbourWallTiles = GetSurroundingWallCount(map, x, y, targetValue);
            tmpMap.Set(x, y, neighbourWallTiles > 1 ? targetValue : map.Get(x, y));
        }

        Swap(ref map, ref tmpMap);
    }

    static void Smooth(ref Bitmap map, ref Bitmap tmpMap, bool targetValue, WavyIslandMapGenerationOptions options)
    {
        for (var y = 0; y < options.Height; y++)
        for (var x = 0; x < options.Width; x++)
        {
            var neighbourWallTiles = GetSurroundingWallCount(map, x, y, targetValue);
            if (neighbourWallTiles > 4)
            {
                tmpMap.Set(x, y, targetValue);
            }
            else if (neighbourWallTiles < 4)
            {
                tmpMap.Set(x, y, false);
            }
            else
            {
                tmpMap.Set(x, y, map.Get(x, y));
            }
        }

        Swap(ref map, ref tmpMap);
    }

    static int GetSurroundingWallCount(Bitmap map, int x, int y, bool targetValue)
    {
        var wallCount = 0;
        for (var nY = y - 1; nY <= y + 1; nY++)
        for (var nX = x - 1; nX <= x + 1; nX++)
        {
            if (nX == nY)
            {
                continue;
            }
            if (nX < 0 || nX >= map.Width || nY < 0 || nY >= map.Height)
            {
                if (nY < 0) { wallCount++; }
            }
            else
            {
                wallCount += map.Get(nX, nY) == targetValue ? 1 : 0;
            }
        }
        return wallCount;
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