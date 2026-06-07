using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class ChamberBase : MonoBehaviour
{
    [Header("Chamber Setup")]
    public int chamberNumber;
    public ChamberDoor exitDoor;

    [Header("Clear Conditions")]
    [Tooltip("전부 충족되면 문이 열림. 비어있으면 TriggerClear()를 직접 호출.")]
    public List<PuzzleCondition> clearConditions = new List<PuzzleCondition>();

    private bool isCleared = false;
    public event Action OnCleared;

    // ── 초기화 ────────────────────────────────────────────────────

    // [수정] private → protected virtual
    // 문제: Start()가 private이면 자식 클래스(Chamber_01 등)가 Start()를 정의할 때
    //       부모의 Start()를 호출할 방법이 없음 → OnChamberInit()이 실행 안 됨
    // 해결: protected virtual로 변경하면 자식이 override Start()에서 base.Start() 호출 가능
    protected virtual void Start()
    {
        OnChamberInit();
    }

    // 자식 챔버에서 반드시 구현 (챔버 고유 초기화 로직)
    protected abstract void OnChamberInit();

    // ── 클리어 조건 체크 ─────────────────────────────────────────

    void Update()
    {
        if (!isCleared) CheckClearConditions();
    }

    // ChamberBase.cs 수정
    void CheckClearConditions()
    {
        if (clearConditions.Count == 0) return;

        foreach (var condition in clearConditions)
        {
            // null = 연결 안 됨 = 미충족으로 처리 (continue 대신 return)
            if (condition == null || !condition.IsSatisfied) return;
        }
        TriggerClear();
    }

    // ── 클리어 처리 ───────────────────────────────────────────────

    // public: 자식 클래스나 외부에서 직접 호출 가능
    public void TriggerClear()
    {
        if (isCleared) return;
        isCleared = true;

        Debug.Log($"Chamber {chamberNumber} 클리어!");

        if (exitDoor != null) exitDoor.Unlock();

        // 다음 챔버 프리로드 시작 (문 열리는 동안 백그라운드에서 생성)
        if (ChamberManager.Instance != null)
            ChamberManager.Instance.PreloadNextChamber(chamberNumber + 1);  

        OnCleared?.Invoke();
    }

    // 테스트용 강제 클리어 (Inspector 우클릭)
    [ContextMenu("Force Clear (Debug)")]
    public void ForceClear()
    {
        isCleared = false;
        TriggerClear();
    }
}