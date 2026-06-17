using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public enum PlayerForm { Humanoid, Soul }

public class PlayerFormController : MonoBehaviour
{
    [Header("모델")]
    public GameObject humanoidForm;
    public GameObject soulForm;

    [Header("콜라이더")]
    public BoxCollider humanoidCollider;
    public SphereCollider soulCollider;

    [Header("카메라")]
    public GameObject soutTrackingTarget;
    public GameObject humanoidTrackingTarget;
    public CinemachineCamera cCam;

    [Header("변신 불가 피드백")]
    [SerializeField] private GameObject humanoidHologram;   // 빨간 반투명 휴머노이드 메쉬
    [SerializeField] private CanvasGroup blockUIGroup;      // "변신 불가" 텍스트 CanvasGroup
    [SerializeField] private float hologramDuration = 1.5f;

    public PlayerForm currentForm { get; private set; } = PlayerForm.Soul;
    public SoulCore playerData;
    public PlayerParticleSystem pps;
    public int sizeIndex = 0;

    private PlayerInputSystem controls;
    private Vector3 defaultColliderSize;     // Inspector에서 설정한 기본 BoxCollider 크기
    private Coroutine hologramCoroutine;
    private List<PushableObject> _frozenObjects = new List<PushableObject>();

    void Awake() => controls = new PlayerInputSystem();
    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        playerData = GetComponent<SoulCore>();
        defaultColliderSize = humanoidCollider.size;

        if (humanoidHologram != null) humanoidHologram.SetActive(false);
        if (blockUIGroup != null) blockUIGroup.alpha = 0f;

        // 시작 시 Soul 형태이므로 PushableObject와 충돌 무시
        SetSoulIgnorePushables(true);
    }

    void Update()
    {
        if (!controls.Player.FormChange.triggered) return;

        if (currentForm == PlayerForm.Soul)
        {
            // Soul → Humanoid: 변신 가능 여부 먼저 체크
            if (CanTransformToHumanoid())
            {
                FormChange();
                playerData.currentForm = currentForm;
            }
            else
            {
                ShowTransformBlocked();
            }
        }
        else
        {
            // Humanoid → Soul: 항상 가능
            FormChange();
            playerData.currentForm = currentForm;
        }
    }

    // ── 변신 가능 여부 체크 ─────────────────────────────────────────────────────

    bool CanTransformToHumanoid()
    {
        // SetHumanoid() 호출 전이므로 현재 sizeIndex로 예상 크기 계산
        float predictedSize = (sizeIndex > 100)
            ? (1 + sizeIndex) / 250.0f
            : 1.0f;

        // 변신 후 콜라이더 반크기
        Vector3 halfExtents = defaultColliderSize * predictedSize * 0.5f;

        // FormChange()에서 Y오프셋을 더하는 것과 동일하게 체크 위치 설정
        Vector3 checkCenter = transform.position + Vector3.up * halfExtents.y;

        // 플레이어 자신과 트리거 제외
        Collider[] hits = Physics.OverlapBox(
            checkCenter,
            halfExtents * 0.95f,          // 5% 여유: 벽에 딱 붙은 경우도 허용
            transform.rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore // LiquidPassage, 파이프 등 트리거 무시
        );

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.TryGetComponent<PushableObject>(out _)) continue; // 얼음에 고정할 오브젝트는 허용
            return false;
        }

        return true;
    }

    void SetSoulIgnorePushables(bool ignore)
    {
        foreach (var po in FindObjectsByType<PushableObject>(FindObjectsSortMode.None))
        {
            Physics.IgnoreCollision(soulCollider, po.Col, ignore);
            if (ignore)
            {
                // 이미 쌓인 velocity 초기화
                var rb = po.GetComponent<Rigidbody>();
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    List<PushableObject> CollectFrozenCandidates()
    {
        float predictedSize = (sizeIndex > 100) ? (1 + sizeIndex) / 250.0f : 1.0f;
        Vector3 halfExtents = defaultColliderSize * predictedSize * 0.5f;
        Vector3 checkCenter = transform.position + Vector3.up * halfExtents.y;

        Collider[] hits = Physics.OverlapBox(
            checkCenter, halfExtents * 0.95f,
            transform.rotation, Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        var result = new List<PushableObject>();
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.TryGetComponent<PushableObject>(out var po))
                result.Add(po);
        }
        return result;
    }

    // ── 변신 불가 피드백 ────────────────────────────────────────────────────────

    void ShowTransformBlocked()
    {
        if (hologramCoroutine != null) StopCoroutine(hologramCoroutine);
        hologramCoroutine = StartCoroutine(HologramRoutine());
    }

    IEnumerator HologramRoutine()
    {
        // 홀로그램 크기 설정 후 활성화
        if (humanoidHologram != null)
        {
            float size = (sizeIndex > 100) ? (1 + sizeIndex) / 250.0f : 1.0f;
            humanoidHologram.transform.localScale = Vector3.one * size;
            humanoidHologram.SetActive(true);
        }

        // "변신 불가" UI 페이드 인
        if (blockUIGroup != null)
            yield return StartCoroutine(FadeUI(blockUIGroup, 0f, 1f, 0.1f));

        yield return new WaitForSeconds(hologramDuration);

        // UI 페이드 아웃
        if (blockUIGroup != null)
            yield return StartCoroutine(FadeUI(blockUIGroup, 1f, 0f, 0.3f));

        if (humanoidHologram != null)
            humanoidHologram.SetActive(false);

        hologramCoroutine = null;
    }

    IEnumerator FadeUI(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    // ── 실제 변신 (기존과 동일) ──────────────────────────────────────────────────

    void FormChange()
    {
        var targetConfig = cCam.Target;
        if (currentForm == PlayerForm.Humanoid)
        {
            foreach (var po in _frozenObjects)
                po.ReleaseFromIce(humanoidCollider);   // ★ humanoidCollider 전달
            _frozenObjects.Clear();

            GetComponent<Rigidbody>().mass = 1;
            currentForm = PlayerForm.Soul;
            humanoidForm.SetActive(false);
            soulForm.SetActive(true);
            humanoidCollider.enabled = false;
            soulCollider.enabled = true;
            SetSoulIgnorePushables(true); // soulCollider 활성화 이후에 호출해야 정상 동작
            pps.SetSoul(sizeIndex);
            sizeIndex = 0;
            targetConfig.TrackingTarget = soutTrackingTarget.transform;
            cCam.Target = targetConfig;
        }
        else
        {
            SetSoulIgnorePushables(false); // Humanoid 형태: 충돌 복원

            // 콜라이더 활성화 전에 먼저 얼려야 물리 충돌로 밀려나지 않음
            _frozenObjects = CollectFrozenCandidates();
            foreach (var po in _frozenObjects)
                po.FreezeInIce(transform, humanoidCollider);   // ★ humanoidCollider 전달

            GetComponent<Rigidbody>().mass = 4;
            currentForm = PlayerForm.Humanoid;
            humanoidForm.SetActive(true);
            soulForm.SetActive(false);
            humanoidCollider.enabled = true;
            soulCollider.enabled = false;
            sizeIndex += pps.SetHumanoid();
            if (sizeIndex > 100)
            {
                float size = (1 + sizeIndex) / 500.0f;
                humanoidForm.transform.localScale = Vector3.one * size;
                humanoidCollider.size = Vector3.one * size;
                transform.position += new Vector3(0, size / 2, 0);
            }
            targetConfig.TrackingTarget = humanoidTrackingTarget.transform;
            cCam.Target = targetConfig;
        }
    }
}