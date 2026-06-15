using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// SoulCore가 이 트리거에 닿으면 화면을 페이드 아웃한 뒤 지정된 씬으로 자동 전환.
/// BoxCollider(isTrigger 체크) + 이 스크립트를 빈 GameObject에 부착해서 사용.
/// </summary>
public class SceneExitTrigger : MonoBehaviour
{
    [Header("이동할 씬")]
    [Tooltip("Build Settings에 등록된 씬 이름")]
    public string targetSceneName = "SecondStage";

    [Header("페이드")]
    public float fadeOutDuration = 0.4f;

    private bool triggered;

    public GameObject end;

    void OnCollisionEnter(Collision other)
    {
        if (triggered) return;
        if (!other.gameObject.CompareTag("SoulCore")) return;

        triggered = true;
        end.SetActive(true);
        StartCoroutine(ToLoby());
        //StartCoroutine(TransitionRoutine());
    }
    IEnumerator ToLoby()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("Loby");
    }

    IEnumerator TransitionRoutine()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.inputLocked = true;

        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeOut(fadeOutDuration));
        else
            yield return new WaitForSeconds(fadeOutDuration);

        SceneManager.LoadScene(targetSceneName);
    }
}
