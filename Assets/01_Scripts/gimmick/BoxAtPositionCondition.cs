using UnityEngine;

/// <summary>
/// PuzzleCondition 구현체.
/// BoxPositionTrigger가 활성화되어 있으면 조건 충족.
/// ChamberBase.clearConditions 리스트에 연결합니다.
/// </summary>
public class BoxAtPositionCondition : PuzzleCondition
{
    public BoxPositionTrigger trigger;

    protected override bool Evaluate()
    {
        return trigger != null && trigger.IsActivated;
    }
}