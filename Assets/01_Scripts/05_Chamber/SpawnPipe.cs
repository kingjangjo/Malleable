using UnityEngine;
using System.Collections;

public class SpawnPipe : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("파이프에서 나오는 방향 힘 (transform.forward 기준)")]
    public float exitForce = 1.5f;

    void Start()
    {
        // SetActive(true) 시점에 Start() 실행
        // → RespawnPlayer()는 이미 완료된 상태 (LoadChamberRoutine에서 먼저 실행됨)
        // → 화면은 검은 상태 (ChamberPipe.PipeTransition이 FadeOut 완료 후 OnChamberCleared 호출)
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // ── 안정화 대기 ─────────────────────────────────────────
        // Start()는 SetActive 다음 프레임에 실행됨.
        // 챔버 내 모든 Start() 호출이 완료되길 기다림.
        yield return new WaitForEndOfFrame();
        yield return null;

        // ── SoulCore 위치 이동 ──────────────────────────────────
        // [핵심] SetSoul() 내부에서 core.transform.position을 spawnOrigin으로 사용함.
        // SoulCore를 먼저 이 SpawnPipe 위치로 옮겨야 파티클이 올바른 위치에 생성됨.
        // 화면이 검은 상태이므로 순간이동이 플레이어에게 보이지 않음.
        PlayerParticleSystem pps = FindObjectOfType<PlayerParticleSystem>();

        if (pps == null)
        {
            Debug.LogWarning("SpawnPipe: PlayerParticleSystem을 찾지 못했습니다.");
            yield break;
        }

        // pps.core (SoulCore)를 SpawnPipe 위치로 이동
        if (pps.core != null)
        {
            pps.core.transform.position = transform.position;
        }
        else
        {
            // core가 null이면 "SoulCore" 태그로 탐색 (fallback)
            var soulCoreGO = GameObject.FindWithTag("SoulCore");
            if (soulCoreGO != null)
                soulCoreGO.transform.position = transform.position;
        }

        // ── 파티클 스폰 ─────────────────────────────────────────
        // particles.Clear()는 SetSoul 내부에서 처리됨 (PlayerParticleSystem 수정 후).
        // SoulCore가 이미 SpawnPipe 위치로 이동했으므로
        // spawnOrigin = core.transform.position = 올바른 SpawnPipe 위치.
        pps.ClearParticles();
        pps.SetSoul(500);

        // 파이프에서 흘러나오는 느낌: transform.forward 방향 초기 속도
        if (exitForce > 0f)
        {
            Vector3 dir = transform.forward * exitForce;
            for (int i = 0; i < pps.particles.Count; i++)
                pps.particles[i].velocity += dir;
        }

        // ── 물리 안정화 대기 ────────────────────────────────────
        // 파티클이 제자리에 자리 잡을 시간.
        // 화면이 아직 검은 상태이므로 정착 과정이 보이지 않음.
        yield return new WaitForSeconds(0.15f);

        // ── 페이드 인 — 화면이 밝아지며 새 챔버 등장 ────────────
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeIn(0.5f));
        else
            yield return new WaitForSeconds(0.5f);

        // ── 입력 잠금 해제 ──────────────────────────────────────
        if (GameManager.Instance != null)
            GameManager.Instance.UnlockInput();
    }
}