using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public class GPUGraph : MonoBehaviour
{
    public FunctionLibrary.FunctionName functionName = default;

    [Range(1, 1000)]
    public int resolution = 10;

    [Min(0f)]
    public float functionDuration = 1f;

    public ComputeShader computeShader;

    public Material material;
    public Mesh mesh;

    static readonly int positionsId = Shader.PropertyToID("_Positions");
    static readonly int resolutionId = Shader.PropertyToID("_Resolution");
    static readonly int stepId = Shader.PropertyToID("_Step");
    static readonly int timeId = Shader.PropertyToID("_Time");


    float lastFunctionSwitch = 0;

    ComputeBuffer positionsBuffer;

    void OnEnable()
    {
        Application.targetFrameRate = 60;
        positionsBuffer = new ComputeBuffer(
            resolution * resolution,
            3 * 4 /* vec3 of floats, each 4 bytes */);
    }

    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    void Update()
    {
        while (Time.time - lastFunctionSwitch >= functionDuration)
        {
            lastFunctionSwitch += functionDuration;
            functionName = FunctionLibrary.GetNext(functionName);
        }

        UpdateFunctionOnGPU();
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        computeShader.SetBuffer(0, positionsId, positionsBuffer);
        var numGroups = CeilToInt(resolution / 8f);
        // Debug.Log(numGroups);
        computeShader.Dispatch(0, numGroups, numGroups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        // TODO: this appears to provide nearly-accurate scaling for point objects, but it's unclear why
        // just specifying `step` doesn't work (as the tutorial describes)
        material.SetFloat(stepId, Sqrt(step));

        var bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f + 2f / resolution, 2f));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionsBuffer.count);
    }
}
