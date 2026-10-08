# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## ⚠️ 최우선 규칙: 작업 파이프라인 (반드시 준수)

설계·구현 작업은 **항상** 아래 순서를 따른다. 단계를 건너뛰거나 순서를 바꾸지 않는다.

문서 위치: `docs/<작업명-영문>/` (예: `docs/BossPattern/Research.md`, `docs/BossPattern/Plan.md`). 작업명 폴더는 영문으로 짓는다.

1. **Research.md 작성** — 설계 전에 먼저, 해당 작업과 관련된 파일들을 실제로 읽고 분석해 정리한다.
   - 관련 스크립트/에셋/씬/프리팹 경로, 현재 동작 흐름, 의존 관계(누가 누구를 호출하는지), 영향 범위, 위험 요소.
   - 추측이 아니라 코드에서 확인한 사실만 적는다.
2. **Plan.md 작성** — Research.md를 근거로 코드 설계를 작성한다.
   - 구현을 **단계(Phase/Step)로 나누고**, 각 단계마다 체크박스(`- [ ]`)로 변경할 파일과 작업 내용을 명시한다.
   - 작성 후 사용자에게 피드백을 요청하고 **멈춘다. 이 단계에서는 코드를 구현하지 않는다.**
3. **피드백 반영 루프** — 사용자가 Plan.md(또는 Research.md)에 `>>>`로 시작하는 주석을 남기거나 프롬프트로 피드백하면:
   - 파일에서 모든 `>>>` 주석을 찾아 반영해 Plan.md를 다시 작성한다 (반영한 `>>>` 주석은 제거).
   - 필요하면 Research.md도 보강한다.
   - 반영 내용과 남은 의문점을 사용자에게 다시 피드백하고 멈춘다. 사용자가 구현을 지시할 때까지 반복.
4. **구현** — 사용자가 "Plan대로 구현해줘" 등으로 명시적으로 지시했을 때만 시작한다.
   - Plan.md의 **모든 단계를 끝까지 구현한다. 중간에 멈추거나 사용자에게 넘기지 않는다.**
   - 각 항목을 완료할 때마다 Plan.md에 즉시 표시한다: 완료 `- [x]`, 미완료 `- [ ]`, 계획과 달라진 점이나 구현 불가 사유는 해당 항목 아래에 메모.
   - 마지막에 Plan.md 상단에 구현 현황 요약(완료/미완료 항목)을 남긴다.
5. **MCP로 검증 (구현 중 지속적으로)** — 스크립트 수정 후마다 Unity MCP로 확인해 새 문제를 만들지 않는다.
   - 컴파일 완료 대기 → 콘솔 에러/경고 확인 → 수정한 씬/프리팹/참조가 깨지지 않았는지 확인.
   - 새로 생긴 에러는 다음 단계로 넘어가기 전에 해결한다. 해결 못 한 문제는 Plan.md에 기록한다.

## 프로젝트 개요

Timeless — Unity 6 (`6000.6.2f1`), URP 기반 3레인 러너 게임. 플레이어는 레인을 바꾸고(A/D) 점프(Space)하며 다가오는 장애물/아이템을 피하거나 먹는다. HP가 일정 비율 아래로 떨어지면 3D 뷰 → 2D 뷰로 전환되는 것이 핵심 메커닉.

- 씬: `Assets/01. Scenes/Title.unity` → `SampleScene.unity` (빌드 순서). `BackGroundTestScene`, `JHWTestScene`은 개인 테스트용.
- 주요 패키지: URP 17 (Render Graph), Cinemachine 3 (`Unity.Cinemachine` 네임스페이스), Input System, ProBuilder.
- 모든 게임 코드는 `Assets/02. Scripts/` 아래 단일 `Assembly-CSharp`에 있음 (asmdef 없음, 네임스페이스 없음).
- 테스트 코드 없음. 빌드/실행은 Unity 에디터에서.

## 명령

