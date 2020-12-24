using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class DepthRenderPass : ScriptableRenderPass
{
    readonly string profilerTag;
    readonly Material materialToBlit;
    private readonly RenderTexture renderTexture;

    //RenderTargetHandle tempTexture;
    RenderTargetIdentifier cameraDepth;
    RenderTargetIdentifier cameraColor;

    public DepthRenderPass(string profilerTag, RenderPassEvent renderPassEvent, Material materialToBlit, RenderTexture renderTexture)
    {
        this.profilerTag = profilerTag;
        this.renderPassEvent = renderPassEvent;
        this.materialToBlit = materialToBlit;
        this.renderTexture = renderTexture;
    }

    public void Setup(RenderTargetIdentifier cameraDepth, RenderTargetIdentifier cameraColor)
    {
        this.cameraDepth = cameraDepth;
        this.cameraColor = cameraColor;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        //cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {

        var camera = renderingData.cameraData.camera;
        if (camera.name != "SecondaryCamera") { return; }
        // Debug.Log($"Camera: {renderingData.cameraData.camera.name}");
        var cmd = CommandBufferPool.Get(profilerTag);
        cmd.Clear();

        cmd.BeginSample(profilerTag);
        var viewProjectInverseMatrix = (camera.projectionMatrix * camera.worldToCameraMatrix).inverse;
        // ^^ this should work, but let's try a way from https://github.com/zezba9000/UnityMathReference/blob/master/Assets/Demos/Shaders/DepthBuffToWorldPos/DepthBuffToWorldPosDemo.cs

        var clipToWorld = Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix);
        Matrix4x4 ivp = Matrix4x4.Inverse((camera.projectionMatrix * camera.worldToCameraMatrix));

        //Debug.Log($"viewProjectInverseMatrix {viewProjectInverseMatrix}");
        materialToBlit.SetMatrix("_ViewProjectInverse", clipToWorld);
        cmd.Blit(BuiltinRenderTextureType.CameraTarget, renderTexture, materialToBlit);
        //cmd.Blit(tempTexture.Identifier(), cameraColor);
        cmd.EndSample(profilerTag);

        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        //cmd.ReleaseTemporaryRT(tempTexture.id);
    }
}