using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;
using System.Xml.Linq;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class WavyIslandMapGenerator : MonoBehaviour
{
    private const string MapDisplayName = "MapDisplay";
    // TODO: probably need to spam some unit tests on this to make sure we get all the edge cases correct for top-level get and set

    // TODO: add a benchmarking project to test other performance optimisations

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

    [SerializeField] private MapData _map;
    private MeshFilter[] _collisionMeshFilters;
    private MeshCollider[] _collisionMeshColliders;
    private Texture2D _mapTexture;

    private CustomSampler _isSolidAtChecksCustomSampler;
    private CustomSampler _marchingCubesSwitchCustomSampler;

    void Start()
    {
        _isSolidAtChecksCustomSampler = CustomSampler.Create("IsSolidAt checks");
        _marchingCubesSwitchCustomSampler = CustomSampler.Create("MarchingCubesSwitch");
        StartCoroutine(GenerateMap());
    }

    void OnEnable()
    {
        EventManager.Instance.StartListening<Events.Explosion>(OnExplosion);
    }

    void OnDisable()
    {
        // TODO: it looks like EventManager always gets destroyed before we call this
        // do we actually need to clean up here?
        //EventManager.Instance.StopListening<Events.Explosion>(OnExplosion);
    }

    private void OnExplosion(Events.Explosion explosion)
    {
        RemoveCircle(_map, transform.InverseTransformPoint(explosion.worldSpacePosition), explosion.radius);
    }

    private void RemoveCircle(MapData map, Vector3 localSpace, int explosionRadius)
    {
        var removeCircleTimer = Stopwatch.StartNew();

        Profiler.BeginSample("RemoveCircle");

        Profiler.BeginSample("UpdateMapData");

        int pixelsCleared = 0;
        map.ChangedChunkIndexes.Clear();
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

            map.Set(x, y, false);
            pixelsCleared++;
        }

        Profiler.EndSample();

        var chunkIdsToUpdate = map.ChangedChunkIndexes.ToArray();

        UpdateCollisionMeshes(chunkIdsToUpdate);

        RemoveCircleFromMapTexture(_mapTexture, localSpace, explosionRadius);

        Debug.LogFormat("RemoveCircle {0} {1} pixelsCleared: {2} chunkIdsToUpdate {3} elapsed-ms {4}",
            localSpace, explosionRadius, pixelsCleared,
            String.Join(",", chunkIdsToUpdate.Select(x => x.ToString()).ToArray()), removeCircleTimer.Elapsed.TotalMilliseconds);

        Profiler.EndSample();
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
        var options = new WavyIslandMapGenerationOptions
        {
            Seed = Seed,
            PerlinScale = PerlinScale,
            PerlinThreshold = PerlinThreshold,
            SmoothPasses = SmoothPasses,
            DilatePasses = DilatePasses,
            Width = Width,
            Height = Height
        };

        if (UseRandomSeed)
        {
            options.Seed = Time.time.GetHashCode();
        }

        var generation = GeneratesWavyIslandMaps.GenerateMap(options);
        while (generation.MoveNext())
        {
            // if we needed to bring back the generation algorithm some logic could go here
            _map = generation.Current;
        }

        ClearChildren();
        AddCollisionMesh();
        AddDisplayMesh();

        yield break;
    }

    private void RemoveDisplayMesh()
    {
        var displayMesh = transform.Find(MapDisplayName);
        Destroy(displayMesh.gameObject);
    }

    private void AddDisplayMesh()
    {
        Profiler.BeginSample("WavyIslandMapGenerator.AddDisplayMesh");

        var displayMesh = new Mesh();
        CreateDisplayMesh(displayMesh);

        var mapDisplay = new GameObject(MapDisplayName, typeof(MeshFilter), typeof(MeshRenderer));
        mapDisplay.transform.SetParent(transform, false);
        var displayMeshFilter = mapDisplay.RequireComponent<MeshFilter>();
        displayMeshFilter.mesh = displayMesh;
        var texture = GetMapTexture();
        _mapTexture = texture;
        var mapRenderer = mapDisplay.RequireComponent<Renderer>();
        mapRenderer.material = MapMaterial;
        mapRenderer.material.mainTexture = texture;

        Profiler.EndSample();
    }

    private void AddCollisionMesh()
    {
        // TODO: we should be able to use CallerMemberName for something nicer with a later C# version
        Profiler.BeginSample("WavyIslandMapGenerator.AddCollisionMesh");

        //Debug.Log("Writing map to mesh");
        _collisionMeshFilters = new MeshFilter[_map.MapChunks.Length];
        _collisionMeshColliders = new MeshCollider[_map.MapChunks.Length];
        for (var i = 0; i < _map.MapChunks.Length; i++)
        {
            var chunk = _map.MapChunks[i];
            var collisionMesh = new Mesh {indexFormat = IndexFormat.UInt32};
            WriteMapToCollisionMesh(chunk, collisionMesh);

            var mapCollisionName = "MapCollision_"+i;
            var mapCollision =
                new GameObject(mapCollisionName, typeof(MeshFilter), typeof(MeshCollider) /*, typeof(MeshRenderer)*/);
            mapCollision.transform.SetParent(transform, false);
            var meshFilter = mapCollision.RequireComponent<MeshFilter>();
            meshFilter.mesh = collisionMesh;
            _collisionMeshFilters[i] = meshFilter;
            var meshCollider = mapCollision.RequireComponent<MeshCollider>();
            meshCollider.sharedMesh = collisionMesh;
            _collisionMeshColliders[i] = meshCollider;
        }

        Profiler.EndSample();
    }

    private void UpdateCollisionMeshes(int[] chunkIdsToUpdate)
    {
        // TODO: we should be able to use CallerMemberName for something nicer with a later C# version
        Profiler.BeginSample("WavyIslandMapGenerator.UpdateCollisionMeshes");

        //Debug.Log("Writing map to mesh");
        foreach (var chunkIndex in chunkIdsToUpdate)
        {
            var chunk = _map.MapChunks[chunkIndex];
            var collisionMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            WriteMapToCollisionMesh(chunk, collisionMesh);

            _collisionMeshFilters[chunkIndex].mesh = collisionMesh;
            _collisionMeshColliders[chunkIndex].sharedMesh = collisionMesh;
        }

        Profiler.EndSample();
    }

    private void ClearChildren()
    {
        Profiler.BeginSample("WavyIslandMapGenerator.ClearChildren");

        foreach (var child in transform.Cast<Transform>().ToList())
        {
            Destroy(child.gameObject);
        }

        Profiler.EndSample();
    }

    private void WriteMapToCollisionMesh(MapChunk chunk, Mesh mesh)
    {
        //Debug.Log("WriteMapToCollisionMesh " + Width + " " + Height);
        Profiler.BeginSample("WriteMapToCollisionMesh");

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        var left = 1+chunk.XOffset;
        var bottom = 1+chunk.YOffset;

        Profiler.BeginSample("Main loop");
        // ref: https://en.wikipedia.org/wiki/Marching_squares
        for (int mapY = 0; mapY < MapChunk.ChunkSize - 1; mapY++)
        {
            var yc = chunk.Chunk[mapY];
            var y1c = chunk.Chunk[mapY + 1];

            if ((yc == 0 && y1c == 0) || (yc == UInt64.MaxValue && y1c == UInt64.MaxValue))
            {
                continue;
            }

            for (int mapX = 0; mapX < MapChunk.ChunkSize - 1; mapX++)
            {
                var cell = 0;

                _isSolidAtChecksCustomSampler.Begin();

                if ((yc & 1UL << mapX) > 0) { cell += 1; }
                if ((yc & 1UL << mapX + 1) > 0) { cell += 2; }
                if ((y1c & 1UL << mapX + 1) > 0) { cell += 4; }
                if ((y1c & 1UL << mapX) > 0) { cell += 8; }
                _isSolidAtChecksCustomSampler.End();

                if (cell == 0 || cell == 15) { continue; }

                _marchingCubesSwitchCustomSampler.Begin();
                // +8  +4
                //
                // +1  +2
                var left1 = left + mapX;
                var left0 = left1 - 0.5f;
                var left2 = left1 + 0.5f;
                var bottom1 = bottom + mapY;
                var bottom0 = bottom1 - 0.5f;
                var bottom2 = bottom1 + 0.5f;
                switch (cell)
                {
                    case 0: // 0b0000:
                        break;
                    case 1: // 0b0001:
                        BuildQuad(vertices, triangles,
                            new Vector3(left1, bottom0, -1),
                            new Vector3(left0, bottom1, -1),
                            new Vector3(left0, bottom1, +1),
                            new Vector3(left1, bottom0, +1));
                        break;
                    case 2: // 0b0010:
                        BuildQuad(vertices, triangles,
                            new Vector3(left1, bottom0, -1),
                            new Vector3(left1, bottom0, +1),
                            new Vector3(left2, bottom1, +1),
                            new Vector3(left2, bottom1, -1));
                        break;
                    case 3: // 0b0011:
                        BuildQuad(vertices, triangles,
                            new Vector3(left2, bottom1, -1),
                            new Vector3(left0, bottom1, -1),
                            new Vector3(left0, bottom1, +1),
                            new Vector3(left2, bottom1, +1));
                        break;
                    case 4: // 0b0100:
                        BuildQuad(vertices, triangles,
                            new Vector3(left2, bottom1, -1),
                            new Vector3(left2, bottom1, +1),
                            new Vector3(left1, bottom2, +1),
                            new Vector3(left1, bottom2, -1));
                        break;
                    case 5: // 0b0101:
                        BuildQuad(vertices, triangles,
                            new Vector3(left2, bottom1, -1),
                            new Vector3(left2, bottom1, +1),
                            new Vector3(left1, bottom0, +1),
                            new Vector3(left1, bottom0, -1));
                        BuildQuad(vertices, triangles,
                            new Vector3(left0, bottom1, -1),
                            new Vector3(left0, bottom1, +1),
                            new Vector3(left1, bottom2, +1),
                            new Vector3(left1, bottom2, -1));
                        break;
                    case 6: // 0b0110:
                        BuildQuad(vertices, triangles,
                            new Vector3(left1, bottom2, -1),
                            new Vector3(left1, bottom0, -1),
                            new Vector3(left1, bottom0, +1),
                            new Vector3(left1, bottom2, +1));
                        break;
                    case 7: // 0b0111:
                        BuildQuad(vertices, triangles,
                            new Vector3(left0, bottom1, -1),
                            new Vector3(left0, bottom1, +1),
                            new Vector3(left1, bottom2, +1),
                            new Vector3(left1, bottom2, -1));
                        break;
                    case 8: // 0b1000:
                        BuildQuad(vertices, triangles,
                            new Vector3(left0, bottom1, -1),
                            new Vector3(left1, bottom2, -1),
                            new Vector3(left1, bottom2, +1),
                            new Vector3(left0, bottom1, +1));
                        break;
                    case 9: // 0b1001:
                        BuildQuad(vertices, triangles,
                            new Vector3(left1, bottom2, -1),
                            new Vector3(left1, bottom2, +1),
                            new Vector3(left1, bottom0, +1),
                            new Vector3(left1, bottom0, -1));
                        break;
                    case 10: // 0b1010:
                        BuildQuad(vertices, triangles,
                            new Vector3(left1, bottom0, -1),
                            new Vector3(left1, bottom0, +1),
                            new Vector3(left0, bottom1, +1),
                            new Vector3(left0, bottom1, -1));
                        BuildQuad(vertices, triangles,
                            new Vector3(left2, bottom1, -1),
                            new Vector3(left2, bottom1, +1),
                            new Vector3(left1, bottom2, +1),
                            new Vector3(left1, bottom2, -1));
                        break;
                    case 11: // 0b1011:
                        BuildQuad(vertices, triangles,
                            new Vector3(left2, bottom1, -1),
                            new Vector3(left1, bottom2, -1),
                            new Vector3(left1, bottom2, +1),
                            new Vector3(left2, bottom1, +1));
                        break;
                    case 12: // 0b1100:
                        BuildQuad(vertices, triangles,
                            new Vector3(left2, bottom1, -1),
                            new Vector3(left2, bottom1, +1),
                            new Vector3(left0, bottom1, +1),
                            new Vector3(left0, bottom1, -1));
                        break;
                    case 13: // 0b1101:
                        BuildQuad(vertices, triangles,
                            new Vector3(left2, bottom1, -1),
                            new Vector3(left2, bottom1, +1),
                            new Vector3(left1, bottom0, +1),
                            new Vector3(left1, bottom0, -1));
                        break;
                    case 14: // 0b1110:
                        BuildQuad(vertices, triangles,
                            new Vector3(left1, bottom0, -1),
                            new Vector3(left1, bottom0, +1),
                            new Vector3(left0, bottom1, +1),
                            new Vector3(left0, bottom1, -1));
                        break;
                    case 15: // 0b1111:
                        break;
                }
                _marchingCubesSwitchCustomSampler.End();
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("Apply to mesh");
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        Profiler.EndSample();

        Profiler.EndSample();
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
                var mapValue = _map.Get(x, y);
                if (!mapValue) { newColors[y * Width + x] = new Color32(0, 0, 0, 0); }
                else { newColors[y * Width + x] = new Color32(128, 128, 128, 255); }
            }
        texture.SetPixels32(newColors);
        texture.Apply();

        return texture;
    }

    private void RemoveCircleFromMapTexture(Texture2D mapTexture, Vector3 localSpace, int explosionRadius)
    {
        Profiler.BeginSample("RemoveCircleFromMapTexture");

        for (int ex = -explosionRadius; ex < +explosionRadius; ex++)
        for (int ey = -explosionRadius; ey < +explosionRadius; ey++)
        {
            var x = (int) localSpace.x + ex;
            var y = (int) localSpace.y + ey;
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                continue;
            }
            if (ex * ex + ey * ey > explosionRadius * explosionRadius)
            {
                continue;
            }

            mapTexture.SetPixel(x, y, new Color(0,0,0,0));
        }
        mapTexture.Apply();
        Profiler.EndSample();
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