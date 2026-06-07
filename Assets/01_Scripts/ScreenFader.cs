using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 화면 페이드 인/아웃 싱글턴.
/// Screen Space - Overlay Canvas 하위의 전체화면 Image에 연결.
/// DontDestroyOnLoad 오브젝트에 부착해서 챔버가 바뀌어도 유지.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image panel;   // 검정 전체화면 Image (alpha=0 시작)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        if (panel != null) panel.color = new Color(0f, 0f, 0f, 0f);
    }

    // ── 공개 API ────────────────────────────────────────────────

    /// <summary>화면을 검정으로 페이드</summary>
    public IEnumerator FadeOut(float duration = 0.4f)
    {
        yield return StartCoroutine(DoFade(0f, 1f, duration));
    }

    /// <summary>검정에서 화면으로 페이드</summary>
    public IEnumerator FadeIn(float duration = 0.5f)
    {
        yield return StartCoroutine(DoFade(1f, 0f, duration));
    }

    /// <summary>즉시 검정</summary>
    public void SetBlack()
    {
        if (panel != null) panel.color = new Color(0f, 0f, 0f, 1f);
    }

    // ── 내부 ────────────────────────────────────────────────────

    IEnumerator DoFade(float from, float to, float duration)
    {
        if (panel == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panel.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        panel.color = new Color(0f, 0f, 0f, to);
    }
}