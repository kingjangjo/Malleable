using UnityEngine;

// 챔버 내 스폰 위치를 나타내는 마커 컴포넌트
// 기존 태그("SpawnPoint") 방식 대신 컴포넌트 방식을 씀
// 이유: FindWithTag는 씬 전체를 검색해서 다른 챔버 잔재와 혼동될 수 있음
//       GetComponentInChildren<SpawnPoint>()는 현재 챔버 안에서만 탐색
public class SpawnPoint : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // 에디터에서 위치 시각화
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}