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
                Debug.Log("모든 챔버를 클리어했습니다!");
            else
                Debug.LogWarning($"Chamber {n} 프리팹이 없습니다.");
            yield break;
        }

        GameManager.Instance.LockInput(0.8f);

        // 기존 챔버 제거
        if (currentChamberInstance != null)
            Destroy(currentChamberInstance);

        yield return null; // Destroy 반영 대기

        // 새 챔버 생성
        currentChamberInstance = Instantiate(chamberPrefabs[index]);

        // [수정] SpawnPoint를 챔버 인스턴스 하위에서만 탐색
        // 이유: FindWithTag는 씬 전체를 뒤져서 이전 챔버 잔재와 혼동될 수 있음
        SpawnPoint sp = currentChamberInstance.GetComponentInChildren<SpawnPoint>();
        if (sp != null)
            currentSpawnPoint = sp.transform;
        else
            Debug.LogWarning($"Chamber {n}: SpawnPoint 컴포넌트를 찾지 못했습니다.");

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