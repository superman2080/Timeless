# SuperMushroom — Research

목표: 슈퍼 버섯 아이템. 플레이어가 먹으면 **크기 증가 + HP 100% 회복**. 모델 `Assets/03. Models/Static/Super_Mushroom.fbx`.

## 1. 이미 있는 것 (재사용 대상)

### `PowerUpItem` — `Assets/02. Scripts/Obstacles/PowerUpItem.cs`
- `InteractionObject` 상속. 필드: `PlayerFormSO form`, `PlayerEffectSO effect`, `AudioClip pickupSFX`.
- 충돌 시: `player.IsJump != isUpper`면 무시 → `form`이 있으면 `StateMachine.ChangeForm(form)`, `effect`가 있으면 `AddEffect(effect)` → SFX → `SetActive(false)`.
- 이동은 `FixedUpdate`에서 `Rb.MovePosition(... Vector3.back * mapSpeed ...)` (HealObject와 동일).
- **현재 어떤 프리팹에도 붙어 있지 않음** (Prefab_Item의 `HP.prefab`, `KEY_CARD.prefab`은 둘 다 `HealObject`, guid `8588b6...`). 슈퍼 버섯이 첫 사용처.
- **HP 회복 기능은 없음** → 추가 필요.

### 상태머신 — `Assets/02. Scripts/Player/State/`
- `PlayerFormSO`: `PlayerStats stats`(절대값) 하나. `ModifyStats`에서 그대로 덮어씀.
- `PlayerStateMachine.ChangeForm(form)`: 같은 형태면 return. 아니면 `OnExit` → 교체 → `OnEnter` → `RecalculateStats()` → `onStatsChanged` → `Player.ApplyStats`.
- `ProcessHit(damage)`: 효과들 → 형태 순으로 `OnHit` 통과, 남은 데미지 반환.
- 기본 형태 에셋: `Assets/11. Datas/PlayerState/Form_Normal.asset` — `scale 0.6, maxHP 100`, 나머지 배율 1, invincible 0.

### `Player` — `Assets/02. Scripts/Player/Player.cs`
- `ApplyStats`: `transform.localScale = one * stats.scale` (즉시 적용), `stat.SetMaxHP(stats.maxHP)`, `mapSpeed = baseMapSpeed * 배율`.
- `Stat.Heal(amount)`: `HP += amount`, `MaxHP`로 Clamp. `onHPChanged` → HPBar 이벤트 + `CheckChangeView()`.
- `CheckChangeView()`: HP 비율이 `threshold3DView` 이상이고 2D 모드면 **3D로 전환 + BGM 크로스페이드 + 레인 오브젝트 전부 회수**. → 2D에서 버섯을 먹어 100% 회복되면 3D로 복귀함(그리고 화면의 오브젝트가 다 사라짐).
- `TakeHit(damage)`: 무적이면 무시 → `StateMachine.ProcessHit` → 남은 데미지만 `TakeDamage`.
- `defaultForm`은 `[SerializeField] private` — 외부에서 읽을 수 없음.
- `PlayerAnimator.cs`는 scale을 건드리지 않음.

### 스폰 등록 경로 (CLAUDE.md 확인)
`ObjectType` enum(`InteractionObject.cs`, 마지막 값 `OCTOOMBA` = 13) → `ObjectData.asset`(type 0~13 등록됨) → `PatternEditorWindow.GetColor` → 패턴 배치.
- `ObjectType`은 **정수로 직렬화**되므로 enum 끝에 추가해야 기존 에셋 값이 안 밀림.
- `InteractionObject.cs`의 기존 주석은 CP949 — 편집 시 인코딩 유지 주의.

## 2. 모델/머티리얼 상태 (git status 기준, 미커밋)
- `Super_Mushroom.fbx`(+meta) 신규. meta의 `externalObjects`가 `tripo_mat_808f0f8d` → `Assets/06. Materials/Item/red_mushroom_3d_model.mat`(guid `dc5aaa...`)로 리맵되어 있음 → **머티리얼 연결은 이미 됨**.
- 그 머티리얼의 `_BaseMap`은 `Assets/TripoModels/red_mushroom_3d_model/red_mushroom_3d_model.fbm/Color.jpg`를 참조. 옛 fbx/mat은 삭제됐지만 **`.fbm` 폴더는 남아 있어야 함** — 지우면 텍스처가 빠짐.

