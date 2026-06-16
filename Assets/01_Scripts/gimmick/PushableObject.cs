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

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = 999f;
        //rb.constraints = RigidbodyConstraints.FreezeRotation;
        if (lockX) rb.constraints |= RigidbodyConstraints.FreezePositionX;
        if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionZ;
        if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionY;

        StartPosition = transform.position;
    }
    public Collider Col { get; private set; }

    void Awake() => Col = GetComponent<Collider>();

    void FixedUpdate()
    {
        CheckGrounded();

        //if (isGrounded)
        //    rb.constraints |= RigidbodyConstraints.FreezePositionY;
        //else
        //    rb.constraints &= ~RigidbodyConstraints.FreezePositionY;

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

    public void FreezeInIce(Transform iceParent)
    {
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        transform.SetParent(iceParent);
    }

    public void ReleaseFromIce()
    {
        transform.SetParent(null);
        rb.constraints = RigidbodyConstraints.None;
        rb.angularDamping = 999f;
        if (lockX) rb.constraints |= RigidbodyConstraints.FreezePositionX;
        if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionZ;
        if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionY;
        rb.isKinematic = false;
    }
}