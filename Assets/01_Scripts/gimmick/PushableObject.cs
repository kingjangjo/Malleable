using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class PushableObject : MonoBehaviour
{
    [Header("이동 제한")]
    [Tooltip("X축 이동을 잠금 — 앞뒤 방향으로만 밀림")]
    public bool lockX = false;
    [Tooltip("Z축 이동을 잠금 — 좌우 방향으로만 밀림")]
    public bool lockZ = false;

    [Header("물리 설정")]
    [Tooltip("박스 무게. 높을수록 밀기 어려움")]
    public float mass = 6f;
    [Tooltip("선형 저항. 높을수록 밀다가 빨리 멈춤")]
    public float drag = 4f;
    [Tooltip("최대 수평 이동 속도")]
    public float maxSpeed = 2.5f;

    [Header("이벤트")]
    public UnityEvent onFirstMove;

    public bool HasMoved { get; private set; }
    public Vector3 StartPosition { get; private set; }
    public Collider Col { get; private set; }

    private Rigidbody rb;
    private bool isGrounded;

    // ── 얼음 고정 관련 ────────────────────────────────────────────
    private bool isFrozen;
    private Transform freezeTarget;     // 따라갈 대상 (플레이어)
    private Vector3 freezeLocalOffset;  // freezeTarget 로컬 기준 상대 위치
    private Quaternion freezeLocalRot;  // freezeTarget 로컬 기준 상대 회전

    void Awake() => Col = GetComponent<Collider>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = 999f;
        if (lockX) rb.constraints |= RigidbodyConstraints.FreezePositionX;
        if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionZ;
        if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionY;

        StartPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (isFrozen) return; // 얼어있는 동안은 일반 물리 로직 스킵

        CheckGrounded();

        Vector3 vel = rb.linearVelocity;
        Vector3 hVel = new Vector3(vel.x, 0f, vel.z);
        if (hVel.magnitude > maxSpeed)
        {
            hVel = hVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(hVel.x, vel.y, hVel.z);
        }

        if (!HasMoved)
        {
            Vector3 curH = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 iniH = new Vector3(StartPosition.x, 0f, StartPosition.z);
            if (Vector3.Distance(curH, iniH) > 0.25f)
            {
                HasMoved = true;
                onFirstMove?.Invoke();
            }
        }
    }

    // ★ 얼어있는 동안 위치를 직접 갱신 (SetParent 없이)
    // FixedUpdate 이후, 일반 LateUpdate보다 물리 갱신과 더 가깝게 맞추기 위해
    // Update 대신 사용 가능하지만 카메라 따라가는 정도면 LateUpdate가 더 안전
    void LateUpdate()
    {
        if (!isFrozen || freezeTarget == null) return;

        // 로컬 오프셋을 월드 좌표로 변환해서 그대로 따라가게 함
        transform.position = freezeTarget.TransformPoint(freezeLocalOffset);
        transform.rotation = freezeTarget.rotation * freezeLocalRot;
    }

    void CheckGrounded()
    {
        float halfH = transform.localScale.y * 0.5f;
        isGrounded = Physics.Raycast(
            transform.position, Vector3.down, halfH + 0.12f,
            LayerMask.GetMask("Default", "Ground", "Environment"));
    }

    public bool IsNear(Vector3 targetPos, float radius = 0.7f)
    {
        Vector3 a = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 b = new Vector3(targetPos.x, 0f, targetPos.z);
        return Vector3.Distance(a, b) <= radius;
    }

    // ── 얼리기 / 풀기 ─────────────────────────────────────────────

public void FreezeInIce(Transform iceParent)
    {
        if (this == null || rb == null) return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Remove the Rigidbody from physics simulation entirely
        rb.isKinematic = true;
        rb.detectCollisions = false;

        // Store relative position/rotation as local coords instead of using SetParent
        // -> doesn't touch the Transform hierarchy, so no Rigidbody nesting issues
        freezeLocalOffset = iceParent.InverseTransformPoint(transform.position);
        freezeLocalRot = Quaternion.Inverse(iceParent.rotation) * transform.rotation;
        freezeTarget = iceParent;
        isFrozen = true;
    }

public void ReleaseFromIce()
    {
        isFrozen = false;
        freezeTarget = null;

        // Object may have already been destroyed (e.g. popped by TrashObject) before this is called.
        if (this == null || rb == null) return;

        rb.detectCollisions = true;
        rb.isKinematic = false;
    }
}