using System.Collections;
using System.Globalization;
using UnityEngine;
using Random = System.Random;

public class CavesMapGenerator : MonoBehaviour
{
    public int Width = 128;
    public int Height = 128;

    public string Seed;
    public bool UseRandomSeed;

    [Range(0, 100)] public int RandomFillPercent = 45;

    [Range(0.0f, 1f)] public float AnimationDelay = 0.25f;
    public bool ShowAnimation { get { return AnimationDelay > 0.0f; } }

    private int[,] _map;

    void Start()
    {
        StartCoroutine(GenerateMap());
    }

    void Update()
    {
        // TODO trigger this with something else (ui / function key?)
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(GenerateMap());
        }
    }

    IEnumerator GenerateMap()
    {
        if (UseRandomSeed)
        {
            Seed = Time.time.ToString(CultureInfo.InvariantCulture);
        }

        Debug.Log("CavesMapGenerator GenerateMap with seed " + Seed, this);

        _map = new int[Width, Height];

        RandomFillMap(ref _map);

        if (ShowAnimation)
        {
            yield return new WaitForSeconds(AnimationDelay);
        }

        var tmpMap = (int[,])_map.Clone();

        for (var i = 0; i < 5; i++)
        {
            SmoothMap(ref _map, ref tmpMap);

            if (ShowAnimation)
            {
                yield return new WaitForSeconds(AnimationDelay);
            }
        }
    }

    void RandomFillMap(ref int[,] map)
    {
        var rng = new Random(Seed.GetHashCode());

        for (var x = 0; x < Width; x++)
        for (var y = 0; y < Height; y++)
        {
            if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
            {
                map[x, y] = 1;
            }
            else
            {
                map[x, y] = rng.Next(0, 100) < RandomFillPercent ? 1 : 0;
            }
        }
    }

    void SmoothMap(ref int[,] map, ref int[,] tmpMap)
    {
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < Height; y++)
        {
            var neighbourWallTiles = GetSurroundingWallCount(map, x, y);
            if (neighbourWallTiles > 4)
            {
                tmpMap[x, y] = 1;
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

        map = tmpMap;
    }

    int GetSurroundingWallCount(int[,] map, int x, int y)
    {
        var wallCount = 0;
        for (var nX = x - 1; nX <= x + 1; nX++)
        for (var nY = y - 1; nY <= y + 1; nY++)
        {
            if (nX == nY)
            {
                continue;
            }
            if (nX < 0 || nX >= Width || nY < 0 || nY >= Height)
            {
                wallCount++;
            }
            else
            {
                wallCount += map[nX, nY];
            }
        }
        return wallCount;
    }

    void OnDrawGizmos()
    {
        if (_map != null)
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
            {
                Gizmos.color = (_map[x, y] == 1) ? Color.black : Color.white;
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
