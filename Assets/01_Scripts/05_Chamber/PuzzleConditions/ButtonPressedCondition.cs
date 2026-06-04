using UnityEngine;

public class ButtonPressedCondition : PuzzleCondition
{
    public ChamberButton button;

    protected override bool Evaluate()
    {
        return button != null && button.IsPressed;
    }
}
