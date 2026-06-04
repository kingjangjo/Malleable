using UnityEngine;

// 챔버 1 전용 클리어 트리거
// 이 트리거 존에 SoulCore가 진입하면 챔버 클리어
// 나중에 퍼즐 기믹으로 교체 예정
[RequireComponent(typeof(Collider))]
public class TestClearTrigger : MonoBehaviour
{
    private ChamberBase chamber;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        // 부모 오브젝트 방향으로 ChamberBase 탐색
        chamber = GetComponentInParent<ChamberBase>();
        if (chamber == null)
            Debug.LogWarning("TestClearTrigger: ChamberBase를 찾지 못했습니다.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;
        if (chamber != null) chamber.TriggerClear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1f, 0, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}