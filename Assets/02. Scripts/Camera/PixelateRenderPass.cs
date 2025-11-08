using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class PixelateRenderPass : ScriptableRenderPass
{
    private int pixelScale;
    private const string PassName = "Pixelate Pass";

    class PassData
    {
        internal TextureHandle source;
        internal TextureHandle destination;
        internal int pixelScale;
    }

    public PixelateRenderPass(int scale)
    {
        pixelScale = scale;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (resourceData.isActiveTargetBackBuffer)
            return;

        TextureHandle source = resourceData.activeColorTexture;

        // 저해상도 텍스처 디스크립터 생성
        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.width /= pixelScale;
        desc.height /= pixelScale;

        TextureHandle lowResTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, desc, "_LowResPixelate", false, FilterMode.Point);

        // Pass 추가
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
        {
            passData.source = source;
            passData.destination = lowResTexture;
            passData.pixelScale = pixelScale;

            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(lowResTexture, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                // 다운샘플링
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
            });
        }

        // 업샘플링 Pass
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName + " Upscale", out var passData))
        {
            passData.source = lowResTexture;
            passData.destination = source;

            builder.UseTexture(lowResTexture, AccessFlags.Read);
            builder.SetRenderAttachment(source, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                // 업샘플링
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
            });
        }
    }

    // Deprecated된 Execute는 RenderGraph를 사용하지 않는 경우를 위한 fallback
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // RenderGraph를 사용하므로 비워둠
    }
}