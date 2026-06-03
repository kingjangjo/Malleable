using UnityEngine;
// Chamber_01.cs — 챔버 1 전용 스크립트
// ChamberBase를 상속, 챔버 루트 오브젝트에 부착
public class Chamber_01 : ChamberBase
{
    protected override void OnChamberInit()
    {
        Debug.Log("Chamber 1 시작");
        // 지금은 비워둠
        // 나중에 버튼/상자 초기화 로직이 들어올 자리
    }
}