# Soliquid — Unity 프로젝트

## 기술 스택
- Unity 2022.x, URP 14.x, C#
- 빌드 타겟: Windows PC

## 핵심 구조
Assets/Scripts/
  Player/
    PlayerFormController.cs  - 액체↔고체 변신, 충돌 체크
    PlayerParticleSystem.cs  - 파티클 200개 물리 시뮬레이션 (커스텀 SPH)
    SoulCore.cs              - 플레이어 메인 데이터
  Chamber/
    ChamberBase.cs           - abstract 챔버 기반 클래스
    ChamberManager.cs        - 챔버 프리로드/로드 (PreloadNextChamber, LoadChamber)
    ChamberPipe.cs           - F키 파이프 진입, FadeOut 후 챔버 전환
    SpawnPipe.cs             - 새 챔버 시작 시 SetSoul + FadeIn
  UI/
    ScreenFader.cs           - 화면 페이드 인/아웃 싱글턴
  Gimmick/
    PushableObject.cs        - Rigidbody 박스, 고체만 밀 수 있음
    BoxPositionTrigger.cs    - 박스 목표 위치 도달 이벤트

## 플레이어 시스템
- SoulCore: 메인 구체 오브젝트 (Rigidbody, 카메라 부착)
- Particle ×200: 커스텀 물리 시뮬레이션, 액체 형태 담당
- 상태: Soul(액체, 좁은 틈 통과) / Humanoid(고체, 박스 밀기 가능)

## 코딩 컨벤션
- public 필드는 [Header] 속성으로 그룹핑
- 싱글턴은 Instance 패턴
- 코루틴은 IEnumerator + StartCoroutine
- 한국어 주석 사용

## 주의사항
- PlayerParticleSystem.SetHumanoid()는 파티클 상태를 실제로 바꾸므로
  사전 체크 없이 호출하면 되돌리기 어려움
- Physics.OverlapBox 체크 시 자기 자신 레이어 제외 필수
- URP Shader Graph: Additive + Depth Write Disabled 설정 유지