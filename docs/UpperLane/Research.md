# UpperLane — Research

목표: 하단 레인 3개 위에 상단 레인 3개를 추가하고, 점프 체공 중에만 상단 레인 오브젝트와 충돌 판정. 체공시간 + 체공 Y 그래프(AnimationCurve)를 Player 인스펙터에서 조정.

## 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Player/Player.cs` | 점프(`Jump`/`JumpCoroutine`), `IsJump`, 레인 이동(`ChangeLaneCoroutine`) |
| `Assets/02. Scripts/Player/PlayerAnimator.cs` | `Jump` 트리거만 세팅 |
| `Assets/02. Scripts/Map/Lane.cs` | 레인 위치(`LaneX`), 오브젝트 스폰(`GenerateObstacle`) |
| `Assets/02. Scripts/Map/LaneManager.cs` | 자식 `Lane` 수집/배치, 패턴 스폰 |
| `Assets/02. Scripts/Datas/CreateObjectSO.cs` | `ObjectSpawnData(laneIndex, type)` |
| `Assets/02. Scripts/Obstacles/InteractionObject.cs` | 트리거 진입 → `onTargetHitEvent` |
| `Assets/02. Scripts/Obstacles/Obstacle.cs`, `HealObject.cs` | 점프 여부로 충돌 무시 판정 |
| `Assets/02. Scripts/Utils.cs` | `IsAnimationTerminated`(점프 종료 판정에 사용) |
| `Assets/04. Animations/Jump.anim`, `PlayerAnimator.controller` | 점프 애니메이션 |
| `Assets/01. Scenes/SampleScene.unity` | `LaneManager` 하위 `Lane_0~2`, `Player` |
| `Assets/11. Datas/CreateObjectData3D.asset`, `CreateObjectData2D.asset` | 스폰 패턴 |

## 현재 동작

### 점프
- `Player.Move()` → `handler.Jump`이면 `Jump()` → `JumpCoroutine` 시작 + `playerAnimator.Jump()`(트리거).
- `JumpCoroutine`: `IsJump = true` → `Utils.IsAnimationTerminated(animator, 0, "Jump")`(normalizedTime ≥ 0.95, 전이 중 아님)까지 대기 → `IsJump = false`.
- **체공시간은 애니메이션에 종속**: `Jump.anim` 길이 1s(30fps, 루프 아님), 컨트롤러의 Jump 스테이트 `m_Speed: 2` → 실제 약 0.5s.
- **Transform/Rigidbody Y는 움직이지 않음**. 시각적 높이는 휴머노이드 클립의 `RootT.y` 커브(약 0.85 → 최대 1.49 @0.37s → 복귀, 약 +0.64)로만 표현됨. Animator `m_ApplyRootMotion: 0`.
- Player 콜라이더: `CapsuleCollider` 트리거, radius 0.25, height 1.8, center y 0.9. Rigidbody: 중력 없음, 비키네마틱, `m_Constraints: 112`(회전 XYZ 고정). 위치는 `Rb.MovePosition`/`Rb.position`으로만 조작.
- `ChangeLaneCoroutine`은 시작 시점의 `Rb.position.y`를 목표 Y로 사용 → 점프 중 Y를 움직이면 레인 변경과 충돌함(점프 중 레인 변경 시 Y가 고정되어 버림).

### 레인
- `LaneManager.InitLanes()`: `GetComponentsInChildren<Lane>()` 순서대로 `lanes[]`, x = `(i - center) * laneInterval`, y/z = `Utils.GetTopViewportPosition(0.05f)`(y=0 평면 교차점, 즉 y=0).
- 씬: `LaneManager` 자식 `Lane_0`, `Lane_1`, `Lane_2`. `laneInterval: 1`, `createInterval: 1`.
- `laneLength`(= `lanes.Length`)는 레인 수 의미로 여러 곳에서 사용: `Player.Start`(중앙 레인), `Player.Update`/`GameManager.ChangeViewMode`(2D에서 마지막 레인 고정), `Player.ChangeLane`(범위 체크), `LaneManager.GenerateRow`(범위 체크).
- 시각적 레일 메시는 없음(트랙 프리팹에 레일 오브젝트 없음). "레인"은 `Lane` 오브젝트(빨간 Gizmo 레이)뿐.
- `Lane.LaneIndex`는 어디서도 할당되지 않아 항상 0 (기존 버그, `currentLane`에 0이 들어감 — 현재 읽는 곳 없음).

### 충돌
- `InteractionObject.OnTriggerEnter` → 리스트에 없으면 `onTargetHitEvent` (진입 시 1회).
- `Obstacle.OnCollded`: `(!ignoreJumpedPlayer && player.IsJump) || 무적`이면 무시. `ignoreJumpedPlayer`는 이름과 달리 **true면 점프해도 맞음**(Laser, Robot_Arm = 1 / Fan, Blockade, Goomba = 0).
- `HealObject.OnCollded`: `player.IsJump`이면 무시.
- 즉 지금도 "점프 = 하단 오브젝트 회피"가 플래그로만 처리되고 물리 높이와 무관.

### 스폰 데이터
- `ObjectSpawnData { int laneIndex; ObjectType type; }`. `GenerateRow`는 `0 <= laneIndex < laneLength`만 스폰.

## 위험 요소
1. 점프로 Transform Y를 올리면 `Jump.anim`의 `RootT.y`와 **높이가 이중 적용**됨.
2. `OnTriggerEnter`는 진입 1회 — 지상에서 상단 오브젝트와 겹친 상태로 점프하면 판정 안 됨(반대도 동일). 상단 레인을 물리적으로 충분히 높게 두면 지상 플레이어(캡슐 상단 1.8)와 안 겹침.
3. `ChangeLaneCoroutine`이 Y를 덮어씀(위 참고).
4. `laneLength` 의미 변경 시 Player/GameManager 영향 → 상단 레인은 `lanes[]`와 분리해야 안전.
5. 2D 모드: 플레이어는 마지막 레인 고정. 상단 레인도 같은 x에 있으므로 2D(측면)에서도 위에 보임 — 별도 처리 불필요할 것으로 보임(플레이 확인 필요).
6. 기존 패턴 에셋은 `laneIndex` 0~2만 사용 → 스폰 데이터에 새 필드를 추가해도 기본값(false = 하단)이면 호환.
