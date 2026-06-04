using UnityEngine;

// ── 챔버 6: 통로 + 버튼 조합 ────────────────────────────────
// 좁은 틈 통과 후 버튼 누르기
// clearConditions에 PassageCondition + ButtonPressedCondition 연결
public class Chamber_06 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 6: 기억 — 형태를 바꾸고 누르라");
    }
}