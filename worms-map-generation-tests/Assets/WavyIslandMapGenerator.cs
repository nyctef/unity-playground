using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;
using Random = System.Random;
using System.Xml.Linq;

public class WavyIslandMapGenerator : MonoBehaviour {

    public int Width = 512;
    public int Height = 128;

    [Header("Generation settings")]

    public int Seed;
    public bool UseRandomSeed = true;

    [Range(0.01f, 0.99f)] public float PerlinThreshold = 0.2f;
    [Range(0.1f, 20f)] public float PerlinScale = 0.05f;

    [Range(0,5)]
    public int DilatePasses = 3;
    [Range(0,5)]
    public int SmoothPasses = 3;

    [Header("Generation animation settings")]

    [Range(0.0f, 1f)] public float AnimationDelay = 0.25f;
    public bool ShowNoiseGeneration = true;
    //public bool ShowMeshGeneration = true;

    private byte[,] _map;

    void Start()
    {
        StartCoroutine(GenerateMap());
    }

    void Update()
    {
        // TODO trigger this with something else (ui / function key?)
        // probably going to need a more general map/game manager at some point
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

        if (ShowNoiseGeneration) { yield return new WaitForSeconds(AnimationDelay); }

        Debug.Log("fill map");
        RandomFillMap(ref _map);

        if (ShowNoiseGeneration) { yield return new WaitForSeconds(AnimationDelay); }

        Debug.Log("threshold map");
        ThresholdMap(ref _map);

        if (ShowNoiseGeneration) { yield return new WaitForSeconds(AnimationDelay); }

        var tmpMap = new byte[Width, Height];

        Debug.Log("pick islands");
        PickIslands(ref _map, ref tmpMap);

        if (ShowNoiseGeneration) { yield return new WaitForSeconds(AnimationDelay); }

        for (int i = 0; i < DilatePasses; i++)
        {
            Debug.Log("dilate");
            Dilate(ref _map, ref tmpMap, 128);

            if (ShowNoiseGeneration) { yield return new WaitForSeconds(AnimationDelay); }
        }

        for (int i = 0; i < SmoothPasses; i++)
        {
            Debug.Log("smooth");
            Smooth(ref _map, ref tmpMap, 128);

            if (ShowNoiseGeneration) { yield return new WaitForSeconds(AnimationDelay); }
        }

        foreach (var child in transform.Cast<Transform>().ToList())
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Writing map to mesh");
        var collisionMesh = new Mesh();
        WriteMapToCollisionMesh(_map, collisionMesh);

        var mapCollision = new GameObject("MapCollision", typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer));
        mapCollision.transform.SetParent(transform, false);
        var meshFilter = mapCollision.RequireComponent<MeshFilter>();
        meshFilter.mesh = collisionMesh;
        var meshCollider = mapCollision.RequireComponent<MeshCollider>();
        meshCollider.sharedMesh = collisionMesh;

        var displayMesh = new Mesh();
        WriteMapToDisplayMesh(_map, displayMesh);

        var mapDisplay = new GameObject("MapDisplay", typeof(MeshFilter), typeof(MeshRenderer));
        mapDisplay.transform.SetParent(transform, false);
        var displayMeshFilter = mapDisplay.RequireComponent<MeshFilter>();
        displayMeshFilter.mesh = displayMesh;
        var texture = GetMapTexture();
        var mapRenderer = mapDisplay.RequireComponent<Renderer>();
        mapRenderer.material.mainTexture = texture;


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

