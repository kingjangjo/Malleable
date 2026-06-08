using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class EffectLayerRendererFeature : ScriptableRendererFeature
{
    // 파티클 드로우 요청 등록용 정적 큐
    public struct DrawRequest
    {
        public Mesh mesh;
        public int submeshIndex;
        public Material material;
        public Matrix4x4[] matrices;
        public int count;
    }

    public static readonly List<DrawRequest> _pending = new();

    public static void RegisterDraw(Mesh mesh, int submesh, Material mat,
                                    Matrix4x4[] matrices, int count)
    {
        var copy = new Matrix4x4[count];
        System.Array.Copy(matrices, copy, count);
        _pending.Add(new DrawRequest
        {
            mesh = mesh,
            submeshIndex = submesh,
            material = mat,
            matrices = copy,
            count = count
        });
    }

    [System.Serializable]
    public class Settings
    {
        public RenderTexture effectRenderTexture;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public Settings settings = new();
    EffectLayerPass _pass;

    public override void Create()
    {
        _pass = new EffectLayerPass(settings) { renderPassEvent = settings.passEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.effectRenderTexture == null) return;

        // ★ 게임 카메라에서만 패스 실행 (섀도우·반사·에디터 씬 카메라 제외)
        if (renderingData.cameraData.camera.cameraType != CameraType.Game) return;

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing) => _pass?.Cleanup();
}

// ──────────────────────────────────────────────
class EffectLayerPass : ScriptableRenderPass
{
    class PassData
    {
        public List<EffectLayerRendererFeature.DrawRequest> requests;
    }

    readonly EffectLayerRendererFeature.Settings _settings;
    RTHandle _effectColorRT;
    RenderTexture _cachedRT;

    public EffectLayerPass(EffectLayerRendererFeature.Settings s) => _settings = s;

    //public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    //{
    //    if (_settings.effectRenderTexture == null) return;

    //    // 이번 프레임 요청 수집 후 큐 클리어
    //    var requests = new List<EffectLayerRendererFeature.DrawRequest>(
    //        EffectLayerRendererFeature._pending);
    //    EffectLayerRendererFeature._pending.Clear();

    //    if (requests.Count == 0) return;

    //    var cameraData = frameData.Get<UniversalCameraData>();
    //    var resourceData = frameData.Get<UniversalResourceData>();

    //    // RT 해상도 동기화
    //    var rt = _settings.effectRenderTexture;
    //    var camDesc = cameraData.cameraTargetDescriptor;
    //    if (rt.width != camDesc.width || rt.height != camDesc.height)
    //    {
    //        rt.Release();
    //        rt.width = camDesc.width;
    //        rt.height = camDesc.height;
    //        rt.Create();
    //        _effectColorRT?.Release();
    //        _effectColorRT = null;
    //        _cachedRT = null;
    //    }

    //    if (rt != _cachedRT)
    //    {
    //        _effectColorRT?.Release();
    //        _cachedRT = rt;
    //        _effectColorRT = RTHandles.Alloc(_cachedRT);
    //    }

    //    var colorTarget = renderGraph.ImportTexture(_effectColorRT);

    //    using (var builder = renderGraph.AddRasterRenderPass<PassData>(
    //               "EffectLayerPass", out var passData))
    //    {
    //        passData.requests = requests;

    //        builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
    //        // ★ Main Camera Depth 공유 → 벽에 가려지는 파티클 차폐
    //        builder.SetRenderAttachmentDepth(
    //            resourceData.activeDepthTexture, AccessFlags.Read);

    //        builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
    //        {
    //            ctx.cmd.ClearRenderTarget(false, true, Color.clear);

    //            foreach (var req in data.requests)
    //            {
    //                // GameObject 없이 직접 GPU 드로우
    //                ctx.cmd.DrawMeshInstanced(
    //                    req.mesh, req.submeshIndex, req.material,
    //                    -1, req.matrices, req.count);
    //            }
    //        });
    //    }
    //}
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_settings.effectRenderTexture == null) return;

        // 이번 프레임 요청 수집 후 큐 클리어
        var requests = new List<EffectLayerRendererFeature.DrawRequest>(
            EffectLayerRendererFeature._pending);
        EffectLayerRendererFeature._pending.Clear();

        // ★ [기존 코드 제거] if (requests.Count == 0) return; 
        // 입자가 0개여도 아래로 내려가서 화면을 지워야 하므로 제거합니다.

        var cameraData = frameData.Get<UniversalCameraData>();
        var resourceData = frameData.Get<UniversalResourceData>();

        // RT 해상도 동기화
        var rt = _settings.effectRenderTexture;
        var camDesc = cameraData.cameraTargetDescriptor;
        if (rt.width != camDesc.width || rt.height != camDesc.height)
        {
            rt.Release();
            rt.width = camDesc.width;
            rt.height = camDesc.height;
            rt.Create();
            _effectColorRT?.Release();
            _effectColorRT = null;
            _cachedRT = null;
        }

        if (rt != _cachedRT)
        {
            _effectColorRT?.Release();
            _cachedRT = rt;
            _effectColorRT = RTHandles.Alloc(_cachedRT);
        }

        var colorTarget = renderGraph.ImportTexture(_effectColorRT);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
           "EffectLayerPass", out var passData))
        {
            passData.requests = requests;

            builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);

            // ★ 이 블록 전체 제거 (바닥과 depth 충돌 원인)
            // if (requests.Count > 0)
            // {
            //     builder.SetRenderAttachmentDepth(
            //         resourceData.activeDepthTexture, AccessFlags.Read);
            // }

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                ctx.cmd.ClearRenderTarget(false, true, Color.clear);

                if (data.requests != null && data.requests.Count > 0)
                {
                    foreach (var req in data.requests)
                    {
                        ctx.cmd.DrawMeshInstanced(
                            req.mesh, req.submeshIndex, req.material,
                            -1, req.matrices, req.count);
                    }
                }
            });
        }
    }

    public void Cleanup()
    {
        _effectColorRT?.Release();
        _effectColorRT = null;
    }
}