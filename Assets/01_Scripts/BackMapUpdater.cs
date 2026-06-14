using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BackMapUpdater : MonoBehaviour
{
    [SerializeField] private Material liquidMaterial;

    private static readonly int BackMapID = Shader.PropertyToID("_BackMap");
    private static readonly int OpaqueTexID = Shader.PropertyToID("_CameraOpaqueTexture");

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
    }

    void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam.cameraType != CameraType.Game) return;

        // _CameraOpaqueTexture를 직접 가져오는 대신
        // 셰이더에 전역으로 등록된 값을 머티리얼에 복사
        var tex = Shader.GetGlobalTexture(OpaqueTexID);
        Debug.Log(tex == null ? "null" : $"{tex.width}x{tex.height}");

        if (tex != null && liquidMaterial != null)
            liquidMaterial.SetTexture(BackMapID, tex);
    }
}