    void Smooth(ref byte[,] map, ref byte[,] tmpMap, byte targetValue)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var neighbourWallTiles = GetSurroundingWallCount(map, x, y, targetValue);
                if (neighbourWallTiles > 4)
                {
                    tmpMap[x, y] = targetValue;
                }
                else if (neighbourWallTiles < 4)
                {
                    tmpMap[x, y] = 0;
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

    private bool IsSolidAt(byte[,] mapData, int sx, int x, int y)
    {
        return mapData[x, y] > 0;
    }

    private void WriteMapToCollisionMesh(byte[,] map, Mesh mesh)
    {
        var sx = Width;
        //auto sy = (int32)Size.Y;
        var sz = Height;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        var Left = 1 + -Width / 2;
        var Bottom = 1 + -Height / 2;
        var Right = 1 + Width / 2;
        var Top = 1 + Height / 2;

        // ref: https://en.wikipedia.org/wiki/Marching_squares
        for (int mapZ = 0; mapZ < sz - 1; mapZ++)
        {
            for (int mapX = 0; mapX < sx - 1; mapX++)
            {
                var cell = 0;

                if (IsSolidAt(map, sx, mapX, mapZ)) { cell += 1; }
                if (IsSolidAt(map, sx, mapX + 1, mapZ)) { cell += 2; }
                if (IsSolidAt(map, sx, mapX + 1, mapZ + 1)) { cell += 4; }
                if (IsSolidAt(map, sx, mapX, mapZ + 1)) { cell += 8; }

                // TODO: use correct axes so that we don't have to rotate this 90deg

                var cellLeftInner = new Vector3(Left + mapX - 0.5f, 1, Bottom + mapZ);
                var cellLeftOuter = new Vector3(Left + mapX - 0.5f, 0, Bottom + mapZ);
                var cellBottomInner = new Vector3(Left + mapX, 1, Bottom + mapZ - 0.5f);
                var cellBottomOuter = new Vector3(Left + mapX, 0, Bottom + mapZ - 0.5f);
                var cellRightInner = new Vector3(Left + mapX + 0.5f, 1, Bottom + mapZ);
                var cellRightOuter = new Vector3(Left + mapX + 0.5f, 0, Bottom + mapZ);
                var cellTopInner = new Vector3(Left + mapX, 1, Bottom + mapZ + 0.5f);
                var cellTopOuter = new Vector3(Left + mapX, 0, Bottom + mapZ + 0.5f);

                // +8  +4
                //
                // +1  +2
                switch (cell)
                {
                    case 0: // 0b0000:
                        break;
                    case 1: // 0b0001:
                        BuildQuad(vertices, triangles, cellBottomInner, cellLeftInner, cellLeftOuter, cellBottomOuter);
                        break;
                    case 2: // 0b0010:
                        BuildQuad(vertices, triangles, cellBottomInner, cellBottomOuter, cellRightOuter, cellRightInner);
                        break;
                    case 3: // 0b0011:
                        BuildQuad(vertices, triangles, cellRightInner, cellLeftInner, cellLeftOuter, cellRightOuter);
                        break;
                    case 4: // 0b0100:
                        BuildQuad(vertices, triangles, cellRightInner, cellRightOuter, cellTopOuter, cellTopInner);
                        break;
                    case 5: // 0b0101:
                        BuildQuad(vertices, triangles, cellRightInner, cellRightOuter, cellBottomOuter, cellBottomInner);
                        BuildQuad(vertices, triangles, cellLeftInner, cellLeftOuter, cellTopOuter, cellTopInner);
                        break;
                    case 6: // 0b0110:
                        BuildQuad(vertices, triangles, cellTopInner, cellBottomInner, cellBottomOuter, cellTopOuter);
                        break;
                    case 7: // 0b0111:
                        BuildQuad(vertices, triangles, cellLeftInner, cellLeftOuter, cellTopOuter, cellTopInner);
                        break;
                    case 8: // 0b1000:
                        BuildQuad(vertices, triangles, cellLeftInner, cellTopInner, cellTopOuter, cellLeftOuter);
                        break;
                    case 9: // 0b1001:
                        BuildQuad(vertices, triangles, cellTopInner, cellTopOuter, cellBottomOuter, cellBottomInner);
                        break;
                    case 10:// 0b1010:
                        BuildQuad(vertices, triangles, cellBottomInner, cellBottomOuter, cellLeftOuter, cellLeftInner);
                        BuildQuad(vertices, triangles, cellRightInner, cellRightOuter, cellTopOuter, cellTopInner);
                        break;
                    case 11: // 0b1011:
                        BuildQuad(vertices, triangles, cellRightInner, cellTopInner, cellTopOuter, cellRightOuter);
                        break;
                    case 12: // 0b1100:
                        BuildQuad(vertices, triangles, cellRightInner, cellRightOuter, cellLeftOuter, cellLeftInner);
                        break;
                    case 13: // 0b1101:
                        BuildQuad(vertices, triangles, cellRightInner, cellRightOuter, cellBottomOuter, cellBottomInner);
                        break;
                    case 14: // 0b1110:
                        BuildQuad(vertices, triangles, cellBottomInner, cellBottomOuter, cellLeftOuter, cellLeftInner);
                        break;
                    case 15: // 0b1111:
                        break;
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    private void WriteMapToDisplayMesh(byte[,] map, Mesh mesh)
    {
        var sx = Width;
        //auto sy = (int32)Size.Y;
        var sz = Height;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        BuildQuad(vertices, triangles, new Vector3(), new Vector3(0, 0, Height), new Vector3(Width, 0, Height), new Vector3(Width, 0, 0));

        var uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv;
        mesh.RecalculateNormals();
    }

    private void BuildQuad(List<Vector3> vertices, List<int> triangles, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int index = vertices.Count - 1;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
        triangles.Add(index + 1);
        triangles.Add(index + 3);
        triangles.Add(index + 4);
    }

    private Texture2D GetMapTexture()
    {
        var texture = new Texture2D(Width, Height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;

        Color32[] newColors = new Color32[Width * Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (_map[x, y] == 0) { newColors[y * Width + x] = new Color32(0, 0, 0, 0); }
                else { newColors[y * Width + x] = new Color32(255, 0, 0, 255); }
            }
        texture.SetPixels32(newColors);
        texture.Apply();

        return texture;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        if (_map == null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(Vector3.zero, new Vector3(Width, 0, Height));
        }
    }
}
