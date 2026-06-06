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

    // [수정] Awake에서 캐싱
    // 문제: Start()는 모든 오브젝트가 동시에 실행되므로 순서가 보장되지 않음
    //       GameManager.Start()가 ChamberManager.Start()보다 먼저 실행되면
    //       LoadChamber() 호출 시 cachedPPS가 아직 null인 상태
    // 해결: Awake()는 Start()보다 항상 먼저 실행되므로 여기서 캐싱
    //       GameManager.Start() → ChamberManager.LoadChamber()가 불려도 이미 준비된 상태
    private PlayerParticleSystem cachedPPS;
    private PlayerFormController cachedFormController; 
    private GameObject preloadedInstance;
    private int preloadedIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Awake에서 씬 전체 탐색으로 캐싱 (Start보다 먼저 실행 보장)
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
        cachedFormController = FindObjectOfType<PlayerFormController>();

        if (cachedPPS == null)
            Debug.LogWarning("ChamberManager: PlayerParticleSystem을 찾지 못했습니다.");
    }
    public void PreloadNextChamber(int n)
    {
        int index = n - 1;
        if (index < 0 || index >= chamberPrefabs.Length) return;
        if (preloadedIndex == index) return; // 이미 프리로드됨

        StartCoroutine(PreloadRoutine(index));
    }

    // ── 챔버 로드 ──────────────────────────────────────────────────

    public void LoadChamber(int n)
    {
        StartCoroutine(LoadChamberRoutine(n));
    }
    IEnumerator PreloadRoutine(int index)
    {
        yield return null; // 한 프레임 대기 (현재 프레임 부하 분산)

        // 플레이어에게 안 보이는 먼 위치에 생성
        // → Start()가 즉시 실행되지만 플레이어와 멀어서 영향 없음
        preloadedInstance = Instantiate(
            chamberPrefabs[index],
            new Vector3(0f, -2000f, 0f), // 맵 아래 멀리
            Quaternion.identity
        );
        preloadedIndex = index;

        Debug.Log($"챔버 {index + 1} 프리로드 완료");
    }


    IEnumerator LoadChamberRoutine(int n)
    {
        int index = n - 1;
        if (index < 0 || index >= chamberPrefabs.Length)
        {
            if (index >= chamberPrefabs.Length) Debug.Log("모든 챔버 클리어!");
            yield break;
        }

        GameManager.Instance.LockInput(0.8f);

        if (currentChamberInstance != null)
            Destroy(currentChamberInstance);

        yield return null;

        // 프리로드된 챔버가 있으면 재사용 — Instantiate 스파이크 없음
        if (preloadedInstance != null && preloadedIndex == index)
        {
            currentChamberInstance = preloadedInstance;
            currentChamberInstance.transform.position = Vector3.zero; // 원위치
            preloadedInstance = null;
            preloadedIndex = -1;
        }
        else
        {
            // 프리로드 없으면 기존 방식 (fallback)
            currentChamberInstance = Instantiate(chamberPrefabs[index]);
        }

        SpawnPoint sp = currentChamberInstance.GetComponentInChildren<SpawnPoint>();
        if (sp != null) currentSpawnPoint = sp.transform;
        else Debug.LogWarning($"Chamber {n}: SpawnPoint 없음");

        RespawnPlayer();
        GameManager.Instance.StartChamber(n);
    }

    // ── 리스폰 ────────────────────────────────────────────────────

    public void RespawnPlayer()
    {
        if (player == null || currentSpawnPoint == null) return;

        // [수정] SoulCore 위치만 이동 (SetSoul은 SpawnPipe가 담당)
        // 이유: RespawnPlayer와 SpawnPipe 둘 다 SetSoul을 호출하면
        //       한 프레임에 입자가 두 번 초기화되어 프레임 스파이크 발생
        player.transform.position = currentSpawnPoint.position;

        if (cachedPPS == null)
            cachedPPS = FindObjectOfType<PlayerParticleSystem>();
    }
}