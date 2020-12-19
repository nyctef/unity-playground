using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public Transform pointPrefab = default;
    public FunctionLibrary.FunctionName functionName = default;

    [Range(10, 200)]
    public int resolution = 10;

    [Min(0f)]
    public float functionDuration = 1f;

    Transform[] points;

    float lastFunctionSwitch = 0;

    void Awake()
    {
        Application.targetFrameRate = 60;

        points = new Transform[resolution * resolution];
        var step = (resolution / 2f);
        var scale = Vector3.one / step;

        for (int x = 0; x < resolution; x++)
            for (int z = 0; z < resolution; z++)
            {
                Transform point = Instantiate(pointPrefab);
                point.SetParent(transform, false);
                point.localScale = scale;
                points[x * resolution + z] = point;
            }

    }

    void Update()
    {
        while (Time.time - lastFunctionSwitch >= functionDuration)
        {
            lastFunctionSwitch += functionDuration;
            functionName = FunctionLibrary.GetNext(functionName);
        }
        UpdateFunction();
    }

    private void UpdateFunction()
    {
        var f = FunctionLibrary.GetFunction(functionName);
        for (int x = 0; x < resolution; x++)
            for (int z = 0; z < resolution; z++)
            {
                var point = points[x * resolution + z];
                var position = point.transform.localPosition;
                var step = (resolution / 2f);
                var u = (x + 0.5f) / step - 1f;
                var v = (z + 0.5f) / step - 1f;
                position = f(u, v, Time.time);
                point.transform.localPosition = position;
            }
    }
}
