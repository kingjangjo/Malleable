using UnityEngine;
using UnityEngine.Events;

// 기믹 1: 바닥 버튼
// 고체(SoulCore) 상태로 올라서면 눌림
// isToggle = false: 올라서 있는 동안만 눌림
// isToggle = true: 한 번 누르면 영구 유지
public class ChamberButton : MonoBehaviour
{
    [Header("Settings")]
    public bool isToggle = false;

    [Header("Visual")]
    public Transform buttonMesh;        // 눌릴 때 내려가는 메시
    public float pressDepth = 0.1f;     // 얼마나 내려가는지

    [Header("Events")]
    public UnityEvent onPress;
    public UnityEvent onRelease;

    public bool IsPressed { get; private set; } = false;

    private Vector3 originalMeshPos;
    private bool solidOnButton = false;

    void Start()
    {
        if (buttonMesh != null)
            originalMeshPos = buttonMesh.localPosition;
    }

    // SoulCore(고체) 올라섰을 때 감지
    // 참고: Collider는 isTrigger: true 설정 필요
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;

        // 고체 상태인지 확인
        var form = other.GetComponentInParent<PlayerFormController>();
        if (form == null || form.currentForm != PlayerForm.Humanoid) return;

        solidOnButton = true;
        Evaluate();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;

        solidOnButton = false;
        if (!isToggle) Evaluate();
    }

    void Evaluate()
    {
        bool shouldPress = solidOnButton;

        if (shouldPress && !IsPressed)
        {
            IsPressed = true;
            onPress?.Invoke();

            if (buttonMesh != null)
                buttonMesh.localPosition = originalMeshPos - Vector3.up * pressDepth;

            Debug.Log($"{gameObject.name} 눌림");
        }
        else if (!shouldPress && IsPressed && !isToggle)
        {
            IsPressed = false;
            onRelease?.Invoke();

            if (buttonMesh != null)
                buttonMesh.localPosition = originalMeshPos;
        }
    }
}