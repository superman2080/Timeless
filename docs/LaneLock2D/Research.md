# Research: 2D 전환 시 레인 고정 실패

## 증상
3D → 2D 전환 순간 A/D 입력이 있으면, 플레이어가 고정 레인(`laneLength - 1`)으로 가지 않고 이동하던 레인에 그대로 고정됨.

## 관련 코드
- `Assets/02. Scripts/Player/Player.cs` (CP949 인코딩)
  - `Update()` (163~176): `Move()` → `StateMachine.Tick` → `HPDecrement()` → 2D면 `ChangeLane(laneLength - 1)` 보정
  - `Move()` (184~194): 3D이고 `MoveInput != 0`이면 `ChangeLane(currentLane ± 1)`
  - `ChangeLane()` (206~216): 아래 조건 중 하나라도 참이면 **조용히 무시**
    - `currentViewMode == View2D`
    - 범위 밖 인덱스
    - `changeLaneCor != null` (레인 이동 코루틴 진행 중, 0.1초)
    - `!allowLaneChangeWhileJumping && IsJump` (현재 기본값 true라 비활성)
  - `ChangeLaneCoroutine()` (218~232): 끝날 때 `currentLane = laneIndex`, `changeLaneCor = null`
  - `CheckChangeView()` (246~262): `stat.onHPChanged`에서 호출(129), HP 비율이 임계값 이하면 `GameManager.ChangeViewMode(View2D)`
- `Assets/02. Scripts/GameManager.cs` `ChangeViewMode()` (40~58): 2D면 `player.ChangeLane(laneLength - 1)` 호출 **후** `currentViewMode = viewMode`

## 원인
1. A/D 입력 → `Move()`가 레인 이동 코루틴 시작 → 0.1초간 `changeLaneCor != null`.
   키를 누르고 있으면 코루틴이 끝나자마자 다음 이동이 시작되므로 거의 항상 non-null.
2. 같은 프레임(또는 그 0.1초 안) `HPDecrement()` → `onHPChanged` → `CheckChangeView()` → `ChangeViewMode(View2D)` →
   `player.ChangeLane(laneLength - 1)`이 `changeLaneCor != null` 가드에 걸려 **무시됨**.
3. 직후 `currentViewMode = View2D`로 바뀜.
4. 진행 중이던 3D 이동 코루틴은 그대로 끝나서 `currentLane`을 입력 방향 레인으로 확정.
5. `Update()` 174~175의 보정 코드는 `ChangeLane()`을 호출하지만, `ChangeLane()` 첫 가드가 `View2D`면 return이라
   **2D에서는 절대 동작하지 않는 죽은 코드**. 따라서 잘못된 레인이 복구되지 않음.
6. 2D에서는 `Move()`도 막혀 있어 플레이어가 직접 빠져나올 수도 없음 → 잘못된 레인에 영구 고정.

요약: 2D 강제 이동이 "일반 레인 이동"과 같은 `ChangeLane()`을 공유하는데, 그 가드(진행 중 코루틴 / 2D 모드)가 강제 이동까지 막는다.

## 잠재적 동일 원인
- `allowLaneChangeWhileJumping`을 false로 바꾸면 점프 중 2D 전환 시에도 같은 버그 발생.

## 영향 범위
- `ChangeLane()` 호출처: `Player.Start`(157), `Player.Update`(175), `Player.Move`(188), `GameManager.ChangeViewMode`(49). 외부 호출은 `GameManager` 1곳.
- `currentLane`은 `Whomp`(89, 145), `Lakitu`(140) 판정에도 쓰이므로 레인 값이 실제 위치와 맞아야 함.
