# PSwitch — Research

목표: P스위치 아이템. 먹으면 현재 뷰를 반대로 강제 전환(3D ↔ 2D)하고 10초 동안 HP 임계값에 의한 뷰 전환을 막는다.

## 관련 파일

| 파일 | 역할 | 인코딩 |
|---|---|---|
| `Assets/02. Scripts/Player/Player.cs` | `CheckChangeView`(HP 임계값 → 뷰 전환), `ApplyStats`, `Stat<T>` | **CP949** |
| `Assets/02. Scripts/Player/State/PlayerStateMachine.cs` | 형태 1개 + 효과 여러 개, `duration` 후 자동 제거 | UTF-8 |
| `Assets/02. Scripts/Player/State/PlayerEffectSO.cs` | 시간제 효과 SO (`duration`, 배율, `invincible`) | UTF-8 |
| `Assets/02. Scripts/Player/State/PlayerStats.cs` | 합성 스탯 struct | UTF-8 |
| `Assets/02. Scripts/Obstacles/PowerUpItem.cs` | 먹으면 `form`/`effect` 적용 | UTF-8 |
| `Assets/02. Scripts/Obstacles/InteractionObject.cs` | `ObjectType` enum (마지막 `CHAIN_CHOMP`) | 한글 주석 깨짐 — ASCII만 삽입 |
| `Assets/02. Scripts/GameManager.cs` | `ChangeViewMode`, `currentViewMode`, 임계값 | UTF-8 |
| `Assets/02. Scripts/Camera/CameraManager.cs` | `SwitchCamera` → 코루틴 큐 | — |
| `Assets/02. Scripts/Map/LaneManager.cs` | `RemoveAllLaneObjects`, 패턴 선택 `patternSet.Select(mode, hpRatio)` | CP949 (작업 중 변경 있음, 수정 안 함) |
| `Assets/02. Scripts/Datas/PatternSetSO.cs` | `threshold2DView`(0.5), `threshold3DView`(0.7), `ToLocalRatio` | UTF-8 |
| `Assets/02. Scripts/Editor/PatternEditorWindow.cs` | `GetColor` | — |
| `Assets/11. Datas/ObjectData.asset` | `ObjectType` → 프리팹 | — |
| `Assets/07. Prefabs/Prefab_Item/` | `SUPER_MUSHROOM`, `MINI_MUSHROOM` 프리팹 (복사 원본) | — |

## 현재 뷰 전환 흐름

```
stat.HP setter → onHPChanged → Player.CheckChangeView()
  hpRatio <= threshold2DView && 3D → ChangeViewMode(2D, 1.5f) + audioPlayer.trigger = true + laneManager.RemoveAllLaneObjects()
  hpRatio >= threshold3DView && 2D → ChangeViewMode(3D, 1.5f) + 같은 처리
```
- HP는 `Player.Update`에서 매 프레임 감소(`HPDecrement`, 무적이면 정지) → `CheckChangeView`가 사실상 매 프레임 호출됨.
- 뷰 전환을 부르는 곳은 `CheckChangeView` 한 곳뿐. 전환 묶음(카메라 + BGM 트리거 + 레인 오브젝트 회수)이 두 분기에 중복되어 있음.
- `GameManager.ChangeViewMode`: 같은 모드면 무시. 2D면 글리치 + 픽셀화 + `player.ChangeLane(마지막, force)`. 3D면 픽셀화 1. 이후 `CameraManager.SwitchCamera`(큐에 넣어 순차 실행).
- `AudioPlayer.trigger`: true면 효과음 + BGM을 다음 클립으로 페이드. 모드와 무관하게 "다음 곡"으로 넘어가는 구조.

## 상태머신 순서 (설계에 영향)

- `AddEffect`: 새 효과면 `effects.Add` → `effect.OnEnter` → `RecalculateStats`. **이미 있는 효과면 `remaining`만 갱신하고 `OnEnter`를 부르지 않음.**
- `RemoveEffect`: `effects.RemoveAt` → `effect.OnExit` → `RecalculateStats`. → `OnExit` 시점엔 `Stats`가 아직 이전 값(효과 포함 상태).
- `RecalculateStats`: `Stats`를 먼저 대입 후 `onStatsChanged` 발행 → `Player.ApplyStats` → `stat.SetMaxHP` → HP setter → `CheckChangeView`. 이때 `Stats`는 이미 새 값.
- `Tick`은 `Player.Update`에서 `Time.deltaTime`으로 호출 → `ResizeCoroutine`(timeScale 0) 중엔 효과 시간도 멈춤.

## 패턴/스폰 영향

- `LaneManager.GenerateObjectCoroutine`: `patternSet.Select(currentViewMode, HP/MaxHP)`. `ToLocalRatio`는 `InverseLerp`라 범위 밖 HP도 0~1로 잘림 → 강제 2D인데 HP 90%면 2D 최상위 구간, 강제 3D인데 HP 20%면 3D 최하위 구간 선택. 에러 없음.
- 2D 패턴은 레인 2(마지막)만 사용. `PowerUpItem`은 `player.IsJump != isUpper`면 무시.
- `RemoveAllLaneObjects`는 레인 자식 오브젝트를 회수. 먹은 P스위치는 `OnCollided` 끝에서 이미 `SetActive(false)`.

## 기존 아이템 패턴 (슈퍼/땅콩버섯)

- 형태 아이템은 `PlayerFormSO` 상속 + `PowerUpItem.form`. 시간제는 `PlayerEffectSO` + `PowerUpItem.effect` — 아직 효과 에셋은 하나도 없음(`Assets/11. Datas/PlayerState/`에 Form만 3개).
- 버섯 작업 시 검증은 플레이 모드 일시정지 + eval로 프리팹 생성 후 `OnCollided` 직접 호출, 2D는 리플렉션으로 `currentViewMode` 변경.

## 위험 요소

- `Player.cs`는 CP949. 편집 시 ASCII 코드만 넣거나 CP949로 저장해야 diff가 폭발하지 않음. 새 한글 주석은 CP949로 인코딩해야 함.
- 효과 종료 시 HP가 임계값 밖이면 즉시 원래 뷰로 돌아감 (예: HP 90%에서 강제 2D → 10초 후 바로 3D). 의도된 동작인지 확인 필요.
- 무적 중엔 HP가 안 변해 `CheckChangeView`가 호출되지 않음 → 효과 종료 시 명시적으로 재검사해야 함.
- 강제 3D에서 HP가 계속 깎여 0이 되면 사망 (전환 잠금과 무관).
- 이미 활성인 상태에서 다시 먹으면 `AddEffect`는 시간만 갱신 → 뷰가 다시 뒤집히지 않음.
- 작업 트리에 킬러/멍멍이 작업(`LaneManager.cs`, `InteractionObject.cs`, `ObjectData.asset`, `PatternEditorWindow.cs`, Debug 데이터셋)이 커밋 안 된 상태로 있음 — 같은 파일을 건드리므로 그 변경을 보존해야 함. `ObjectType`의 새 값은 `CHAIN_CHOMP`(17) 다음 18.
- 모델: 씬 `SampleScene`에 저장 안 된 `PSwitch` 오브젝트(메시 `Assets/TripoModels/rusty_parking_bollard_3d_model/rusty_parking_bollard_3d_model.fbx`, 회전 270/180/0).
