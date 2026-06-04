// PuzzleCondition.cs — 일단 빈 껍데기로 생성
// 나중에 ButtonPressedCondition 등 자식 클래스 만들 때 채워넣을 것
using UnityEngine;

public abstract class PuzzleCondition : MonoBehaviour
{
    // 조건 충족 여부 — ChamberBase가 이걸 폴링함
    public bool IsSatisfied { get; protected set; } = false;

    // 힌트 텍스트 (나중에 TutorialPrompt에서 사용)
    public string hintText;

    // 자식 클래스에서 구현 — 매 프레임 조건 판단
    protected abstract bool Evaluate();

    void Update()
    {
        IsSatisfied = Evaluate();
    }
}