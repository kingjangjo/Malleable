using UnityEngine;

public class PassageCondition : PuzzleCondition
{
    public LiquidPassage passage;

    protected override bool Evaluate()
    {
        return passage != null && passage.IsComplete;
    }
}