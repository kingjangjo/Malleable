// SpawnPipe.cs 수정 — SetSoul 호출 전 한 프레임 더 대기
// Instantiate 직후 Start()가 바로 실행되므로 챔버 로드 완료까지 대기
using UnityEngine;
using System.Collections;

public class SpawnPipe : MonoBehaviour
{
    [Header("Settings")]
    public float spawnDelay = 0.5f;    // 0.3 → 0.5로 증가 (챔버 로드 여유시간)
    public float spawnOutSpeed = 2f;

    private PlayerParticleSystem cachedPPS;
    private GameObject cachedPlayer;

    void Start()
    {
        cachedPPS = FindObjectOfType<PlayerParticleSystem>();
        cachedPlayer = GameObject.FindWithTag("SoulCore");
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // [수정] Instantiate 직후 Start()가 바로 실행되므로
        // 챔버 로드 루틴이 완전히 끝날 때까지 대기
        yield return new WaitForEndOfFrame();       // 현재 프레임 렌더링 끝날 때까지
        yield return new WaitForSeconds(spawnDelay); // 추가 딜레이

        if (cachedPlayer == null || cachedPPS == null)
        {
            // [수정] Start() 이후 씬이 바뀌었을 수 있으므로 재탐색
            cachedPPS = FindObjectOfType<PlayerParticleSystem>();
            cachedPlayer = GameObject.FindWithTag("SoulCore");
        }

        if (cachedPlayer == null || cachedPPS == null) yield break;

        cachedPlayer.transform.position = transform.position;

        int count = cachedPPS.particles.Count > 0 ? cachedPPS.particles.Count : 200;
        cachedPPS.SetSoul(count);

        yield return new WaitForSeconds(0.1f);

        foreach (var p in cachedPPS.particles)
            p.velocity += transform.forward * spawnOutSpeed;
    }
}