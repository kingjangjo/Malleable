using UnityEngine;

// ── 챔버 5: 수확 ─────────────────────────────────────────────
// 액체 수집 기믹
// clearConditions에 AllCollectedCondition 연결
public class Chamber_05 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 5: 수확 — 흩어진 것들을 모아라");
    }
}