using UnityEngine;

public class DrainCondition : PuzzleCondition
{
    public LiquidDrain drain;

    protected override bool Evaluate()
    {
        return drain != null && drain.IsComplete;
    }
}
