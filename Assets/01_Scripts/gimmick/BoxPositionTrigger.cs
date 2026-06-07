using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 박스가 목표 위치(targetZone)에 도달했을 때 이벤트를 발사합니다.
/// 발판/버튼/압력판 등 "박스를 특정 위치에 밀어야 하는" 기믹 전반에 사용합니다.
/// </summary>
public class BoxPositionTrigger : MonoBehaviour
{
    [Header("박스 & 목표 위치")]
    public PushableObject box;
    [Tooltip("빈 GameObject. 박스가 이 위치에 오면 활성화")]
    public Transform targetZone;
    public float acceptRadius = 0.7f;

    [Header("시각 안내")]
    [Tooltip("목표 위치를 알려주는 시각 오브젝트 (도착하면 숨김)")]
    public GameObject zoneIndicator;

    [Header("이벤트")]
    public UnityEvent onBoxArrived;
    public UnityEvent onBoxLeft;

    public bool IsActivated { get; private set; }

    void Start()
    {
        if (zoneIndicator != null) zoneIndicator.SetActive(true);
    }

    void Update()
    {
        if (box == null || targetZone == null) return;

        bool inZone = box.IsNear(targetZone.position, acceptRadius);

        if (inZone && !IsActivated)
        {
            IsActivated = true;
            if (zoneIndicator != null) zoneIndicator.SetActive(false);
            onBoxArrived?.Invoke();
        }
        else if (!inZone && IsActivated)
        {
            IsActivated = false;
            if (zoneIndicator != null) zoneIndicator.SetActive(true);
            onBoxLeft?.Invoke();
        }
    }
}