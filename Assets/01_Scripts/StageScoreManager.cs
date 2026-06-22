using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 스테이지별 쓰레기 처리 점수를 누적합니다.
/// TrashObject.OnAnyTrashPopped 전역 이벤트를 자동 구독합니다.
/// </summary>
public class StageScoreManager : MonoBehaviour
{
    public static StageScoreManager Instance { get; private set; }

    [Header("점수")]
    public int currentStageScore;
    public int targetScore = 20; // 챔버별 목표치 (Inspector에서 조정)

    [Header("UI 갱신용 이벤트")]
    public UnityEvent<int, int> onScoreChanged;

    // Static event so subscription works regardless of Awake/OnEnable order (used by UI)
    public static readonly UnityEvent<int, int> OnScoreChangedStatic = new UnityEvent<int, int>();
 // (현재점수, 목표점수)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        TrashObject.OnAnyTrashPopped.AddListener(AddScore);
    }

    void OnDisable()
    {
        TrashObject.OnAnyTrashPopped.RemoveListener(AddScore);
    }

public void AddScore(int amount)
    {
        currentStageScore += amount;
        Debug.Log($"Trash processed +{amount} (current {currentStageScore}/{targetScore})");
        onScoreChanged?.Invoke(currentStageScore, targetScore);
        OnScoreChangedStatic?.Invoke(currentStageScore, targetScore);
    }

public void ResetForNewStage()
    {
        currentStageScore = 0;
        onScoreChanged?.Invoke(currentStageScore, targetScore);
        OnScoreChangedStatic?.Invoke(currentStageScore, targetScore);
    }

    public string GetGrade()
    {
        float ratio = targetScore > 0 ? (float)currentStageScore / targetScore : 0f;
        if (ratio >= 1.0f) return "S";
        if (ratio >= 0.8f) return "A";
        if (ratio >= 0.5f) return "B";
        return "C";
    }
}