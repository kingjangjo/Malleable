using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물 형태 플레이어 이동 시 랜덤 발소리 재생 스크립트
/// - 공중(바닥 미접지) 상태에서는 발소리 재생 안 함
/// - 착지 순간 높은 피치의 튕기는 소리 재생
/// AudioSource 컴포넌트와 함께 사용하세요.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WaterPlayerSound : MonoBehaviour
{
    [Header("발소리 클립 리스트 (5개)")]
    public List<AudioClip> footstepClips = new List<AudioClip>();

    [Header("이동 소리 설정")]
    [Tooltip("발소리 재생 최소 간격 (초)")]
    public float stepInterval = 0.3f;

    [Tooltip("이동 판정 최소 속도 (이 값 이상일 때 소리 재생)")]
    public float minMoveSpeed = 0.1f;

    [Tooltip("볼륨 (0~1)")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Tooltip("피치 랜덤 범위 (+/-) — 같은 소리도 조금씩 다르게 들림")]
    [Range(0f, 0.3f)]
    public float pitchVariation = 0.1f;

    [Header("착지 소리 설정")]
    [Tooltip("착지 시 기본 피치 배율 (1보다 클수록 높은 소리)")]
    [Range(1.1f, 3f)]
    public float landingPitchMultiplier = 1.8f;

    [Tooltip("낙하 속도에 따라 피치를 추가로 높이는 배율")]
    [Range(0f, 0.2f)]
    public float landingPitchBySpeed = 0.05f;

    [Tooltip("착지 소리 볼륨 배율 (기본 볼륨 대비)")]
    [Range(0.5f, 2f)]
    public float landingVolumeMultiplier = 1.2f;

    [Header("바닥 감지 설정")]
    [Tooltip("바닥 감지에 사용할 레이어 마스크")]
    public LayerMask groundLayer = ~0;   // 기본값: 모든 레이어

    [Tooltip("발 아래 Raycast 거리 (캡슐/구 콜라이더 반지름보다 약간 크게)")]
    public float groundCheckDistance = 0.15f;

    [Tooltip("Raycast 시작 오프셋 (콜라이더 중심 기준 아래 방향)")]
    public float groundCheckOriginOffset = 0.5f;

    // ── 내부 변수 ──────────────────────────────────────────────
    private AudioSource _audioSource;
    private float _stepTimer = 0f;
    private int _lastClipIndex = -1;

    private Rigidbody _rb;
    private Rigidbody2D _rb2D;
    private Vector3 _prevPosition;

    private bool _wasGrounded = true;   // 이전 프레임 접지 여부
    private bool _isGrounded = true;   // 현재 프레임 접지 여부
    private float _fallVelocity = 0f;     // 착지 직전 낙하 속도 기록용

    public PlayerFormController pfc;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        _rb = GetComponent<Rigidbody>();
        _rb2D = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        _prevPosition = transform.position;

        if (footstepClips == null || footstepClips.Count == 0)
            Debug.LogWarning("[WaterPlayerSound] footstepClips 리스트가 비어 있습니다. Inspector에서 클립을 할당해 주세요.");
    }

    void Update()
    {
        if (pfc.currentForm == PlayerForm.Humanoid) return;
        _stepTimer -= Time.deltaTime;

        // ── 1. 낙하 속도 기록 (착지 직전 속도를 피치에 반영하기 위해) ──
        float verticalVelocity = GetVerticalVelocity();
        if (verticalVelocity < 0f)                  // 아래로 떨어지는 중
            _fallVelocity = Mathf.Abs(verticalVelocity);

        // ── 2. 접지 감지 ──────────────────────────────────────────
        _wasGrounded = _isGrounded;
        _isGrounded = CheckGrounded();

        // ── 3. 착지 순간 감지 (공중 → 접지) ──────────────────────
        if (!_wasGrounded && _isGrounded)
        {
            PlayLandingSound();
            _stepTimer = stepInterval;   // 착지 직후 발소리 쿨타임 초기화
            _fallVelocity = 0f;
            _prevPosition = transform.position;
            return;
        }

        // ── 4. 지상 이동 발소리 (공중이면 완전히 스킵) ───────────
        if (!_isGrounded)
        {
            _prevPosition = transform.position;
            return;
        }

        float horizontalSpeed = GetHorizontalSpeed();
        if (horizontalSpeed >= minMoveSpeed && _stepTimer <= 0f)
        {
            PlayRandomFootstep();
            _stepTimer = stepInterval;
        }

        _prevPosition = transform.position;
    }

    // ── 바닥 감지 (Raycast) ────────────────────────────────────
    bool CheckGrounded()
    {
        Vector3 origin = transform.position - Vector3.up * groundCheckOriginOffset;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayer)
            // 2D 지원
            || (_rb2D != null && Physics2D.Raycast(
                    (Vector2)transform.position - Vector2.up * groundCheckOriginOffset,
                    Vector2.down, groundCheckDistance, groundLayer));
    }

    // ── 수직 속도 반환 ─────────────────────────────────────────
    float GetVerticalVelocity()
    {
        if (_rb != null) return _rb.linearVelocity.y;
        if (_rb2D != null) return _rb2D.linearVelocity.y;
        // Transform 기반: 이전 프레임 y 변화량
        return (transform.position.y - _prevPosition.y) / Time.deltaTime;
    }

    // ── 수평 속도 반환 (지상 이동 판정용) ─────────────────────
    float GetHorizontalSpeed()
    {
        if (_rb != null)
        {
            Vector3 hv = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            return hv.magnitude;
        }
        if (_rb2D != null)
            return Mathf.Abs(_rb2D.linearVelocity.x);

        // Transform 기반
        Vector3 delta = transform.position - _prevPosition;
        delta.y = 0f;
        return delta.magnitude / Time.deltaTime;
    }

    // ── 일반 발소리 재생 ──────────────────────────────────────
    void PlayRandomFootstep()
    {
        if (footstepClips == null || footstepClips.Count == 0) return;

        int index = GetRandomIndexExcluding(_lastClipIndex);
        _lastClipIndex = index;

        AudioClip clip = footstepClips[index];
        if (clip == null) return;

        _audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        _audioSource.volume = volume;
        _audioSource.PlayOneShot(clip);
    }

    // ── 착지 소리 재생 (높은 피치 + 볼륨 강조) ───────────────
    void PlayLandingSound()
    {
        if (footstepClips == null || footstepClips.Count == 0) return;

        int index = GetRandomIndexExcluding(_lastClipIndex);
        _lastClipIndex = index;

        AudioClip clip = footstepClips[index];
        if (clip == null) return;

        // 낙하 속도가 빠를수록 피치를 더 높임
        float speedBonus = _fallVelocity * landingPitchBySpeed;
        float finalPitch = landingPitchMultiplier
                            + Random.Range(-pitchVariation, pitchVariation)
                            + speedBonus;

        _audioSource.pitch = Mathf.Clamp(finalPitch, 1f, 4f);
        _audioSource.volume = Mathf.Clamp01(volume * landingVolumeMultiplier);
        _audioSource.PlayOneShot(clip);
    }

    // ── 직전 인덱스 제외 랜덤 선택 ────────────────────────────
    int GetRandomIndexExcluding(int exclude)
    {
        if (footstepClips.Count == 1) return 0;

        int index;
        do { index = Random.Range(0, footstepClips.Count); }
        while (index == exclude);

        return index;
    }

    // ── 외부에서 강제 재생 ─────────────────────────────────────
    public void ForcePlayFootstep() { PlayRandomFootstep(); _stepTimer = stepInterval; }
    public void ForcePlayLanding() { PlayLandingSound(); }

    // ── 디버그용 Gizmo ─────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position - Vector3.up * groundCheckOriginOffset;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, 0.05f);
    }
}