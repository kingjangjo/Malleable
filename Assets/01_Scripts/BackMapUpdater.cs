using UnityEngine;

public class BackMapUpdater : MonoBehaviour
{
    [SerializeField] private Material liquidMaterial;

    void Update()
    {
        Texture opaqueTexture = Shader.GetGlobalTexture("_CameraOpaqueTexture");
        if (opaqueTexture == null) return;
        liquidMaterial.SetTexture("_BackMap", opaqueTexture);
    }
}