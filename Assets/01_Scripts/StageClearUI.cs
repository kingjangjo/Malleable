using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneExitTrigger가 발동되면 호출되어 등급 패널을 띄우고,
/// 플레이어 입력(Interaction 키)을 받으면 다음 씬으로 진행한다.
/// Canvas 자식 StageClearPanel에 부착.
/// </summary>
public class StageClearUI : MonoBehaviour
{
    public static StageClearUI Instance { get; private set; }

    [Header("UI 참조")]
    public CanvasGroup panelGroup;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI promptText;

    [Header("표시 형식")]
    public string scoreFormat = "{0} / {1}";
    public string promptMessage = "계속하려면 E키를 누르세요";

    [Header("페이드")]
    public float fadeInDuration = 0.3f;

    private string pendingSceneName;
    private bool waitingForInput;
    private PlayerInputSystem controls;

    public GameObject demoEndUI;

    void Awake()
    {
        Instance = this;
        controls = new PlayerInputSystem();
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.gameObject.SetActive(false);
            panelGroup.blocksRaycasts = false;
        }
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    void Update()
    {
        if (!waitingForInput) return;

        if (controls.Player.Interaction.triggered)
        {
            waitingForInput = false;
            StartCoroutine(demoEnd());
            //ProceedToNextScene();
        }
    }
    IEnumerator demoEnd()
    {
        demoEndUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Loby");
    }

    /// <summary>
    /// SceneExitTrigger 등에서 호출. 등급 패널을 띄우고 입력을 기다린다.
    /// </summary>
    public void ShowClearResult(string targetSceneName)
    {
        pendingSceneName = targetSceneName;

        int current = 0, target = 0;
        string grade = "C";
        if (StageScoreManager.Instance != null)
        {
            current = StageScoreManager.Instance.currentStageScore;
            target = StageScoreManager.Instance.targetScore;
            grade = StageScoreManager.Instance.GetGrade();
        }

        if (gradeText != null) gradeText.text = grade;
        if (scoreText != null) scoreText.text = string.Format(scoreFormat, current, target);
        if (promptText != null) promptText.text = promptMessage;

        if (GameManager.Instance != null)
            GameManager.Instance.inputLocked = true;

        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        if (panelGroup == null) yield break;

        panelGroup.gameObject.SetActive(true);
        panelGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            panelGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        panelGroup.alpha = 1f;

        waitingForInput = true;
    }

    void ProceedToNextScene()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.gameObject.SetActive(false);
            panelGroup.blocksRaycasts = false;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.inputLocked = false;

        SceneManager.LoadScene(pendingSceneName);
    }
}
