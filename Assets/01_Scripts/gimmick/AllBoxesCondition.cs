using UnityEngine;

/// <summary>
/// 여러 BoxPositionTrigger를 한꺼번에 감시합니다.
/// 챔버 8 (이중 잠금) 등 박스 2개 이상이 모두 제자리에 있어야 할 때 사용.
/// </summary>
public class AllBoxesCondition : PuzzleCondition
{
    public BoxPositionTrigger[] triggers;

    protected override bool Evaluate()
    {
        if (triggers == null || triggers.Length == 0) return false;
        foreach (var t in triggers)
            if (t == null || !t.IsActivated) return false;
        return true;
    }
}