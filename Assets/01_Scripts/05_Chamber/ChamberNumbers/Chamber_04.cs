using UnityEngine;

// ── 챔버 4: 희생 ─────────────────────────────────────────────
// 액체 소모 기믹
// clearConditions에 DrainCondition 연결
public class Chamber_04 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 4: 희생 — 일부를 흘려보내라");
    }
}