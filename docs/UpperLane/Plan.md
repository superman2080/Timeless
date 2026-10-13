# UpperLane — Plan

## 구현 현황 (2026-10-14)
- **완료**: Phase 1~6 전 항목. 미완료 없음.
- 컴파일 에러 0, 플레이 중 신규 에러/경고 0 (기존 Missing Script 경고만).
- 플레이 검증: 점프 Y가 커브대로 이동(0.94 → 1.40 확인), 점프 중 레인 변경/2D 전환 시 마지막 레인 이동 확인, 충돌 8케이스(상/하단 × HP/BLOCKADE × 지상/점프) 모두 의도대로.
- 계획 외 변경: Animator 전이 타이밍 조정(아래 Phase 2 메모).

근거: `docs/UpperLane/Research.md`

## 설계 요약
- **점프**: 애니메이션 종료 대기 대신 `airTime`(초) 동안 `jumpCurve`(x: 0~1 정규화 시간, y: 높이)로 Rigidbody Y를 직접 구동. `IsJump`는 이 구간 동안 true.
- **애니메이션 동기화**: Jump 스테이트 속도를 파라미터로 바꿔 `클립길이 / airTime`으로 맞춤. 클립의 `RootT.y` 높이는 평탄화해 높이는 `jumpCurve`만 담당(결정: A).
- **상단 레인**: `LaneManager`에 `upperLanes[]`를 `lanes[]`와 별도로 둠 → `laneLength` 의미 유지. 상단 레인 y = `upperLaneHeight`.
- **스폰 데이터**: `ObjectSpawnData`에 `bool upper` 추가(기본 false → 기존 에셋 호환). 같은 `laneIndex`로 하단/상단 선택.
- **충돌 판정**: `InteractionObject`에 `isUpper` 플래그. 상단 오브젝트는 `player.IsJump`일 때만, 하단은 기존 규칙대로. 높이도 물리적으로 분리해 지상 플레이어와 상단 오브젝트가 겹치지 않게 함.

## Phase 1 — Player 점프를 체공시간 + 커브로
- [x] `Player.cs`: 필드 추가
  ```csharp
  [Header("Jump")]
  [SerializeField] float airTime = 0.5f;                       // 체공 시간(초)
  [SerializeField] AnimationCurve jumpCurve =                  // x: 0~1 정규화 시간, y: 높이(월드 유닛)
      new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1.5f), new Keyframe(1, 0));
  [SerializeField] bool allowLaneChangeWhileJumping = true; // 점프 중 레인 변경 허용 여부
  ```
- [x] `Player.cs`: `groundY`를 `Start`에서 `Rb.position.y`로 저장.
- [x] `Player.cs`: `JumpCoroutine`을 `FixedUpdate` 기반으로 교체 — `t = elapsed / airTime`, `Rb.MovePosition(x, groundY + jumpCurve.Evaluate(t), z)`, 끝나면 `y = groundY`, `IsJump = false`.
- [x] `Player.cs`: `ChangeLaneCoroutine`이 Y를 건드리지 않게 — X만 보간하고 Y는 매 스텝 현재 `Rb.position.y` 사용.
  - 두 코루틴이 각각 `MovePosition`을 호출하면 마지막 호출만 적용되므로, 점프는 `jumpOffsetY` 값만 갱신하고 실제 `MovePosition`은 한 곳(`FixedUpdate`)에서 x(레인 보간) + y(groundY + 점프)로 합성하는 구조로 정리.
  - 메모: `laneX` 필드를 두고 `ChangeLaneCoroutine`은 `laneX`만 보간, `Player.FixedUpdate`에서 `(laneX, groundY + jumpOffsetY, z)`로 `MovePosition` 1회.
- [x] `Player.ChangeLane`: 가드에 `(!allowLaneChangeWhileJumping && IsJump)`이면 return 추가. 2D 강제 레인 이동(`Update`/`GameManager.ChangeViewMode`)도 같은 `ChangeLane`을 거치므로, 플래그가 false일 때는 착지 후 `Update`에서 다시 시도되어 자연스럽게 처리됨.
- [x] `Utils.IsAnimationTerminated`는 다른 사용처 없음 → 점프에서 안 쓰게 되면 그대로 둘지 삭제할지는 구현 시 사용처 재확인 후 삭제.
  - 메모: 다른 사용처 없음 확인 후 삭제.

## Phase 2 — 점프 애니메이션 동기화
- [x] `PlayerAnimator.controller`: float 파라미터 `JumpSpeed` 추가, Jump 스테이트 `Speed Multiplier`를 `JumpSpeed`로 연결(기본 `m_Speed` 2 → 1로).
- [x] `PlayerAnimator.cs`: `Jump(float airTime)` → `animator.SetFloat(JumpSpeed, clipLength / airTime)` 후 트리거. `clipLength`는 `[SerializeField] AnimationClip jumpClip`의 `length`.
- [x] `Jump.anim`: `RootT.y` 커브를 평탄화(첫 키 값 ≈0.85로 모든 키 고정)해 높이는 스크립트 커브만 담당. MCP `eval`로 `AnimationUtility.GetEditorCurve/SetEditorCurve` 사용(YAML 직접 편집 X). 플레이에서 발이 지면에 붙어 있는지 확인.
  - 메모: 첫 키 값 0.8515로 상수 커브 적용.
