using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;
using Random = System.Random;
using System.Xml.Linq;
using UnityEngine.Rendering;

public class WavyIslandMapGenerator : MonoBehaviour, ISerializationCallbackReceiver {

    public int Width = 512;
    public int Height = 128;

    public Material MapMaterial;

    [Header("Generation settings")]

    public int Seed;
    public bool UseRandomSeed = true;

    [Range(0.01f, 0.99f)] public float PerlinThreshold = 0.2f;
    [Range(0.01f, 1f)] public float PerlinScale = 0.05f;

    [Range(0,5)]
    public int DilatePasses = 3;
    [Range(0,5)]
    public int SmoothPasses = 3;

    [Header("Generation animation settings")]

    [Range(0.0f, 1f)] public float AnimationDelay = 0.25f;
    public bool ShowNoiseGeneration = true;
    //public bool ShowMeshGeneration = true;

    private byte[,] _map;
    [SerializeField] private byte[] _serializedMap;

    public void OnBeforeSerialize()
    {
        _serializedMap = new byte[Width * Height];
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            _serializedMap[y * Width + x] = _map[x, y];
        }
    }

    public void OnAfterDeserialize()
    {
        _map = new byte[Width, Height];
        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        {
            _map[x,y] = _serializedMap[y*Width + x];
        }
    }

    void Start()
    {
        StartCoroutine(GenerateMap());
    }

    void OnEnable()
    {
        EventManager.Instance.StartListening<Events.Explosion>(OnExplosion);
    }

    void OnDisable()
    {
        EventManager.Instance.StopListening<Events.Explosion>(OnExplosion);
    }

    private void OnExplosion(Events.Explosion explosion)
    {
        RemoveCircle(_map, transform.InverseTransformPoint(explosion.worldSpacePosition), explosion.radius);
    }

    private void RemoveCircle(byte[,] map, Vector3 localSpace, int explosionRadius)
    {
        int pixelsCleared = 0;
        for (int ex = -explosionRadius; ex < +explosionRadius; ex++)
        for (int ey = -explosionRadius; ey < +explosionRadius; ey++)
        {
            var x = (int)localSpace.x + ex;
            var y = (int)localSpace.y + ey;
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                continue;
            }
            if (ex*ex + ey*ey > explosionRadius * explosionRadius)
            {
                continue;
            }

            map[x, y] = 0;
            pixelsCleared++;
        }

        ClearChildren();
        AddCollisionMesh();
        AddDisplayMesh();

        Debug.Log("RemoveCircle " + localSpace + " " + explosionRadius + " pixelsCleared: "+pixelsCleared);
    }

    void Update()
    {
        // TODO probably going to need a more general map/game manager at some point - UI trigger?
        if (Input.GetButtonDown("RegenMap"))
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

        if (ShowNoiseGeneration) { yield return AnimationPause(); }

        Debug.Log("fill map");
        RandomFillMap(ref _map);

        if (ShowNoiseGeneration) { yield return AnimationPause(); }

        Debug.Log("threshold map");
        ThresholdMap(ref _map);

        if (ShowNoiseGeneration) { yield return AnimationPause(); }

        var tmpMap = new byte[Width, Height];

        Debug.Log("pick islands");
        PickIslands(ref _map, ref tmpMap);

        if (ShowNoiseGeneration) { yield return AnimationPause(); }

        for (int i = 0; i < DilatePasses; i++)
        {
            Debug.Log("dilate");
            Dilate(ref _map, ref tmpMap, 128);

            if (ShowNoiseGeneration) { yield return AnimationPause(); }
        }

        for (int i = 0; i < SmoothPasses; i++)
        {
            Debug.Log("smooth");
            Smooth(ref _map, ref tmpMap, 128);

            if (ShowNoiseGeneration) { yield return AnimationPause(); }
        }

        ClearChildren();
        AddCollisionMesh();
        AddDisplayMesh();

        Debug.Log("done");
    }

    private WaitForSeconds AnimationPause()
    {
        ClearChildren();
        AddDisplayMesh();
        return new WaitForSeconds(AnimationDelay);
    }

    private void AddDisplayMesh()
    {
        var displayMesh = new Mesh();
        CreateDisplayMesh(displayMesh);

        var mapDisplay = new GameObject("MapDisplay", typeof(MeshFilter), typeof(MeshRenderer));
        mapDisplay.transform.SetParent(transform, false);
        var displayMeshFilter = mapDisplay.RequireComponent<MeshFilter>();
        displayMeshFilter.mesh = displayMesh;
        var texture = GetMapTexture();
        var mapRenderer = mapDisplay.RequireComponent<Renderer>();
        mapRenderer.material = MapMaterial;
        mapRenderer.material.mainTexture = texture;
    }

    private void AddCollisionMesh()
    {
        Debug.Log("Writing map to mesh");
        var collisionMesh = new Mesh {indexFormat = IndexFormat.UInt32};
        WriteMapToCollisionMesh(_map, collisionMesh);

        var mapCollision =
            new GameObject("MapCollision", typeof(MeshFilter), typeof(MeshCollider) /*, typeof(MeshRenderer)*/);
        mapCollision.transform.SetParent(transform, false);
        var meshFilter = mapCollision.RequireComponent<MeshFilter>();
        meshFilter.mesh = collisionMesh;
        var meshCollider = mapCollision.RequireComponent<MeshCollider>();
        meshCollider.sharedMesh = collisionMesh;
    }

    private void ClearChildren()
    {
        foreach (var child in transform.Cast<Transform>().ToList())
        {
            Destroy(child.gameObject);
        }
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
                    perlinXOffset + PerlinScale * (x / 1.0f),
                    perlinYOffset + PerlinScale * (y / 1.0f));

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
        var fillLineHeights = new[] {20, 50, 100, 200};
        foreach (var fillLineHeight in fillLineHeights)
        {
            for (var x = 0; x < Width; x++)
            {
                FloodFill(map, tmpMap, 255, 128, x, fillLineHeight);
            }
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
        Debug.Log("WriteMapToCollisionMesh " + Width + " " + Height);

        var sx = Width;
        //auto sy = (int32)Size.Y;
        var sy = Height;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        var left = 1;
        var bottom = 1;

        // ref: https://en.wikipedia.org/wiki/Marching_squares
        for (int mapY = 0; mapY < sy - 1; mapY++)
        {
            for (int mapX = 0; mapX < sx - 1; mapX++)
            {
                var cell = 0;

                if (IsSolidAt(map, sx, mapX, mapY)) { cell += 1; }
                if (IsSolidAt(map, sx, mapX + 1, mapY)) { cell += 2; }
                if (IsSolidAt(map, sx, mapX + 1, mapY + 1)) { cell += 4; }
                if (IsSolidAt(map, sx, mapX, mapY + 1)) { cell += 8; }

                var cellLeftInner   = new Vector3(left + mapX - 0.5f, bottom + mapY       , -1);
                var cellLeftOuter   = new Vector3(left + mapX - 0.5f, bottom + mapY       , +1);
                var cellBottomInner = new Vector3(left + mapX,        bottom + mapY - 0.5f, -1);
                var cellBottomOuter = new Vector3(left + mapX,        bottom + mapY - 0.5f, +1);
                var cellRightInner  = new Vector3(left + mapX + 0.5f, bottom + mapY       , -1);
                var cellRightOuter  = new Vector3(left + mapX + 0.5f, bottom + mapY       , +1);
                var cellTopInner    = new Vector3(left + mapX,        bottom + mapY + 0.5f, -1);
                var cellTopOuter    = new Vector3(left + mapX,        bottom + mapY + 0.5f, +1);

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

    private void CreateDisplayMesh(Mesh mesh)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        BuildQuad(vertices, triangles, new Vector3(), new Vector3(0, Height), new Vector3(Width, Height), new Vector3(Width, 0));

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
                var mapValue = _map[x, y];
                if (mapValue == 0) { newColors[y * Width + x] = new Color32(0, 0, 0, 0); }
                else { newColors[y * Width + x] = new Color32(mapValue, mapValue, mapValue, 255); }
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
            Gizmos.DrawCube(new Vector3(Width/2, Height/2), new Vector3(Width, Height));
        }
    }
}
