using UnityEngine;
using TMPro;

/// <summary>
/// StageScoreManager의 점수 변화를 화면에 표시.
/// ScorePanel 같은 Canvas 자식 오브젝트에 부착.
/// </summary>
public class ScoreUIDisplay : MonoBehaviour
{
    [Header("UI 참조")]
    public TextMeshProUGUI scoreText;

    [Header("표시 형식")]
    [Tooltip("{0}=현재점수, {1}=목표점수")]
    public string format = "{0} / {1}";

    void Start()
    {
        // 시작할 때 한 번 갱신 (씬 시작 직후 0/20 표시)
        if (StageScoreManager.Instance != null)
            UpdateText(StageScoreManager.Instance.currentStageScore,
                       StageScoreManager.Instance.targetScore);
    }

void OnEnable()
    {
        // Static event: always subscribable regardless of StageScoreManager's Awake/Instance timing
        StageScoreManager.OnScoreChangedStatic.AddListener(UpdateText);

        // Instance may already exist with a non-zero score (e.g. this UI was disabled then re-enabled)
        if (StageScoreManager.Instance != null)
            UpdateText(StageScoreManager.Instance.currentStageScore, StageScoreManager.Instance.targetScore);
    }

void OnDisable()
    {
        StageScoreManager.OnScoreChangedStatic.RemoveListener(UpdateText);
    }

    public void UpdateText(int current, int target)
    {
        if (scoreText != null)
            scoreText.text = string.Format(format, current, target);
    }
}