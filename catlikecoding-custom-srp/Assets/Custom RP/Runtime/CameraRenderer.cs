using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial struct CameraRenderer
{
    readonly ScriptableRenderContext context;
    readonly Camera camera;
    const string bufferName = "Render Camera";
    readonly CommandBuffer buffer;

    static readonly ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

#if UNITY_EDITOR
    string SampleName { get; set; }
#else
    const string SampleName = bufferName;
#endif

    public CameraRenderer(ScriptableRenderContext context, Camera camera)
    {
        this.camera = camera;
        this.context = context;
        this.buffer = new CommandBuffer { name = bufferName };
#if UNITY_EDITOR
        SampleName = default;
#endif
    }

    public void Render()
    {
        PrepareBuffer();
        Setup();
        PrepareForSceneWindow();

        var (success, cullingResults) = Cull();
        if (!success) { return; }

        DrawVisibleGeometry(cullingResults);
        DrawUnsupportedShaders(cullingResults);
        DrawGizmos();

        Submit();
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);
        var flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        //Debug.Log($"BeginSample SampleName {SampleName} bufferName {bufferName} buffer.name {buffer.name} camera.name {camera.name} ");
        ExecuteBuffer();
    }

    partial void PrepareBuffer();

    partial void PrepareForSceneWindow();

    private (bool, CullingResults) Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
        {
            var cullingResults = context.Cull(ref parameters);
            return (true, cullingResults);
        }
        return (false, default);
    }

    private void DrawVisibleGeometry(CullingResults cullingResults)
    {
        // only render non-transparent objects for the first pass
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        );
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        context.DrawSkybox(camera);

        // we need to draw transparent materials after the skybox. Transparent objects
        // don't write to the depth buffer, so the skybox will overwrite them otherwise.
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    partial void DrawUnsupportedShaders(CullingResults cullingResults);
    partial void DrawGizmos();

    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}