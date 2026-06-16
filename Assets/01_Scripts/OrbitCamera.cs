using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("타겟")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 2.0f, 0f);

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
    public float cameraRadius = 0.15f;

    [Header("거리 보간 속도")]
    public float pullInSpeed = 15f;
    public float pullOutSpeed = 3f;

    private float _yaw;
    private float _pitch;
    private float _currentDistance;

    void Start()
    {
        _yaw = transform.eulerAngles.y;
        _pitch = 20f; // 살짝 내려다보기
        _currentDistance = distance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 마우스 입력
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSpeedX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSpeedY;

        _yaw += mouseX;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 safeOffset = GetSafePivotOffset();
        Vector3 pivotPos = target.position + safeOffset;

        // 1차: SphereCast로 목표 거리 계산
        float targetDistance = GetTargetDistance(pivotPos, rotation);

        // 보간
        if (targetDistance < _currentDistance)
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * pullInSpeed);
        else
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * pullOutSpeed);

        // 최종 카메라 위치 계산
        Vector3 desiredPos = pivotPos + rotation * new Vector3(0f, 0f, -_currentDistance);

        // 2차: 최종 위치가 벽 안인지 OverlapSphere로 검증
        desiredPos = ValidateCameraPosition(pivotPos, desiredPos, rotation);

        transform.position = desiredPos;
        transform.rotation = rotation;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ★ 2차 검증: 카메라 위치가 벽 안에 있으면 강제로 밀어냄
    Vector3 ValidateCameraPosition(Vector3 pivotPos, Vector3 desiredPos, Quaternion rotation)
    {
        Collider[] overlaps = Physics.OverlapSphere(desiredPos, cameraRadius, collisionLayers);

        if (overlaps.Length == 0) return desiredPos; // 겹치는 거 없으면 그대로

        // 겹치면 pivot 방향으로 당기기
        Vector3 dir = (desiredPos - pivotPos).normalized;
        float totalDist = Vector3.Distance(pivotPos, desiredPos);

        // pivot에서 카메라 방향으로 짧게 짧게 쪼개서 안전한 위치 탐색
        int steps = 20;
        float stepSize = totalDist / steps;

        for (int i = steps - 1; i >= 0; i--)
        {
            Vector3 checkPos = pivotPos + dir * (stepSize * i);
            if (Physics.OverlapSphere(checkPos, cameraRadius, collisionLayers).Length == 0)
            {
                _currentDistance = stepSize * i; // 현재 거리도 업데이트
                return checkPos;
            }
        }

        // 모든 위치가 막히면 pivot에 붙임
        _currentDistance = minDistance;
        return pivotPos + dir * minDistance;
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

    float GetTargetDistance(Vector3 pivotPos, Quaternion rotation)
    {
        Vector3 dir = rotation * Vector3.back;

        if (Physics.SphereCast(
                pivotPos,
                cameraRadius,
                dir,
                out RaycastHit hit,
                distance,
                collisionLayers))
        {
            return Mathf.Max(hit.distance - cameraRadius, minDistance);
        }

        return distance;
    }
}