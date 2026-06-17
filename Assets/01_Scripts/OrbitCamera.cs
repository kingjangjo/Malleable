using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("타겟")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 2.0f, 0f);

    [Header("거리")]
    public float distance = 5f;
    public float minDistance = 0f;

    [Header("회전 속도")]
    public float mouseSpeedX = 3f;
    public float mouseSpeedY = 2f;

    [Header("수직 각도 제한")]
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    [Header("충돌")]
    public LayerMask collisionLayers;
    public float cameraRadius = 0.2f;

    [Header("거리 보간")]
    public float pullInSpeed = 25f;
    public float pullOutSpeed = 4f;

    private float _yaw;
    private float _pitch;
    private float _currentDistance;
    private Camera _cam;
    private SphereCollider _dummyCollider;

    void Start()
    {
        _yaw = transform.eulerAngles.y;
        _pitch = 20f;
        _currentDistance = distance;
        _cam = GetComponent<Camera>();

        if (_cam != null) _cam.nearClipPlane = 0.01f;

        // ComputePenetration용 더미 콜라이더
        _dummyCollider = gameObject.AddComponent<SphereCollider>();
        _dummyCollider.radius = cameraRadius;
        _dummyCollider.isTrigger = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSpeedX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSpeedY;

        _yaw += mouseX;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivot = target.position + GetSafePivotOffset();

        float targetDist = GetSafeDistance(pivot, rotation);

        float lerpSpeed = targetDist < _currentDistance ? pullInSpeed : pullOutSpeed;
        _currentDistance = Mathf.Lerp(_currentDistance, targetDist, Time.deltaTime * lerpSpeed);
        _currentDistance = Mathf.Clamp(_currentDistance, minDistance, distance);

        Vector3 finalPos = pivot + rotation * new Vector3(0f, 0f, -_currentDistance);

        // ★ 코너 뚫림 방지: 최종 위치 강제 보정
        finalPos = PushOutOfWall(finalPos, pivot);

        transform.position = finalPos;
        transform.rotation = rotation;

        if (_cam != null)
            _cam.nearClipPlane = Mathf.Lerp(0.01f, 0.3f, _currentDistance / distance);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    Vector3 PushOutOfWall(Vector3 camPos, Vector3 pivot)
    {
        for (int i = 0; i < 5; i++)
        {
            Collider[] overlaps = Physics.OverlapSphere(camPos, cameraRadius, collisionLayers);
            if (overlaps.Length == 0) break;

            foreach (Collider col in overlaps)
            {
                if (col == _dummyCollider) continue;  // 자기 자신 무시

                if (Physics.ComputePenetration(
                        _dummyCollider,
                        camPos,
                        Quaternion.identity,
                        col,
                        col.transform.position,
                        col.transform.rotation,
                        out Vector3 pushDir,
                        out float pushDist))
                {
                    camPos += pushDir * (pushDist + 0.01f);
                }
            }
        }

        _currentDistance = Mathf.Clamp(Vector3.Distance(camPos, pivot), 0f, distance);
        return camPos;
    }

    Vector3 GetSafePivotOffset()
    {
        if (Physics.SphereCast(
                target.position,
                cameraRadius,
                Vector3.up,
                out RaycastHit hit,
                targetOffset.y,
                collisionLayers))
        {
            float safeY = Mathf.Max(hit.distance - cameraRadius, 0f);
            return new Vector3(targetOffset.x, safeY, targetOffset.z);
        }
        return targetOffset;
    }

    float GetSafeDistance(Vector3 pivot, Quaternion rotation)
    {
        Vector3 dir = rotation * Vector3.back;
        float safeDist = distance;

        Vector3[] offsets =
        {
            Vector3.zero,
            rotation * new Vector3( cameraRadius, 0,           0),
            rotation * new Vector3(-cameraRadius, 0,           0),
            rotation * new Vector3(0,             cameraRadius, 0),
            rotation * new Vector3(0,            -cameraRadius, 0),
        };

        foreach (Vector3 offset in offsets)
        {
            if (Physics.Raycast(
                    pivot + offset,
                    dir,
                    out RaycastHit hit,
                    distance,
                    collisionLayers))
            {
                float d = Mathf.Max(hit.distance - cameraRadius, 0f);
                if (d < safeDist) safeDist = d;
            }
        }

        return safeDist;
    }
}