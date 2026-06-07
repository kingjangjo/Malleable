using UnityEngine;

// ── 챔버 2: 변신 ─────────────────────────────────────────────
// 좁은 틈 통과 튜토리얼
// clearConditions에 PassageCondition 연결
public class Chamber_02 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 2: 틈새 — 좁은 통로를 통과하라");
    }
}