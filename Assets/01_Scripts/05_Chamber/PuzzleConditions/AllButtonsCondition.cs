using UnityEngine;

public class AllButtonsCondition : PuzzleCondition
{
    public ChamberButton[] buttons;

    protected override bool Evaluate()
    {
        if (buttons == null || buttons.Length == 0) return false;
        foreach (var b in buttons)
            if (b == null || !b.IsPressed) return false;
        return true;
    }
}