```bash
# 배치모드 컴파일/빌드 확인 (에디터가 이 프로젝트를 열고 있으면 실패함)
"<Unity 6000.6.2f1 경로>/Editor/Unity.exe" -batchmode -quit -projectPath . -logFile -

# 열려 있는 에디터 제어 (com.unity.pipeline 패키지 설치됨). 다른 Unity 프로젝트도 열려 있을 수 있으니 --project-path 필수
unity command --project-path .                                  # 사용 가능한 명령 목록
unity command console_status --project-path .                   # 컴파일 실패 여부 + 에러/경고 수
unity command console --project-path . --level warning --tail 20
unity command eval_file --project-path . --file <윈도우 절대경로.cs>  # C# 실행
```

- `eval_file` 코드는 메서드 본문으로 감싸져 실행되므로 `using` 사용 불가 — 타입은 전부 정규화된 이름(`UnityEngine.Rendering.Volume`)으로 쓰고 `return`으로 결과 반환.
- `capture_game_view --save_path`는 프로젝트 안 경로만 허용되며 `Assets/` 아래에 저장됨 — 확인 후 반드시 삭제(`.meta` 포함).
- 플레이 시 SampleScene에서 배경 프리팹 Missing Script 경고, `Ray does not intersect with y=0 plane` 경고가 원래부터 나온다.

## 아키텍처

### 싱글톤과 실행 순서
- `Singleton<T>` (`Singleton.cs`): `Instance` 접근 시 `FindAnyObjectByType`로 찾고, 없으면 새 GameObject를 **자동 생성**함. 기본 `dontDestroyOnLoad = true`(루트를 DDOL 처리).
- `GameManager`는 `[DefaultExecutionOrder(-100)]`. 전역 상태(`mapSpeed`, `currentViewMode`, `GeneratePos`/`DisposePos`, 2D/3D 전환 임계값)를 들고 있음. `Player`, `LaneManager`는 `Start`에서 `FindAnyObjectByType`로 연결.
- `CameraManager`도 싱글톤. 거의 모든 시스템이 `GameManager.Instance` / `CameraManager.Instance`를 직접 호출.

### 월드 이동 모델 (플레이어는 Z축 고정)
플레이어는 X(레인)만 움직이고, 월드가 `-Z` 방향으로 `GameManager.mapSpeed`만큼 흘러옴.
- `Track` / `BackgroundManager`: `Update`에서 `Translate`, `DisposePos.z`를 지나면 `SetActive(false)`.
- `MapManager`: 시작 시 `GeneratePos`~`DisposePos` 구간을 트랙/배경으로 채우고, 코루틴으로 마지막 타일이 자기 길이만큼 이동하면 다음 타일 생성. 자식 중 비활성 오브젝트를 재사용하는 자체 풀링.
- `InteractionObject` 계열: `FixedUpdate`에서 `Rb.MovePosition`으로 이동, `DisposePos` 지나면 비활성화.
- 무적 상태에서는 `Player`가 `mapSpeed` 자체를 바꿈(`invincibleSpeed` ↔ 원래 속도).

### 레인 & 스폰
- `LaneManager`가 자식 `Lane`들을 `laneInterval` 간격으로 카메라 상단 뷰포트(y=0 평면 교차점, `Utils.GetTopViewportPosition`) 위치에 배치.
- 스폰 패턴은 `CreateObjectSO` 에셋(`Assets/11. Datas/CreateObjectData3D.asset`, `CreateObjectData2D.asset`): 패턴 목록 → 각 패턴은 행(row) 목록 → 각 행은 `(laneIndex, ObjectType)` 목록. 현재 뷰 모드에 따라 2D/3D 데이터셋 선택, 랜덤 패턴을 `patternIntervalTime` 간격으로 행 단위 생성.
- `ObjectType` → 프리팹 매핑은 `ObjectDataSO`(`ObjectData.asset`). 새 장애물 추가 시: `ObjectType` enum(`InteractionObject.cs`)에 값 추가 → 프리팹에 `Obstacle`/`HealObject` 등 부착 → `ObjectData.asset`에 등록 → 패턴 에셋에 배치.

### 풀링
- `Pool<T>`는 `Singleton<Pool<T>>`을 상속 → `Pool<InteractionObject>.Instance`로 접근하고 `InteractionObjectPool`로 캐스팅해서 타입별 `Get(ObjectType, pos, rot)` 사용.
- 풀은 별도 리스트 없이 **자기 자식 중 비활성 오브젝트를 검색**하는 방식. 반환 = `SetActive(false)`.

