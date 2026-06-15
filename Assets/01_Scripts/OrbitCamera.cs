using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("타겟")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 0.5f, 0f); // 카메라가 바라보는 높이

    [Header("거리")]
    public float distance = 5f;
    public float minDistance = 0.5f;

    [Header("회전 속도")]
    public float mouseSpeedX = 3f;
    public float mouseSpeedY = 2f;

    [Header("수직 각도 제한")]
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    [Header("충돌")]
    public LayerMask collisionLayers;
    public float cameraRadius = 0.15f;          // 작게 해야 벽에 가까이 붙음

    [Header("거리 보간 속도")]
    public float pullInSpeed = 15f;            // 당겨지는 속도 (빠르게)
    public float pullOutSpeed = 3f;             // 복귀 속도 (느리게)

    private float _yaw;
    private float _pitch;
    private float _currentDistance;            // 실제 현재 거리

    void Start()
    {
        _yaw = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;
        _currentDistance = distance;

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

        // ★ 핵심: pivotPos를 올리기 전에 천장 체크
        Vector3 safeOffset = GetSafePivotOffset();
        Vector3 pivotPos = target.position + safeOffset;

        float targetDistance = GetTargetDistance(pivotPos, rotation);

        if (targetDistance < _currentDistance)
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * pullInSpeed);
        else
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * pullOutSpeed);

        transform.position = pivotPos + rotation * new Vector3(0f, 0f, -_currentDistance);
        transform.rotation = rotation;
    }

    // ★ 추가된 함수: offset 올리기 전에 천장 체크
    Vector3 GetSafePivotOffset()
    {
        float wantedOffsetY = targetOffset.y;

        // 플레이어에서 위로 올릴 때 천장에 막히는지 체크
        if (Physics.SphereCast(
                target.position,        // 플레이어 위치에서
                cameraRadius,
                Vector3.up,             // 위 방향으로
                out RaycastHit hit,
                wantedOffsetY,
                collisionLayers))
        {
            // 천장에 막히면 그 아래까지만 올림
            float safeY = Mathf.Max(hit.distance - cameraRadius, 0f);
            return new Vector3(targetOffset.x, safeY, targetOffset.z);
        }

        return targetOffset; // 안 막히면 원래 offset
    }

    float GetTargetDistance(Vector3 pivotPos, Quaternion rotation)
    {
        Vector3 dir = rotation * Vector3.back; // 카메라 뒤 방향

        // SphereCast: 피벗에서 카메라 방향으로 쏨
        if (Physics.SphereCast(
                pivotPos,           // 피벗(머리 위)에서 시작 → 플레이어 콜라이더 안 걸림
                cameraRadius,
                dir,
                out RaycastHit hit,
                distance,
                collisionLayers))
        {
            // 벽에 닿으면 그 거리로 당김 (cameraRadius만큼 여유)
            return Mathf.Max(hit.distance - cameraRadius, minDistance);
        }

        return distance; // 안 닿으면 원래 거리 복귀
    }
}