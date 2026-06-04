using UnityEngine;

// ── 챔버 8: 균형 ─────────────────────────────────────────────
// 두 개의 버튼을 모두 눌러야 함
// clearConditions에 AllButtonsCondition 연결 (buttons = [버튼A, 버튼B])
public class Chamber_08 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("챔버 8: 균형 — 두 곳을 동시에");
    }
}