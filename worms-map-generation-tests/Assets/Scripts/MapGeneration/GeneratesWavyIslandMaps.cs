using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class GeneratesWavyIslandMaps
{
    public static IEnumerator<MapData> GenerateMap(WavyIslandMapGenerationOptions options)
    {
        Debug.Log("WavyIslandMapGenerator GenerateMap with seed " + options.Seed);

        Debug.Log("map create");


        Debug.Log("fill map");
        var noiseData = RandomFillMap(options);

        // TODO: display noise map somehow?

        Debug.Log("threshold map");
        var map = ThresholdMap(noiseData, options);

        yield return map;

        var tmpMap = new MapData();
        tmpMap.Init(options.Width, options.Height);

        Debug.Log("pick islands");
        PickIslands(ref map, ref tmpMap);

        yield return map;

        for (int i = 0; i < options.DilatePasses; i++)
        {
            Debug.Log("dilate");
            Dilate(ref map, ref tmpMap, true, options);

            yield return map;
        }

        for (int i = 0; i < options.SmoothPasses; i++)
        {
            Debug.Log("smooth");
            Smooth(ref map, ref tmpMap, true, options);

            yield return map;
        }

        Debug.Log("done");
        yield return map;
    }

    static byte[] RandomFillMap(WavyIslandMapGenerationOptions options)
    {
        int perlinSeed = new Random(options.Seed).Next();
        int perlinXOffset = perlinSeed & 0xFF;
        int perlinYOffset = perlinSeed >> 15;

        var noiseData = new byte[options.Width * options.Height];

        for (int x = 0; x < options.Width; x++)
        for (int y = 0; y < options.Height; y++)
        {
            if (y == 0)
            {
                noiseData[y*options.Width + x] = 255;
            }
            else
            {
                float perlin = Mathf.PerlinNoise(
                    perlinXOffset + options.PerlinScale * (x / 1.0f),
                    perlinYOffset + options.PerlinScale * (y / 1.0f));

                // apply "turbulence" to the perlin noise
                perlin = Mathf.Abs(perlin - 0.5f) * 2;

                noiseData[y * options.Width + x] = (byte)(perlin * 255);
            }
        }

        return noiseData;
    }

    private static void Swap(ref MapData map, ref MapData tmpMap)
    {
        var swap = map;
        map = tmpMap;
        tmpMap = swap;
    }

    static MapData ThresholdMap(byte[] noiseData, WavyIslandMapGenerationOptions options)
    {
        var map = new MapData();
        map.Init(options.Width, options.Height);

        var threshold = (byte)(options.PerlinThreshold * 255);

        for (int x = 0; x < options.Width; x++)
        for (int y = 0; y < options.Height; y++)
        {
            if (noiseData[y*options.Width + x] > threshold)
            {
                map.Set(x, y, true);
            }
            else
            {
                map.Set(x, y, false);
            }
        }

        return map;
    }

    static void FloodFill(MapData sourceMap, MapData targetMap, bool sourceValue, bool targetValue, int startx, int starty)
    {
        if (startx < 0 || startx >= sourceMap.Width || starty < 0 || starty >= sourceMap.Width)
        {
            Debug.LogWarning("FloodFill off the edge of the map");
            return;
        }
        //if (sourceValue == targetValue)
        //{
        //    Debug.LogWarning("FloodFill source==targetvalue");
        //    return;
        //}
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
            var x = next.x;
            var y = next.y;
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x + 1, y);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x - 1, y);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x, y + 1);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x, y - 1);
        }
    }

    private static void Fill(MapData sourceMap, MapData targetMap, bool sourceValue, bool targetValue, Queue<Coordinate> q, int x, int y)
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
        public int x;
        public int y;
    }

    private static Coordinate Coord(int x, int y)
    {
        return new Coordinate { x = x, y = y };
    }

    static void Dilate(ref MapData map, ref MapData tmpMap, bool targetValue, WavyIslandMapGenerationOptions options)
    {
        for (int x = 0; x < options.Width; x++)
        for (int y = 0; y < options.Height; y++)
        {
            var neighbourWallTiles = GetSurroundingWallCount(map, x, y, targetValue);
            if (neighbourWallTiles > 1)
            {
                tmpMap.Set(x, y, targetValue);
            }
            else
            {
                tmpMap.Set(x, y, map.Get(x, y));
            }
        }

        Swap(ref map, ref tmpMap);
    }

    static void Smooth(ref MapData map, ref MapData tmpMap, bool targetValue, WavyIslandMapGenerationOptions options)
    {
        for (int x = 0; x < options.Width; x++)
        for (int y = 0; y < options.Height; y++)
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

    static int GetSurroundingWallCount(MapData map, int x, int y, bool targetValue)
    {
        int wallCount = 0;
        for (int nX = x - 1; nX <= x + 1; nX++)
        for (int nY = y - 1; nY <= y + 1; nY++)
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

    static void PickIslands(ref MapData map, ref MapData tmpMap)
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