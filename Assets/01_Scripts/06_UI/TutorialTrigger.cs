    using UnityEngine;

/// <summary>
/// 플레이어(SoulCore)가 영역에 들어오면 TutorialTooltipUI에 안내 문구를 띄우는 트리거 존.
///
/// 설치 방법:
/// 1. 빈 GameObject 생성 후 BoxCollider(isTrigger 체크) 추가
/// 2. 이 스크립트 부착, message에 안내 문구 입력
/// 3. 좁은 통로 앞 / 새 기믹 앞 / 변신이 필요한 구간 앞 등에 배치
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    [Header("표시 내용")]
    [TextArea(2, 5)]
    [Tooltip("툴팁에 표시할 안내 문구")]
    public string message = "F키를 눌러 변신하세요";

    [Header("표시 조건")]
    [Tooltip("체크하면 지정된 폼(Soul/Humanoid)일 때만 표시")]
    public bool requireSpecificForm = false;
    public PlayerForm requiredForm = PlayerForm.Soul;

    [Header("한 번만 표시")]
    [Tooltip("체크 시, 한 번 본 뒤로는 다시 표시하지 않음 (PlayerPrefs에 저장됨)")]
    public bool showOnce = false;
    [Tooltip("showOnce 저장용 고유 키. 비워두면 씬 이름 + 오브젝트 이름으로 자동 생성")]
    public string saveKey = "";

    private bool alreadyShown;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;
        if (alreadyShown) return;

        if (requireSpecificForm)
        {
            var form = other.GetComponentInParent<PlayerFormController>();
            if (form == null || form.currentForm != requiredForm) return;
        }

        if (showOnce)
        {
            string key = GetSaveKey();
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                alreadyShown = true;
                return;
            }
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            alreadyShown = true;
        }

        if (TutorialTooltipUI.Instance != null)
            TutorialTooltipUI.Instance.Show(message, this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;

        if (TutorialTooltipUI.Instance != null)
            TutorialTooltipUI.Instance.Hide(this);
    }

    string GetSaveKey()
    {
        if (!string.IsNullOrEmpty(saveKey)) return "Tutorial_" + saveKey;
        return "Tutorial_" + gameObject.scene.name + "_" + gameObject.name;
    }

    // 에디터 시각화
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(col.center, col.size);
    }
}
