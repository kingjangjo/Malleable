using UnityEngine;
using System.Collections;

public class ChamberPipe : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("이 파이프가 속한 챔버 번호. 챔버 1이면 1을 입력.")]
    public int currentChamber;

    [Header("Prompt")]
    public GameObject promptUI;

    private bool playerNearby = false;
    private bool isTransitioning = false;
    private PlayerParticleSystem cachedPPS;
    private PlayerInputSystem controls;

    void Awake() => controls = new PlayerInputSystem();
    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
        if (cachedPPS == null)
            Debug.LogWarning("ChamberPipe: PlayerParticleSystem을 찾지 못했습니다.");
    }

    // ── 파이프 앞 진입/이탈 ──────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;
        playerNearby = true;
        if (promptUI != null) promptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;
        playerNearby = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    // ── F키 감지 ─────────────────────────────────────────────────

    void Update()
    {
        if (isTransitioning) return;
        if (!playerNearby) return;
        if (GameManager.Instance.inputLocked) return;

        if (controls.Player.Interaction.triggered)
        {
            Debug.Log("Interaction triggered");
            StartCoroutine(PipeTransition());
        }
    }

    // ── 파이프 전환 ───────────────────────────────────────────────

    IEnumerator PipeTransition()
    {
        isTransitioning = true;
        if (promptUI != null) promptUI.SetActive(false);

        // 입력 잠금 (SpawnPipe의 FadeIn이 끝난 뒤 해제)
        GameManager.Instance.inputLocked = true;

        // ① 페이드 아웃 — 화면이 검어지면서 전환 시작
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeOut(0.4f));
        else
            yield return new WaitForSeconds(0.4f); // ScreenFader 없을 때 fallback

        // ② 챔버 교체 요청
        //    → SaveProgress → ChamberManager.LoadChamber
        //      → Destroy(현재챔버) → SetActive(true) → RespawnPlayer
        //      → SpawnPipe.Start() → SpawnRoutine() 시작 (이후 처리를 SpawnPipe에 위임)
        //    모든 작업이 검은 화면 뒤에서 처리됨.
        //    프레임 드랍 / 파티클 이동 모두 안 보임.
        GameManager.Instance.OnChamberCleared(currentChamber);

        // ③ 이 코루틴 종료. 이후 FadeIn + inputLocked 해제는 SpawnPipe 담당.
        isTransitioning = false;
    }
}