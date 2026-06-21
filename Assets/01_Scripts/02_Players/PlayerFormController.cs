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

    [Header("변신 시 위치 보정 (바닥 끼임 방지)")]
    [Tooltip("Soul -> Humanoid 변신 시, 발밑 바닥까지의 거리를 측정할 레이캐스트 최대 거리")]
    public float groundCheckDistance = 3f;
    [Tooltip("발밑 바닥까지의 거리가 이 값보다 작으면 '바닥에 붙어있음'으로 간주해 위치를 들어올림. 이 값보다 크면 '공중'으로 간주해 보정 없이 현재 위치 그대로 변신")]
    public float minAirborneHeight = 0.5f;
    [Tooltip("바닥에 붙어있을 때 변신 시 추가로 들어올릴 높이")]
    public float groundedRiseOffset = 0.6f;
    [Tooltip("바닥 감지에 사용할 레이어 (Ground, Environment 등)")]
    public LayerMask groundCheckLayers = ~0;

    
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

        // 바닥에 붙어있으면 들어올린 위치를 기준으로, 공중이면 현재 위치 그대로 기준으로 체크
        float riseOffset = CalculateGroundRiseOffset();
        Vector3 basePosition = transform.position + Vector3.up * riseOffset;
        Vector3 checkCenter = basePosition + Vector3.up * halfExtents.y;

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


    // Calculates the Y offset to use when transforming Soul -> Humanoid.
    // If the feet are nearly touching the ground (distance < minAirborneHeight),
    // rise by groundedRiseOffset. If already airborne (distance >= minAirborneHeight),
    // return 0 so the current position is used as-is.
    [Header("바닥 감지")]
    [Tooltip("Raycast 시작점을 위로 띄우는 보정값 (자기 콜라이더 통과 방지)")]
    public float rayStartOffset = 0.15f;

    float CalculateGroundRiseOffset()
    {
        // ★ 시작점을 위로 띄워서 발사
        Vector3 rayStart = transform.position + Vector3.up * rayStartOffset;

        bool didHit = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
            groundCheckDistance + rayStartOffset, groundCheckLayers,
            QueryTriggerInteraction.Ignore);

        Debug.Log($"[GroundCheck] hit={didHit} | distance={(didHit ? hit.distance.ToString("F3") : "N/A")} | " +
                  $"hitObj={(didHit ? hit.collider.name : "없음")}");

        if (didHit)
        {
            float distanceToGround = hit.distance - rayStartOffset;
            if (distanceToGround < minAirborneHeight)
                return groundedRiseOffset;
        }

        return 0f;
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

        // ★ 변경: riseOffset 빼고 현재 위치 그대로 기준
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
                po.ReleaseFromIce();
            _frozenObjects.Clear();

            GetComponent<Rigidbody>().mass = 1;
            currentForm = PlayerForm.Soul;
            humanoidForm.SetActive(false);
            soulForm.SetActive(true);
            humanoidCollider.enabled = false;
            soulCollider.enabled = true;
            SetSoulIgnorePushables(true);
            pps.SetSoul(sizeIndex);
            sizeIndex = 0;
            targetConfig.TrackingTarget = soutTrackingTarget.transform;
            cCam.Target = targetConfig;
        }
        else
        {
            SetSoulIgnorePushables(false);

            // ★ 변경: 수집을 먼저 함 (들어올리기 전, 바닥에 있는 그대로의 위치 기준)
            _frozenObjects = CollectFrozenCandidates();
            foreach (var po in _frozenObjects)
                po.FreezeInIce(transform);

            // ★ 변경: 수집 끝난 다음에 들어올림
            float riseOffset = CalculateGroundRiseOffset();
            if (riseOffset > 0f)
            {
                transform.position += Vector3.up * riseOffset;
            }

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