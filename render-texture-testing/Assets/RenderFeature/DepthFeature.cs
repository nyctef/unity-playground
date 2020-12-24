using UnityEngine;
using UnityEngine.Rendering.Universal;

// based on https://samdriver.xyz/article/scriptable-render

public class DepthFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class DepthFeatureSettings
    {
        public Material MaterialToBlit;
        public RenderTexture RenderTexture;
    }

    public DepthFeatureSettings settings = new DepthFeatureSettings();

    RenderTargetHandle renderTextureHandle;
    DepthRenderPass depthRenderPass;

    public override void Create()
    {
        depthRenderPass = new DepthRenderPass("Depth view rendering pass", RenderPassEvent.AfterRenderingOpaques, settings.MaterialToBlit, settings.RenderTexture);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        depthRenderPass.Setup(renderer.cameraDepth, renderer.cameraColorTarget);

        renderer.EnqueuePass(depthRenderPass);
    }
}