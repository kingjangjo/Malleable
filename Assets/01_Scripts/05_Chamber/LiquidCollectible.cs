using UnityEngine;

// 기믹 4: 액체 수집 기믹
// 플레이어(Soul 상태)가 닿으면 입자를 추가로 획득
// "수확" 계열 퍼즐에 사용
// 방에 여러 개 배치해서 전부 수집해야 클리어하는 퍼즐에 활용
public class LiquidCollectible : MonoBehaviour
{
    [Header("Settings")]
    public int particlesToAdd = 20;      // 수집 시 추가되는 입자 수
    public bool requireSoulForm = true;  // Soul 상태에서만 수집 가능

    [Header("Visual")]
    public float bobSpeed = 1.5f;        // 위아래로 흔들리는 속도
    public float bobHeight = 0.2f;       // 흔들리는 높이

    private Vector3 startPos;
    private PlayerParticleSystem cachedPPS;
    private bool collected = false;

    void Start()
    {
        startPos = transform.position;
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
    }

    void Update()
    {
        if (collected) return;
        // 위아래로 둥실둥실 움직이는 연출
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, y, startPos.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("SoulCore")) return;

        if (requireSoulForm)
        {
            var form = other.GetComponentInParent<PlayerFormController>();
            if (form == null || form.currentForm != PlayerForm.Soul) return;
        }

        Collect(other.transform.position);
    }

    void Collect(Vector3 spawnCenter)
    {
        collected = true;

        if (cachedPPS != null)
        {
            // SoulCore 주변에 새 입자 생성
            for (int i = 0; i < particlesToAdd; i++)
            {
                Vector3 pos = spawnCenter + Random.insideUnitSphere * 0.5f;
                pos.y = Mathf.Max(pos.y, cachedPPS.groundY + cachedPPS.particleRadius);
                cachedPPS.particles.Add(new Particle(pos));
            }
        }

        Debug.Log($"{gameObject.name} 수집! +{particlesToAdd} 입자");
        gameObject.SetActive(false);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.8f, 1f, 0.6f);
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}