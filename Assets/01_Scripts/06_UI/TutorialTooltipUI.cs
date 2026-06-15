using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 화면 한쪽에 표시되는 흰색 배경 툴팁 UI 싱글턴.
/// TutorialTrigger 등에서 Show()/Hide()를 호출해서 사용.
///
/// 구성 (Screen Space - Overlay Canvas 하위):
///   TooltipRoot (CanvasGroup)         ← canvasGroup
///     └ Background (Image, 흰색)
///         └ Message (TextMeshProUGUI) ← messageText
///
/// 시작 시 alpha = 0으로 숨겨져 있다가 Show() 호출 시 페이드 인.
/// </summary>
public class TutorialTooltipUI : MonoBehaviour
{
    public static TutorialTooltipUI Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private CanvasGroup canvasGroup;      // 툴팁 전체(배경+텍스트) 페이드용
    [SerializeField] private TextMeshProUGUI messageText;  // 안내 문구 표시용

    [Header("페이드 설정")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("자동 숨김")]
    [Tooltip("0보다 크면 표시 후 해당 시간(초)이 지나면 자동으로 숨김")]
    [SerializeField] private float autoHideDelay = 0f;

    private Coroutine fadeRoutine;
    private Coroutine autoHideRoutine;
    private object currentSource; // 현재 메시지를 띄운 호출자 (중복 Show/Hide 충돌 방지용)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    // ── 공개 API ────────────────────────────────────────────────

    /// <summary>
    /// 툴팁을 표시. source는 호출자 식별용으로,
    /// 같은 source가 Hide()를 호출했을 때만 닫히도록 보장한다.
    /// (다른 트리거 영역이 먼저 닫아버리는 것을 방지)
    /// </summary>
    public void Show(string message, object source = null)
    {
        if (messageText != null) messageText.text = message;
            currentSource = source;
        Fade(1f);

        if (autoHideRoutine != null) StopCoroutine(autoHideRoutine);
        if (autoHideDelay > 0f)
            autoHideRoutine = StartCoroutine(AutoHideRoutine(source));
    }

    /// <summary>
    /// 툴팁을 숨김. source가 지정된 경우, 현재 표시 중인 출처와 다르면 무시한다.
    /// </summary>
    public void Hide(object source = null)
    {
        if (source != null && currentSource != source) return;
        currentSource = null;
        Fade(0f);
    }

    // ── 내부 ────────────────────────────────────────────────────

    IEnumerator AutoHideRoutine(object source)
    {
        yield return new WaitForSeconds(autoHideDelay);
        Hide(source);
    }

    void Fade(float target)
    {
        if (canvasGroup == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
    }
}
