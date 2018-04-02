using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

        if (ShowAnimation) { yield return new WaitForSeconds(AnimationDelay*3); }

        Debug.Log("threshold map");
        ThresholdMap(ref _map);

        if (ShowAnimation) { yield return new WaitForSeconds(AnimationDelay); }
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
