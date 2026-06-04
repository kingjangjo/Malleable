using UnityEngine;

// 기믹 2: 좁은 틈 통과
// 물리적으로 좁은 통로를 만들어 Humanoid(캡슐)는 막히고
// Soul(구체, 작은 콜라이더)만 통과 가능하게 함
// 이 스크립트는 통과를 감지하는 역할
// 
// 설치 방법:
// 1. 통로 맞은편(도착 지점)에 BoxCollider(isTrigger) 배치
// 2. 이 스크립트 부착
// 3. SoulCore가 이 트리거에 들어오면 통과 완료로 처리
public class LiquidPassage : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("통과하려면 반드시 Soul 상태여야 하는지 여부")]
    public bool requireSoulForm = true;

    public bool IsComplete { get; private set; } = false;

    void OnTriggerEnter(Collider other)
    {
        if (IsComplete) return;
        if (!other.CompareTag("SoulCore")) return;

        if (requireSoulForm)
        {
            // Soul 상태인지 확인
            var form = other.GetComponentInParent<PlayerFormController>();
            if (form == null || form.currentForm != PlayerForm.Soul)
            {
                Debug.Log("LiquidPassage: 액체 상태가 아니라 통과 불가");
                return;
            }
        }

        IsComplete = true;
        Debug.Log($"{gameObject.name}: 통과 완료!");
    }

    // 에디터 시각화
    void OnDrawGizmos()
    {
        Gizmos.color = IsComplete ? Color.green : new Color(0, 0.5f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}