- [x] (계획 외) `PlayerAnimator.controller` 전이 타이밍 조정 — 기존 `Jump→Run`이 exitTime ≈0 / 고정 0.32s라 점프 포즈가 체공 시간과 무관하게 바로 빠졌음. `Jump→Run`: normalized, exitTime 0.8, duration 0.2. `Any→Jump`: 고정 0.25s → normalized 0.1 (airTime 0.5s의 절반을 블렌드에 쓰던 문제).
- [x] `SampleScene` Player의 `PlayerAnimator.jumpClip`에 `Jump.anim` 연결.

## Phase 3 — 상단 레인
- [x] `Lane.cs`: `LaneIndex` setter를 `Init(int index, bool upper)`로 할당(기존 미할당 버그 수정), `public bool IsUpper`. Gizmo 색 상단은 다르게(파랑).
- [x] `Lane.GenerateObstacle`: 생성 오브젝트에 `obj.isUpper = IsUpper` 세팅.
- [x] `LaneManager.cs`: `public Lane[] upperLanes;` + `public float upperLaneHeight = 2.5f;`(지상 캡슐 상단 1.8보다 높게, 점프 최고점에서 닿는 높이).
  - `InitLanes`: 자식 Lane을 `IsUpper` 직렬화 필드 기준으로 `lanes` / `upperLanes`로 분리. 같은 x 배치, 상단은 y + `upperLaneHeight`.
  - `RemoveAllLaneObjects`: 상단 레인 포함.
  - `GenerateRow`: `spawnData.upper ? upperLanes : lanes`에서 인덱스 선택.
- [x] `SampleScene.unity`: `LaneManager` 하위에 `Lane_Upper_0~2` 추가(Lane 컴포넌트, `isUpper = true`). MCP로 생성 후 씬 저장.
  - 메모: `LaneManager.lanes`/`upperLanes` 직렬화 배열도 갱신. 플레이 시 상단 레인 y=2.5, LaneIndex 0~2 확인.
- [x] (후속) 에디터 Gizmo 위치 불일치 수정 — 레인 배치가 `Start`/`Reset`에서만 돌아 씬의 저장 위치가 런타임과 달랐음(하단 z=66, 상단 원점). `LaneManager.OnValidate` → `delayCall`로 `InitLanes` 실행(중복 등록 방지), `Lane` Gizmo에 WireSphere 추가. 씬 위치를 런타임 값(z=19.05, 상단 y=2.5)으로 갱신 후 저장.

## Phase 4 — 스폰 데이터
- [x] `CreateObjectSO.cs`: `ObjectSpawnData`에 `[Tooltip("상단 레인(점프 중에만 판정)")] public bool upper;` 추가, 생성자 인자 추가(기본 false).
- [x] 기존 패턴은 수정 불필요(기본 false).
- [x] `CreateObjectData3D.asset`: 상단 레인 테스트 패턴 1개 추가 — 예) 1행: 하단 0·1·2 장애물(점프 유도) + 상단 1에 HP / 2행: 상단 0·2에 장애물, 하단 1 비움.
- [x] `CreateObjectData2D.asset`: 상단 레인 테스트 패턴 1개 추가 — 2D는 마지막 레인(2) 고정이므로 하단 2 장애물 + 상단 2 HP, 다음 행 상단 2 장애물.
  - 장애물 타입은 기존 에셋에서 쓰이는 것 중 `ignoreJumpedPlayer = 0`인 것(Blockade 등) 사용. MCP로 SO 수정 후 저장.
  - 메모: 3D 패턴 20번, 2D 패턴 4번으로 추가됨. 기존 엔트리에 `upper: 0` 필드가 직렬화되어 에셋 diff가 큼(값 변화 없음).

## Phase 5 — 충돌 판정
- [x] `InteractionObject.cs`: `[HideInInspector] public bool isUpper;` 추가. 풀 재사용 시 `Lane.GenerateObstacle`에서 매번 덮어쓰므로 리셋 불필요.
  - 메모: 상단 배치 시 프리팹 콜라이더 최저점 ≈2.3(Laser) > 지상 캡슐 상단 1.8 확인 → 지상에선 물리적으로도 안 겹침.
- [x] `Obstacle.OnCollded`:
  ```csharp
  bool hit = isUpper ? player.IsJump : (ignoreJumpedPlayer || !player.IsJump);
  if (!hit || player.stat.isInvincible) return;
  ```
- [x] `HealObject.OnCollded`: `if (player.IsJump != isUpper) return;`

## Phase 6 — 검증 (MCP)
- [x] 각 Phase 후 컴파일 대기 → `console_status` 에러/경고 확인.
- [x] 씬 `Lane`/`LaneManager` 참조, Player 인스펙터 새 필드 확인.
- [x] 플레이: 점프 높이가 커브대로 움직이는지, 점프 중 레인 변경(플래그 true/false 둘 다), 상단 오브젝트가 지상에선 통과·점프 중 피격/회복, 하단 기존 동작 유지, 2D 전환 후 상단 레인 스폰·판정 동작.

## 결정 사항
- 높이 이중 적용: **A** — `Jump.anim` 루트 Y 평탄화.
- 점프 중 레인 변경: 허용(기본), `allowLaneChangeWhileJumping`로 인스펙터에서 변경 가능.
- 상단 레인 테스트 패턴: 3D/2D 에셋에 각각 추가.
- 2D 모드: 상단 레인 스폰함.

## 남은 의문점
1. `upperLaneHeight` 2.5, `jumpCurve` 최고점 1.5 기본값 — 답이 없어 이대로 진행 예정(인스펙터에서 조정 가능). 상단 오브젝트는 기존 프리팹을 높이만 올려 재사용.
