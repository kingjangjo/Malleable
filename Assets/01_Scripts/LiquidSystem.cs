using UnityEngine;
using System.Collections.Generic;

public class LiquidSystem : MonoBehaviour
{
    public List<Particle> particles = new List<Particle>();

    [Header("Particle")]
    public int particleCount = 500;

    [Header("Simulation")]
    public float radius = 1.0f;
    public float restDensity = 6f;
    public float stiffness = 60f;
    public float nearStiffness = 60f;
    public float linearViscosity = 0.5f;
    public float maxSpeed = 10f;

    [Header("Boundary")]
    public float boxSize = 3f;
    public float bounce = 0.3f;

    private Vector3 gravity = new Vector3(0, -9.8f, 0);

    void Start()
    {
        SpawnParticles();
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.016f);

        // 1) 중력
        foreach (var p in particles)
            p.velocity += gravity * dt;

        // 2) 점성 (velocity에, 위치 예측 전에)
        ApplyViscosity(dt);

        // 3) 위치 예측
        foreach (var p in particles)
        {
            p.prevPosition = p.position;
            p.position += p.velocity * dt;
        }

        // 4) DDR
        DoubleDensityRelaxation(dt);

        // 5) 충돌
        SolveCollisions();

        // 6) 속도 갱신
        foreach (var p in particles)
        {
            p.velocity = (p.position - p.prevPosition) / dt;
            if (p.velocity.sqrMagnitude > maxSpeed * maxSpeed)
                p.velocity = p.velocity.normalized * maxSpeed;
        }
    }

    void SpawnParticles()
    {
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * 1.5f;
            pos.y = Mathf.Abs(pos.y) + 2f;

            var p = new Particle(pos);
            p.prevPosition = pos;
            particles.Add(p);
        }
    }

    // ── 원본 논문의 점성: 접근하는 입자만 감쇠 ──
    void ApplyViscosity(float dt)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = i + 1; j < particles.Count; j++)
            {
                Vector3 rij = particles[j].position - particles[i].position;
                float dist = rij.magnitude;

                if (dist >= radius || dist < 0.0001f)
                    continue;

                float q = 1f - dist / radius;
                Vector3 n = rij / dist;

                // 서로 접근하는 속도 성분만 추출
                float u = Vector3.Dot(
                    particles[i].velocity - particles[j].velocity, n);

                if (u > 0) // 접근할 때만 감쇠
                {
                    Vector3 impulse = dt * q * linearViscosity * u * n;
                    particles[i].velocity -= impulse * 0.5f;
                    particles[j].velocity += impulse * 0.5f;
                }
            }
        }
    }

    // ── 원본 논문의 DDR: dt² 사용 ──
    void DoubleDensityRelaxation(float dt)
    {
        float dt2 = dt * dt;

        // 밀도 계산
        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].density = 0;
            particles[i].nearDensity = 0;

            for (int j = 0; j < particles.Count; j++)
            {
                if (i == j) continue;
                float dist = (particles[j].position - particles[i].position).magnitude;

                if (dist < radius)
                {
                    float q = 1f - dist / radius;
                    particles[i].density += q * q;
                    particles[i].nearDensity += q * q * q;
                }
            }
        }

        // 압력 변위
        for (int i = 0; i < particles.Count; i++)
        {
            Particle pi = particles[i];
            float Pi = stiffness * (pi.density - restDensity);
            float Pi_near = nearStiffness * pi.nearDensity;

            for (int j = i + 1; j < particles.Count; j++)
            {
                Particle pj = particles[j];
                Vector3 rij = pj.position - pi.position;
                float dist = rij.magnitude;

                if (dist < radius && dist > 0.0001f)
                {
                    float q = 1f - dist / radius;
                    Vector3 n = rij / dist;

                    float Pj = stiffness * (pj.density - restDensity);
                    float Pj_near = nearStiffness * pj.nearDensity;

                    // ★ dt² 사용 — 원본 논문 그대로
                    float D = dt2 * (
                        (Pi + Pj) * 0.5f * q +
                        (Pi_near + Pj_near) * 0.5f * q * q
                    );

                    Vector3 disp = D * 0.5f * n;
                    pi.position -= disp;
                    pj.position += disp;
                }
            }
        }
    }

    void SolveCollisions()
    {
        foreach (var p in particles)
        {
            if (p.position.y < 0f)
            { p.position.y = 0f; p.velocity.y *= -bounce; }

            if (p.position.x < -boxSize)
            { p.position.x = -boxSize; p.velocity.x *= -bounce; }
            else if (p.position.x > boxSize)
            { p.position.x = boxSize; p.velocity.x *= -bounce; }

            if (p.position.z < -boxSize)
            { p.position.z = -boxSize; p.velocity.z *= -bounce; }
            else if (p.position.z > boxSize)
            { p.position.z = boxSize; p.velocity.z *= -bounce; }
        }
    }

    void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = Color.cyan;
        foreach (var p in particles)
            Gizmos.DrawSphere(p.position, 0.05f);

        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireCube(
            new Vector3(0, boxSize * 0.5f, 0),
            new Vector3(boxSize * 2, boxSize, boxSize * 2));
    }
}