using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PixelateRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 16)]
        public int pixelScale = 4;
    }
    public Settings settings = new Settings();
    private PixelateRenderPass renderPass;

    public override void Create()
    {
        renderPass = new PixelateRenderPass(settings.pixelScale);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 매 프레임 현재 설정값을 전달
        renderPass.UpdatePixelScale(settings.pixelScale);
        renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        // Cleanup if needed
    }
}