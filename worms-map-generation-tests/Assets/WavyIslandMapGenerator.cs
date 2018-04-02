using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;
using Random = System.Random;

public class WavyIslandMapGenerator : MonoBehaviour {

    public int Width = 128;
    public int Height = 128;

    public int Seed;
    public bool UseRandomSeed = true;

    [Range(0.01f, 0.99f)] public float PerlinThreshold = 0.2f;
    [Range(0.01f, 0.2f)] public float PerlinScale = 0.1f;

    [Range(0.0f, 1f)] public float AnimationDelay = 0.25f;
    public bool ShowAnimation { get { return AnimationDelay > 0.0f; } }

    private byte[,] _map;

    void Start()
    {
        StartCoroutine(GenerateMap());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(GenerateMap());
        }
    }

    IEnumerator GenerateMap()
    {
        if (UseRandomSeed)
        {
            Seed = (int)(Time.time * 1000);
        }

        Debug.Log("WavyIslandMapGenerator GenerateMap with seed " + Seed, this);

        Debug.Log("map create");
        _map = new byte[Width, Height];

        if (ShowAnimation) { yield return new WaitForSeconds(AnimationDelay); }

        Debug.Log("fill map");
        RandomFillMap(ref _map);

        if (ShowAnimation) { yield return new WaitForSeconds(AnimationDelay); }

        Debug.Log("threshold map");
        ThresholdMap(ref _map);

        if (ShowAnimation) { yield return new WaitForSeconds(AnimationDelay); }

        var tmpMap = new byte[Width, Height];

        Debug.Log("pick islands");
        PickIslands(ref _map, ref tmpMap);

        if (ShowAnimation) { yield return new WaitForSeconds(AnimationDelay); }

        Debug.Log("dilate");
        Dilate(ref _map, ref tmpMap, 128);

        Debug.Log("done");
    }

    void RandomFillMap(ref byte[,] map)
    {
        int perlinSeed = new Random(Seed).Next();
        int perlinXOffset = perlinSeed & 0xFF;
        int perlinYOffset = perlinSeed >> 15;

        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            if (y == 0)
            {
                map[x, y] = 255;
            }
            else
            {
                float perlin = Mathf.PerlinNoise(
                    perlinXOffset + (PerlinScale * x / 1.0f),
                    perlinYOffset + (PerlinScale * y / 1.0f));

                    // apply "turbulence" to the perlin noise
                    perlin = Mathf.Abs(perlin - 0.5f) * 2;

                map[x, y] = (byte)(perlin * 255);
            }
        }
    }

    void ThresholdMap(ref byte[,] map)
    {
        var threshold = (byte)(PerlinThreshold * 255);

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (_map[x, y] > threshold)
                {
                    _map[x, y] = 255;
                }
                else
                {
                    _map[x, y] = 0;
                }
            }
    }

    void PickIslands(ref byte[,] map, ref byte[,] tmpMap)
    {
        var firstLineHeight = 12;
        for (var x = 0; x < Width; x++)
        {
            FloodFill(map, tmpMap, 255, 128, x, firstLineHeight);
        }

        Swap(ref map, ref tmpMap);
    }

    private static void Swap(ref byte[,] map, ref byte[,] tmpMap)
    {
        var swap = map;
        map = tmpMap;
        tmpMap = swap;
    }

    private struct Coordinate
    {
        public int x;
        public int y;
    }

    private Coordinate Coord(int x, int y)
    {
        return new Coordinate { x = x, y = y };
    }

    void FloodFill(byte[,] sourceMap, byte[,] targetMap, byte sourceValue, byte targetValue, int startx, int starty)
    {
        if (startx < 0 || startx >= Width || starty < 0 || starty >= Width)
        {
            Debug.Log("FloodFill off the edge of the map");
            return;
        }
        if (sourceValue == targetValue)
        {
            Debug.Log("FloodFill source==targetvalue");
            return;
        }
        if (sourceMap[startx, starty] != sourceValue)
        {
            //Debug.Log("FloodFill sourceMap at " + startx + "," + starty + " is not " + sourceValue);
            return;
        }
        if (targetMap[startx,starty] == targetValue)
        {
            //Debug.Log("FloodFill targetMap at " + startx + "," + starty + " is already " + targetValue);
            return;
        }

        targetMap[startx, starty] = targetValue;

        var q = new Queue<Coordinate>();
        q.Enqueue(Coord(startx,starty));
        while (q.Any())
        {
            // TODO this probably won't be significantly slow, but if it is try optimisation from wp:
            // Most practical implementations use a loop for the west and east directions as an optimization to avoid the overhead of stack or queue management

            var next = q.Dequeue();
            var x = next.x;
            var y = next.y;
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x+1, y);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x-1, y);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x, y+1);
            Fill(sourceMap, targetMap, sourceValue, targetValue, q, x, y-1);
        }
    }

    private void Fill(byte[,] sourceMap, byte[,] targetMap, byte sourceValue, byte targetValue, Queue<Coordinate> q, int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) { return; }
        if (sourceMap[x, y] == sourceValue && targetMap[x, y] != targetValue)
        {
            targetMap[x, y] = targetValue;
            q.Enqueue(Coord(x, y));
        }
    }

    void Dilate(ref byte[,] map, ref byte[,] tmpMap, byte targetValue)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var neighbourWallTiles = GetSurroundingWallCount(map, x, y, targetValue);
                if (neighbourWallTiles > 1)
                {
                    tmpMap[x, y] = targetValue;
                }
                else
                {
                    tmpMap[x, y] = map[x, y];
                }
            }

        Swap(ref map, ref tmpMap);
    }

    int GetSurroundingWallCount(byte[,] map, int x, int y, byte targetValue)
    {
        int wallCount = 0;
        for (int nX = x - 1; nX <= x + 1; nX++)
            for (int nY = y - 1; nY <= y + 1; nY++)
            {
                if (nX == nY)
                {
                    continue;
                }
                if (nX < 0 || nX >= Width || nY < 0 || nY >= Height)
                {
                    if (nY < 0) { wallCount++; }
                }
                else
                {
                    wallCount += map[nX, nY] == targetValue ? 1 : 0;
                }
            }
        return wallCount;
    }

    void OnDrawGizmos()
    {
        if (_map != null)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var color = ((float)_map[x, y]) / 255f;
                    Gizmos.color = new Color(color, color, color);
                    var pos = new Vector3(-Width / 2 + x + .5f, 0, -Height / 2 + y + .5f);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(Vector3.zero, new Vector3(Width, 0, Height));
        }
    }
}
