using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class SimpleEffectPass : MonoBehaviour
{
    public LayerMask effectLayer;      // 효과를 적용할 레이어
    public Material effectMaterial;    // 질문해주신 Shader Graph 머티리얼
    public string texturePropertyName = "_YourRTName"; // 셰이더 내부의 Texture2D 변수명

    private RenderTexture effectRT;

    void OnEnable()
    {
        // 1. 오브젝트를 구워낼 렌더 텍스처 생성 (화면 해상도 맞춤)
        effectRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        // 2. Render Pipeline에 내 커스텀 렌더링 루프를 등록
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        if (effectRT != null) effectRT.Release();
    }

    void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != Camera.main) return;

        // 메인 카메라의 현재 렌더 텍스처 타겟을 가로채서 효과 적용
        // 이 타이밍에 그리게 되면 메인 카메라의 Depth Buffer가 활성화되어 있어 가림 처리가 작동합니다.
        Graphics.SetRenderTarget(effectRT.colorBuffer, camera.activeTexture.depthBuffer);

        // 셰이더 그래프에 완성된 렌더 텍스처를 꽂아줍니다.
        effectMaterial.SetTexture(texturePropertyName, effectRT);
    }
}