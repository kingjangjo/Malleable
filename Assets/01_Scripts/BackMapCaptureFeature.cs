// BackMapCaptureFeature.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class BackMapCaptureFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderTexture backMapRT;
        // EffectLayerPass보다 먼저 실행되어야 함
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public Settings settings = new();
    BackMapCapturePass _pass;

    public override void Create()
    {
        _pass = new BackMapCapturePass(settings)
        {
            renderPassEvent = settings.passEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                         ref RenderingData renderingData)
    {
        if (settings.backMapRT == null) return;
        if (renderingData.cameraData.camera.cameraType != CameraType.Game) return;
        renderer.EnqueuePass(_pass);
    }
}

class BackMapCapturePass : ScriptableRenderPass
{
    readonly BackMapCaptureFeature.Settings _settings;

    public BackMapCapturePass(BackMapCaptureFeature.Settings s) => _settings = s;

    //public override void RecordRenderGraph(RenderGraph renderGraph,
    //                                   ContextContainer frameData)
    //{
    //    if (_settings.backMapRT == null) return;

    //    var resourceData = frameData.Get<UniversalResourceData>();
    //    var cameraData = frameData.Get<UniversalCameraData>();

    //    // RT 해상도 동기화
    //    var desc = cameraData.cameraTargetDescriptor;
    //    desc.depthBufferBits = 0;
    //    desc.msaaSamples = 1;

    //    var rt = _settings.backMapRT;
    //    if (rt.width != desc.width || rt.height != desc.height)
    //    {
    //        rt.Release();
    //        rt.width = desc.width;
    //        rt.height = desc.height;
    //        rt.Create();
    //    }

    //    var src = resourceData.activeColorTexture;

    //    // RTHandle 캐싱 (매 프레임 생성/해제 방지)
    //    var rtHandle = RTHandles.Alloc(rt);
    //    var dst = renderGraph.ImportTexture(rtHandle);

    //    // src → dst 직접 복사
    //    renderGraph.AddCopyPass(src, dst, passName: "CaptureBackMap");

    //    rtHandle.Release();
    //}
    public override void RecordRenderGraph(RenderGraph renderGraph,
                                       ContextContainer frameData)
    {
        // RecordRenderGraph는 비워두기
    }

    // 레거시 방식으로 오버라이드
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
    }

    public override void Execute(ScriptableRenderContext context,
                                  ref RenderingData renderingData)
    {
        if (_settings.backMapRT == null) return;

        var cmd = CommandBufferPool.Get("CaptureBackMap");

        // 현재 카메라 컬러 버퍼 → backMapRT 직접 복사
        cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle,
                 _settings.backMapRT);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    class PassData
    {
        public TextureHandle src;
    }
}