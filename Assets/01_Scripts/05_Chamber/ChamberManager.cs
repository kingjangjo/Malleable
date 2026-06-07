using UnityEngine;
using System.Collections;

public class ChamberManager : MonoBehaviour
{
    public static ChamberManager Instance { get; private set; }

    [Header("Chamber Prefabs")]
    [Tooltip("인덱스 0 = 챔버 1, 인덱스 1 = 챔버 2 ...")]
    public GameObject[] chamberPrefabs;

    [Header("References")]
    public GameObject player;

    private GameObject currentChamberInstance;
    private Transform currentSpawnPoint;

    // ── 프리로드 상태 ──────────────────────────────────────────────
    // TriggerClear() 시점에 다음 챔버를 미리 생성해두고,
    // 실제 전환 시 Instantiate 스파이크 없이 바로 사용
    private GameObject preloadedInstance;
    private int preloadedIndex = -1;   // 현재 프리로드된 챔버의 인덱스 (n-1)

    private PlayerParticleSystem cachedPPS;
    private PlayerFormController cachedFormController;

    public GameObject Ending;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
        cachedFormController = FindObjectOfType<PlayerFormController>();

        if (cachedPPS == null)
            Debug.LogWarning("ChamberManager: PlayerParticleSystem을 찾지 못했습니다.");
    }

    // ── 챔버 로드 ──────────────────────────────────────────────────

    public void LoadChamber(int n)
    {
        StartCoroutine(LoadChamberRoutine(n));
    }

    IEnumerator LoadChamberRoutine(int n)
    {
        int index = n - 1;
        if (index < 0 || index >= chamberPrefabs.Length)
        {
            if (index >= chamberPrefabs.Length)
            {
                Ending.SetActive(true);
                Debug.Log("모든 챔버를 클리어했습니다!");
            }
            else
                Debug.LogWarning($"Chamber {n} 프리팹이 없습니다.");
            yield break;
        }

        // 기존 챔버 제거
        if (currentChamberInstance != null)
            Destroy(currentChamberInstance);

        yield return null; // Destroy 반영 대기
        Debug.Log($"{preloadedInstance} {preloadedIndex} {index}");
        // ── 새 챔버 확보 ─────────────────────────────────────────
        if (preloadedInstance != null && preloadedIndex == index)
        {
            // 프리로드된 챔버가 있으면 재사용 → Instantiate 스파이크 없음
            currentChamberInstance = preloadedInstance;
            currentChamberInstance.transform.position = Vector3.zero;

            // SetActive(false) 상태였으므로 이제 켜기
            // → Start()가 이 시점에 실행됨 (SpawnPipe.Start() 포함)
            currentChamberInstance.SetActive(true);

            preloadedInstance = null;
            preloadedIndex = -1;
        }
        else
        {
            // 프리로드 없으면 즉시 생성 (fallback)
            currentChamberInstance = Instantiate(chamberPrefabs[index]);
        }

        // SpawnPoint 탐색
        SpawnPoint sp = currentChamberInstance.GetComponentInChildren<SpawnPoint>();
        if (sp != null)
            currentSpawnPoint = sp.transform;
        else
            Debug.LogWarning($"Chamber {n}: SpawnPoint 컴포넌트를 찾지 못했습니다.");

        RespawnPlayer();
        GameManager.Instance.StartChamber(n);
    }

    // ── 프리로드 ───────────────────────────────────────────────────
    // ChamberBase.TriggerClear()에서 호출.
    // 다음 챔버를 백그라운드에서 미리 생성해두되,
    // SetActive(false)로 Start() 실행을 막아 SpawnPipe가 플레이어를
    // y=-2000으로 순간이동시키는 버그를 방지.

    public void PreloadNextChamber(int n)
    {
        int index = n - 1;
        if (index < 0 || index >= chamberPrefabs.Length) return;
        if (preloadedIndex == index) return; // 이미 프리로드됨

        StartCoroutine(PreloadRoutine(index));
    }

    IEnumerator PreloadRoutine(int index)
    {
        yield return null; // 현재 프레임 부하 분산

        // y=-2000 위치에 생성 (플레이어 눈에 안 보임)
        preloadedInstance = Instantiate(
            chamberPrefabs[index],
            new Vector3(0f, -2000f, 0f),
            Quaternion.identity
        );

        // ★ 핵심: Awake()는 실행되지만 Start()는 차단
        //   → SpawnPipe.Start()가 실행되지 않으므로 플레이어가 y=-2000으로 이동하지 않음
        preloadedInstance.SetActive(false);

        preloadedIndex = index;
        Debug.Log($"챔버 {index + 1} 프리로드 완료 (비활성 대기 중)");
    }

    // ── 리스폰 ────────────────────────────────────────────────────

    public void RespawnPlayer()
    {
        if (player == null || currentSpawnPoint == null) return;

        // SoulCore 위치만 이동 (SetSoul은 SpawnPipe가 담당)
        player.transform.position = currentSpawnPoint.position;

        if (cachedPPS == null)
            cachedPPS = FindObjectOfType<PlayerParticleSystem>();
    }
}