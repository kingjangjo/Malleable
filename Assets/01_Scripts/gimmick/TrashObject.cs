using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 터트릴 수 있는 쓰레기 오브젝트.
/// 고체(Humanoid) 형태로 근처에서 E키 입력 시 파티클 이펙트 후 사라짐 + 점수 획득.
/// PushableObject와 별개의 컴포넌트로, 두 개를 같은 오브젝트에 같이 붙여도 되고
/// (밀 수도 있고 터트릴 수도 있는 쓰레기) 따로 써도 됩니다.
/// </summary>
public class TrashObject : MonoBehaviour
{
    [Header("점수")]
    [Tooltip("터질 때 획득하는 점수/카운트")]
    public int scoreValue = 1;

    [Header("감지 범위")]
    [Tooltip("플레이어가 이 거리 안에 있으면 터트리기 가능")]
    public float interactRadius = 1.2f;

    [Header("이펙트")]
    public ParticleSystem popEffect;     // 터지는 파티클 (프리팹 자식으로 미리 배치, 비활성 상태로 시작 가능)
    public AudioClip popSound;
    [Tooltip("이펙트 재생 후 실제로 사라지기까지 대기 시간")]
    public float destroyDelay = 0.3f;

    [Header("이벤트")]
    public static UnityEvent<int> OnAnyTrashPopped = new UnityEvent<int>(); // 전역 이벤트 (점수 매니저가 구독)

    private bool isPopped;
    private Transform playerTransform;
    private PlayerFormController playerForm;
    private PlayerInputSystem controls;

    void Start()
    {
        // 플레이어 참조를 한 번만 캐싱 (매 프레임 Find 방지)
        var pfc = FindAnyObjectByType<PlayerFormController>();
        if (pfc != null)
        {
            playerForm = pfc;
            playerTransform = pfc.transform;
        }
    }

    void Update()
    {
        if (isPopped) return;
        if (playerForm == null || playerTransform == null) return;

        // 고체 상태가 아니면 터트릴 수 없음
        if (playerForm.currentForm != PlayerForm.Humanoid) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist > interactRadius) return;

        // E키 입력 체크 (PlayerInputSystem에 Interact 액션이 있다고 가정)
        // 없다면 Input.GetKeyDown(KeyCode.E)로 대체 가능
        if (controls.Player.Interaction.triggered)
        {
            Pop();
        }
    }

    void Pop()
    {
        if (isPopped) return;
        isPopped = true;

        if (popEffect != null)
        {
            popEffect.transform.SetParent(null); // 오브젝트보다 이펙트가 더 오래 남아야 하므로 분리
            popEffect.Play();
        }

        if (popSound != null)
            AudioSource.PlayClipAtPoint(popSound, transform.position);

        OnAnyTrashPopped?.Invoke(scoreValue);

        // 콜라이더/렌더러 즉시 비활성화 (이펙트는 따로 재생 중)
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Renderer>(out var rend)) rend.enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}