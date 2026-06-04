using UnityEngine;

// ── 챔버 9: 순환 ─────────────────────────────────────────────
// 좁은 틈 + 드레인 + 버튼 복합
// clearConditions에 PassageCondition + DrainCondition + ButtonPressedCondition
public class Chamber_09 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 9: 순환 — 모든 것을 기억하라");
    }
}