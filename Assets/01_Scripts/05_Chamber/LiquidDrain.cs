using UnityEngine;
using UnityEngine.Events;
using TMPro;

// 기믹 3: 액체 소모 기믹
// E키를 눌러 자신의 입자(액체)를 소모해서 기믹 활성화
// "희생" 계열 퍼즐에 사용
public class LiquidDrain : MonoBehaviour
{
    [Header("Settings")]
    public int requiredParticles = 50;   // 총 얼마나 소모해야 하는지
    public int drainPerPress = 10;       // E키 한 번에 소모량
    public int minParticlesRemaining = 20; // 이 수량 이하로는 소모 불가 (플레이어 보호)
    public KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    public TextMeshProUGUI progressText;  // "30 / 50" 형태로 표시 (없어도 됨)
    public GameObject interactPrompt;    // "E: 액체를 흘려보내기" 프롬프트

    [Header("Events")]
    public UnityEvent onDrainComplete;

    public int CurrentDrained { get; private set; } = 0;
    public bool IsComplete { get; private set; } = false;

    private bool playerNearby = false;
    private PlayerParticleSystem cachedPPS;

    void Awake()
    {
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;
        playerNearby = true;
        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SoulCore")) return;
        playerNearby = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (!playerNearby || IsComplete) return;
        if (GameManager.Instance != null && GameManager.Instance.inputLocked) return;
        if (!Input.GetKeyDown(interactKey)) return;
        if (cachedPPS == null) return;

        // 소모 가능한 양 계산 (최소 보유량 확보)
        int available = cachedPPS.particles.Count - minParticlesRemaining;
        int remaining = requiredParticles - CurrentDrained;
        int toDrain = Mathf.Min(drainPerPress, Mathf.Min(available, remaining));

        if (toDrain <= 0)
        {
            Debug.Log("입자가 부족합니다!");
            return;
        }

        // 뒤에서부터 입자 제거 (자연스러운 감소 효과)
        for (int i = 0; i < toDrain; i++)
            cachedPPS.particles.RemoveAt(cachedPPS.particles.Count - 1);

        CurrentDrained += toDrain;
        UpdateUI();

        if (CurrentDrained >= requiredParticles)
        {
            IsComplete = true;
            if (interactPrompt != null) interactPrompt.SetActive(false);
            onDrainComplete?.Invoke();
            Debug.Log($"{gameObject.name}: 소모 완료! ({CurrentDrained}/{requiredParticles})");
        }
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"{CurrentDrained} / {requiredParticles}";
    }
}