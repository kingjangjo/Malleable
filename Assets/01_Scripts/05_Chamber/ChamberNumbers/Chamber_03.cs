using UnityEngine;

// ── 챔버 3: 무게 ─────────────────────────────────────────────
// 바닥 버튼 튜토리얼
// clearConditions에 ButtonPressedCondition 연결
public class Chamber_03 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 3: 무게 — 버튼을 눌러라");
    }
}