### 충돌 / 상호작용
- `InteractionObject`(추상, Collider+Rigidbody 필수, `ICollisionable` 구현)가 트리거 진입 시 `onTargetHitEvent(상대)` 발생. `Player`도 `InteractionObject`를 상속(단, `OnEnable/OnDisable` 오버라이드로 dispose 코루틴 비활성).
- `Obstacle`: 점프 중(`ignoreJumpedPlayer`가 false일 때)이나 무적이면 무시, 아니면 데미지 + 글리치/카메라 셰이크/SFX.
- `HealObject`: 점프 중이 아니면 회복 후 비활성화.

### 플레이어 스탯 & 2D/3D 전환
- `Stat<T>` (`Player.cs` 안에 정의): HP, 무적(최대 HP 도달 시 발동 → 지속시간 → 쿨다운), 각종 `Action` 이벤트.
- HP는 시간에 따라 계속 감소(`hpDecrement`). HP 변경 시 `Player.CheckChangeView()`가 `threshold2DView`/`threshold3DView`로 `GameManager.ChangeViewMode` 호출 + `AudioPlayer.trigger`로 BGM 크로스페이드 + 레인 오브젝트 전부 회수.
- `ChangeViewMode` → `CameraManager.SwitchCamera`(Cinemachine vCam 우선순위 교체, 2D에서는 `"Right"` 레이어 컬링) + 픽셀화 강도 변경. 2D 모드에서는 플레이어가 마지막 레인에 고정되고 좌우 이동 불가.
- 사망 시 `SampleScene` 재로드.

### 이벤트 채널
`EventChannelSO`(파라미터 없는 SO 이벤트) + `EventListener`(UnityEvent 응답). `Player`가 `onPlayerStatChanged`, `onPlayerDied` 채널로 발행하고, `HPBar`는 `EventListener`를 상속해 인스펙터에서 응답 연결.

### 카메라 / 렌더링
- `CameraManager`: 카메라 동작(전환)을 코루틴 큐로 순차 실행. 셰이크는 vCam의 `CinemachineBasicMultiChannelPerlin` 사용.
- 후처리는 URP Renderer Feature를 **에셋 직접 수정**으로 제어: `FullScreenPassRendererFeature`의 머티리얼 `_Alpha`(글리치), `PixelateRendererFeature.settings.pixelScale`. 이 값들은 에셋에 저장되므로 `OnApplicationQuit`에서 기본값으로 되돌림. 플레이 후 렌더러 에셋(`Assets/Settings/PC_Renderer.asset`)이나 글리치 머티리얼에 의도치 않은 git diff가 생길 수 있음.
- `PixelateRenderPass`는 Render Graph API (`RecordRenderGraph`) 사용. `PixelateCamera.cs`는 `OnRenderImage` 기반 구버전(URP에서 동작 안 함).

### 입력
`Assets/02. Scripts/Input/PlayerInput.cs`는 `PlayerInput.inputactions`에서 **자동 생성**된 파일 — 직접 수정 금지, `.inputactions` 수정 후 재생성. `InputHandler`가 이를 감싸서 `MoveInput`/`Jump`(한 프레임 플래그, `LateUpdate`에서 리셋) 노출.

## 주의사항

- **인코딩**: 기존 스크립트의 한글 주석 상당수가 CP949(EUC-KR)로 저장되어 있음(예: `MapManager.cs`, `AudioPlayer.cs`). UTF-8로 읽으면 깨져 보임. 해당 파일을 편집할 때 인코딩을 바꾸면 diff 전체가 변경되니 주의하고, 새 파일은 UTF-8로 작성.
- Unity 에셋/씬/프리팹은 YAML 직렬화 — 이동·삭제 시 `.meta` 파일도 같이 처리해야 GUID 참조가 유지됨.
- 폴더명에 공백과 번호 접두사가 있음(`02. Scripts`) — 셸 명령에서 경로 인용 필수.
- 브랜치: 작업은 `dev`, PR 대상은 `master`.
- 커밋/푸시 규칙: 커밋 메시지와 PR 본문에 Claude를 컨트리뷰터로 넣지 않는다. `Co-Authored-By: Claude ...` 트레일러, `Generated with Claude Code` 문구 등 Claude 표기를 모두 제외하고, 작성자는 사용자 git 계정만 남긴다.
