using UnityEngine;

public class AllCollectedCondition : PuzzleCondition
{
    [Tooltip("수집해야 할 LiquidCollectible 목록. 비워두면 자동으로 자식에서 탐색")]
    public LiquidCollectible[] collectibles;

    protected void Awake()
    {
        // 비어있으면 부모 오브젝트에서 자동 탐색
        if (collectibles == null || collectibles.Length == 0)
            collectibles = GetComponentsInParent<LiquidCollectible>();
    }

    protected override bool Evaluate()
    {
        if (collectibles == null || collectibles.Length == 0) return false;

        foreach (var c in collectibles)
        {
            // gameObject.activeSelf가 false면 수집된 것
            if (c != null && c.gameObject.activeSelf) return false;
        }
        return true;
    }
}