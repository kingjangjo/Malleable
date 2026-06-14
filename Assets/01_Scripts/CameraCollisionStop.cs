using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraCollisionStop : MonoBehaviour
{
    [Header("충돌 설정")]
    public LayerMask collisionLayers = ~0; // 충돌할 레이어
    public float cameraRadius = 0.3f;      // 카메라 구체 크기
    public float minDistance = 1.0f;       // 플레이어와 최소 거리
    public float desiredDistance = 5.0f;   // 원하는 거리

    [Header("부드러움")]
    public float damping = 5f;

    private CinemachineCamera _vcam;
    private Transform _follow;
    private float _currentDistance;

    void Awake()
    {
        _vcam = GetComponent<CinemachineCamera>();
        _currentDistance = desiredDistance;
    }

    void LateUpdate()
    {
        if (_vcam.Follow == null) return;
        _follow = _vcam.Follow;

        Vector3 dirToCamera = (transform.position - _follow.position).normalized;
        float targetDistance = desiredDistance;

        Vector3 origin = _follow.position + dirToCamera * 0.5f;

        // SphereCast로 카메라 경로에 장애물 감지
        if (Physics.SphereCast(
            origin,
            cameraRadius,
            dirToCamera,
            out RaycastHit hit,
            desiredDistance - 0.5f,
            collisionLayers))
        {
            // 막히면 그 거리에서 멈춤 (당기지 않음)
            targetDistance = Mathf.Max(hit.distance, minDistance);
        }

        // 부드럽게 거리 변화 (늘어날 땐 천천히, 줄어들 땐 빠르게)
        if (targetDistance < _currentDistance)
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * damping * 2f);
        else
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * damping * 0.5f);

        // 카메라 위치 적용
        transform.position = _follow.position + dirToCamera * _currentDistance;
    }
}