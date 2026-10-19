# MiniMushroom — Research

땅콩버섯(미니 버섯) 아이템: 먹으면 작아짐 + 이동속도 감소 + HP를 뷰 모드 임계값 기준 비율로 설정. 피격 시 원래 크기로 복귀. 미니 상태에서 슈퍼버섯을 먹으면 효과 대신 점수.

## 1. 관련 파일 (코드에서 확인)

| 종류 | 경로 | 확인 내용 |
|---|---|---|
| 형태 베이스 | `Assets/02. Scripts/Player/State/PlayerFormSO.cs` | `stats`(절대값)로 스탯 덮어씀. 한 번에 1개 |
| 효과 베이스 | `Player/State/PlayerEffectSO.cs` | 배율, `duration`(0 이하 = 무기한) |
| 상태머신 | `Player/State/PlayerStateMachine.cs` | `ChangeForm`: 같은 형태면 **return(OnEnter 안 불림)**. 순서: 이전 `OnExit` → 새 `OnEnter` → `RecalculateStats` |
| 슈퍼 형태 | `Player/State/SuperFormSO.cs` | `OnHit`에서 `ChangeForm(player.DefaultForm)` 후 `0` 반환(데미지 무시) — 미니 형태가 그대로 따라 할 수 있는 선례 |
| 스탯 | `Player/State/PlayerStats.cs` | `scale`, `maxHP`, `mapSpeedMultiplier`, `jumpHeightMultiplier`, `airTimeMultiplier`, `hpDecrementMultiplier`, `invincible` |
| 플레이어 | `Player/Player.cs` (**CP949**) | `Stat<T>.HP` setter는 private. 외부에서 HP를 바꾸는 방법은 `TakeDamage`/`Heal`/`SetMaxHP`뿐. `ApplyStats`: 크기 다르면 크기 연출(`timeScale 0`) + `SetMaxHP` + `mapSpeed = base × 배율` |
| 레인 이동 | `Player.cs` `ChangeLane` | 레인 이동 시간 `0.1f` 하드코딩. 스탯에 레인 이동 속도 항목 없음 |
| 아이템 | `Obstacles/PowerUpItem.cs` | `form`/`effect`/`fullHeal`/`pickupSFX`. 점프 여부(`IsJump != isUpper`)가 맞으면 적용 후 비활성화 |
| 임계값 | `Datas/PatternSetSO.cs` | `threshold2DView 0.5`, `threshold3DView 0.7`. **`ToGlobalRatio(mode, local)`**: 2D = `Lerp(0, 0.7, local)`, 3D = `Lerp(0.5, 1, local)` → local 0.1이면 2D 0.07, 3D 0.55. 요청한 계산식과 정확히 일치 |
| 임계값 접근 | `GameManager.cs:28-29`, `Map/LaneManager.cs:7` | `GameManager.Instance.laneManager.patternSet` |
| 점수 | `UI/ScoreUI.cs` | `score`는 private, 시간 기반 증가만 있음. **점수 추가 API 없음**. 강조 연출은 `Update` 안에서 마일스톤을 넘었을 때만. `SampleScene.unity`에 1개 |
| ObjectType | `Obstacles/InteractionObject.cs` (**CP949**) | 마지막 값 `SUPER_MUSHROOM`(14) |
| 패턴 에디터 색 | `Editor/PatternEditorWindow.cs:261` | `SUPER_MUSHROOM` → 빨강 |
| 슈퍼버섯 프리팹 | `Assets/07. Prefabs/Prefab_Item/SUPER_MUSHROOM.prefab` | `PowerUpItem`(form = Form_Super guid `252f0548…`, `fullHeal 1`), 모델은 `Super_Mushroom.fbx` 중첩 프리팹 |
| 형태 에셋 | `Assets/11. Datas/PlayerState/Form_Normal.asset`, `Form_Super.asset` | Normal scale 0.6, Super scale 0.9, 둘 다 maxHP 100 |
| 디버그 데이터 | `Assets/11. Datas/DebugCreateObject3D.asset`, `Patterns/3D/Pattern3D_24.asset` | 슈퍼버섯 테스트용 |

## 2. 모델
- `Assets/03. Models/Static/Mini_Mushroom.fbx` (guid `dfe6cdc9…`, git 미추적). `externalObjects`로 `tripo_mat_0d10698e` → `Assets/06. Materials/Item/M_Mini_Mushroom.mat`(guid `90854654…`) 연결됨. 텍스처는 `Assets/TripoModels/mushroom_3d_model/mushroom_3d_model.fbm/` (지우면 안 됨).
- SampleScene에 저장 안 된 `Mini_Mushroom` 오브젝트(프리팹 아님, MeshFilter/MeshRenderer만, 회전 X 270°, scale 1, bounds 반경 약 0.44×0.49×0.41).

## 3. 영향/위험
- HP 설정 시 `onHPChanged` → `CheckChangeView`가 불림. 3D에서 55%, 2D에서 7%는 전환 임계값을 넘지 않으므로 뷰 전환 없음 (`threshold2DView`가 0.5 미만으로 바뀌거나 비율이 0이면 경계에 걸릴 수 있음).
- `TakeDamage`/`Heal`로 HP를 맞추면 `onTakeDamaged`/`onHealed`가 발생하지만 `Player.cs` 밖에 구독자가 없음.
- `ChangeForm`은 같은 형태면 무시 → 미니 상태에서 미니를 또 먹으면 `OnEnter`가 안 불려 HP가 다시 설정되지 않음.
- `OnEnter` 시점에는 아직 `SetMaxHP`가 반영 전 → 이전 형태의 MaxHP 기준. 모든 형태가 maxHP 100이라 현재는 문제 없음.
- 크기 변경 연출(`ResizeCoroutine`)은 `timeScale 0`이라 먹는 순간 잠시 멈춤 — 기존 동작 그대로 적용됨.
- `Player.cs`, `InteractionObject.cs`는 CP949 — 편집 시 인코딩 유지.
