using UnityEngine;

// ── 챔버 7: 희생과 수확 조합 ────────────────────────────────
// 드레인으로 소모 → 수집 아이템 등장 → 수집하면 클리어
// clearConditions에 DrainCondition + AllCollectedCondition 연결
// LiquidDrain.onDrainComplete에서 숨겨진 LiquidCollectible 활성화
public class Chamber_07 : ChamberBase
{
    [Header("Chamber 7 Specific")]
    [Tooltip("드레인 완료 시 활성화될 수집 아이템들")]
    public GameObject[] hiddenCollectibles;

    protected override void OnChamberInit()
    {
        Debug.Log("챔버 7: 희생과 수확");
        // 수집 아이템 비활성화
        foreach (var obj in hiddenCollectibles)
            if (obj != null) obj.SetActive(false);
    }

    // LiquidDrain.onDrainComplete UnityEvent에서 이 메서드를 연결
    public void RevealCollectibles()
    {
        foreach (var obj in hiddenCollectibles)
            if (obj != null) obj.SetActive(true);
    }
}