## 3. 프리팹 참고 (`HP.prefab`)
- 루트: BoxCollider(`isTrigger 1`, size 0.6), Rigidbody(`useGravity 0`, `isKinematic 0`), 스크립트 컴포넌트, 레이어 0.
- 자식에 시각 모델 `HP_Sphere`(scale 0.58)와, 중첩 프리팹으로 `Super_Mushroom.fbx`(guid `099785...`, 오브젝트 이름 `red_mushroom_3d_model`)가 **둘 다 활성 상태로** 들어가 있음. 옛 red_mushroom fbx가 같은 GUID로 `Super_Mushroom.fbx`로 옮겨진 것.

## 4. 위험 요소
- 크기를 키우면 플레이어 콜라이더도 같이 커져 장애물에 더 잘 맞음(마리오 느낌상 의도와 일치하지만 체감 확인 필요). 루트 피벗이 발밑이 아니면 커질 때 땅에 파묻히거나 뜰 수 있음 → 구현 때 MCP로 확인.
- 이미 슈퍼 형태일 때 또 먹으면 `ChangeForm`이 return → 회복만 일어남(문제 없음).
- 형태는 지금 구조상 **되돌리는 곳이 없음** → 슈퍼 상태가 언제 끝나는지 정해야 함 (Plan 의문점 1).

---

# 2차: 커지는 연출 — Research (2026-10-19)

요구: 커질 때 연출 시간 동안 시간을 멈추고, 작은 크기와 큰 크기를 번갈아 보여주다가 커짐(마리오 파워업 연출).

## 크기가 바뀌는 지점
- 크기는 오직 `Player.ApplyStats`에서 `transform.localScale = one * stats.scale`로 **즉시** 바뀜. 호출 경로: `ChangeForm`/`AddEffect`/`RemoveEffect` → `RecalculateStats` → `onStatsChanged` → `ApplyStats`.
- `ChangeForm`은 `OnEnter`를 **`RecalculateStats`보다 먼저** 호출 → `SuperFormSO.OnEnter`에서 연출을 시작하면 바로 뒤의 `ApplyStats`가 scale을 덮어씀. 연출은 `ApplyStats` 쪽에서 처리하는 게 순서상 자연스러움.
- 현재 크기가 커지는 경우는 `Form_Normal(0.6) → Form_Super(0.9)` 하나뿐. 작아지는 경우(슈퍼 피격)는 즉시 0.6.
- 씬의 Player `localScale` = 0.6 (= `Form_Normal`). 그래서 `Start`의 `ChangeForm(defaultForm)`에서는 크기 변화 없음 → 연출이 시작될 때 잘못 재생되지 않음.

## `Time.timeScale = 0`의 영향 (프로젝트 코드 확인)
현재 `timeScale`을 쓰는 코드는 **없음** (일시정지 메뉴 등과 충돌 없음).

| 대상 | timeScale 0일 때 |
|---|---|
| 물리(`FixedUpdate`, 트리거) | 멈춤 → 월드 이동(`Rb.MovePosition`), 충돌 모두 정지. 연출 중 피격 없음 |
| `Track`/`BackgroundManager`/`ScoreUI`/HP 감소 | `Time.deltaTime` 기반 → 0이 되어 정지 |
| `Player.Update` | **계속 호출됨**. `Move()`가 입력을 받으면 `ChangeLaneCoroutine`/`JumpCoroutine`(`WaitForFixedUpdate`)을 시작하고 멈춘 채로 있다가 연출 후 실행됨 → 연출 중 입력 무시 필요 |
| Player `Animator` | `updateMode = Normal` → 포즈가 멈춤 (마리오처럼 정지 상태로 커짐) |
| `CameraManager`(`WaitForSeconds`, `deltaTime`), Cinemachine Brain(`IgnoreTimeScale 0`) | 멈췄다가 연출 후 이어서 진행 |
| BGM/SFX(`AudioSource`) | timeScale 영향 없음 → 계속 재생 |
| Lakitu/Whomp(`Time.time`) | scaled time이라 같이 멈춤 |

- 연출 코루틴 자체는 `WaitForSecondsRealtime` 또는 `Time.unscaledDeltaTime`을 써야 진행됨.
- 2D에서 먹으면 `Heal` → `CheckChangeView` → 3D 전환이 같은 프레임에 시작됨. 카메라 전환 코루틴과 Cinemachine 블렌드는 멈췄다가 연출 후 진행되고, `RemoveAllLaneObjects`는 즉시 실행됨.

## 위험 요소
- 연출 중 씬이 재로드되면 `timeScale`이 0으로 남음. 재로드는 사망 시에만 일어나고, 연출 중에는 HP가 줄지 않아 사망할 수 없음 → 실제로는 발생 안 함.
- 연출 중 같은 프레임에 다른 크기 변화(효과 추가 등)가 들어오면 연출이 그 값을 덮어씀 → 현재 그런 아이템은 없음.
