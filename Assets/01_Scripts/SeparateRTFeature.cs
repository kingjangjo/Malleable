using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class SeparateRTFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private LayerMask layerMask;
        private Material overrideMaterial;
        private RenderTexture targetRT;
        private RTHandle colorHandle;
        private List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();

        public CustomRenderPass(LayerMask layer, Material mat, RenderTexture rt)
        {
            layerMask = layer;
            overrideMaterial = mat;
            targetRT = rt;

            shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
        }

        private class PassData
        {
            public RendererListHandle rendererListHandle;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (targetRT == null) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            if (cameraData == null || renderingData == null || lightData == null) return;

            // 1. 목적지 RenderTexture를 RTHandle로 변환하여 그래프에 등록
            if (colorHandle == null || colorHandle.rt != targetRT)
            {
                colorHandle?.Release();
                colorHandle = RTHandles.Alloc(targetRT);
            }
            TextureHandle destinationHandle = renderGraph.ImportTexture(colorHandle);

            // 2. [블랙스크린 해결 핵심] 목적지 RT의 가로/세로 크기와 '완벽히 일치하는 전용 깊이 버퍼' 동적 생성
            TextureDesc depthDesc = new TextureDesc(targetRT.width, targetRT.height);
            depthDesc.format = GraphicsFormat.D24_UNorm_S8_UInt; // 표준 24비트 뎁스 포맷
            depthDesc.name = "SeparateRTPassDepth";
            TextureHandle customDepthHandle = renderGraph.CreateTexture(depthDesc);

            // 3. 오브젝트 드로우 및 레이어 필터링 설정
            SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagIds, renderingData, cameraData, lightData, sortingCriteria);

            if (overrideMaterial != null)
            {
                drawingSettings.overrideMaterial = overrideMaterial;
            }

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
            RendererListParams renderListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            RendererListHandle rendererListHandle = renderGraph.CreateRendererList(renderListParams);

            // 4. 유니티 6 표준 래스터 패스 빌드
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("SeparateRTRasterPass", out var passData))
            {
                passData.rendererListHandle = rendererListHandle;

                // 크기가 동일한 컬러 텍스처와 깊이 텍스처를 안전하게 프레임버퍼에 세팅
                builder.SetRenderAttachment(destinationHandle, 0);
                builder.SetRenderAttachmentDepth(customDepthHandle, AccessFlags.Write);

                builder.UseRendererList(rendererListHandle);

                // 5. GPU 실행 함수 (정적 람다 구조)
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    // 그리기 직전, 텍스처 버퍼들을 완전히 초기화 (투명한 색상 & 깊이 버퍼 리셋)
                    context.cmd.ClearRenderTarget(true, true, Color.clear);
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }

        public void Dispose()
        {
            colorHandle?.Release();
        }
    }

    [System.Serializable]
    public struct Settings
    {
        public LayerMask effectLayer;
        public Material shaderGraphMaterial;
        public RenderTexture destinationRT;
        public RenderPassEvent renderEvent;
    }

    public Settings settings = new Settings();
    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(settings.effectLayer, settings.shaderGraphMaterial, settings.destinationRT);
        m_ScriptablePass.renderPassEvent = settings.renderEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game && settings.destinationRT != null)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();
    }
}