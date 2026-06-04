using UnityEngine;
// ── 챔버 1: 각성 ─────────────────────────────────────────────
// 기믹 없음. 그냥 걸어나가면 됨.
public class Chamber_01 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 1: 각성 — 이동해서 출구를 찾아라");
    }
}