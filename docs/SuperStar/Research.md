# SuperStar — Research

요구사항: 슈퍼스타 아이템. 먹으면 5초 동안 몸이 무지개색으로 빛나고, 무적이 되며, 맵 속도가 빨라진다. 무적 중에 부딪힌 적은 레인 위치에 따라 튕겨 나간다(왼쪽 레인은 왼쪽, 오른쪽 레인은 오른쪽, 가운데 레인은 위아래로만). 모델은 씬에 있는 `SuperStar`를 쓴다.

## 1. 재사용할 기존 시스템

### 상태머신 (`Assets/02. Scripts/Player/State/`)
- `PlayerEffectSO`(`PlayerEffectSO.cs`): `duration`(초, 기본 5), 배율 필드 `mapSpeedMultiplier`, `invincible` 플래그가 **이미 있다**. `ModifyStats`에서 형태 스탯에 곱하거나 OR 연산한다.
- `PlayerStateMachine.AddEffect`: 같은 효과가 이미 있으면 남은 시간만 갱신한다. `Tick`에서 `OnUpdate(owner, dt)`를 호출하고 시간이 다 되면 `RemoveEffect`가 `OnExit`을 부른 뒤 스탯을 다시 계산한다.
- `PlayerStateSO` 주석: SO는 공유되므로 런타임 값은 Player 쪽에 둔다.
- 지금까지 `PlayerEffectSO` 에셋을 쓴 아이템은 없다(`Assets/11. Datas/PlayerState/`에는 Form_Normal, Form_Super, Form_Mini만 있음).

→ 무적과 맵 속도는 **코드 없이** 효과 에셋(`duration 5`, `invincible true`, `mapSpeedMultiplier > 1`)만으로 된다.

### 무적이 이미 처리하는 것 (`Player.cs`, CP949 인코딩)
- `IsInvincible => StateMachine.Stats.invincible`
- `Update`: 무적이면 `HPDecrement()`를 건너뛴다(HP 자연 감소가 멈춤).
- `TakeHit`: 무적이면 return한다. `ProcessHit`이 불리지 않으므로 Super/Mini 형태도 풀리지 않는다.
- `ApplyStats`: `GameManager.mapSpeed = baseMapSpeed × stats.mapSpeedMultiplier`. 효과 배율은 형태 배율에 곱해진다(Mini 0.5 × 별 배율).

### 아이템 (`Obstacles/PowerUpItem.cs`)
- `effect` 필드에 넣으면 `AddEffect`가 호출된다. `form`은 비워 둬도 된다. 판정은 `player.IsJump != isUpper`이면 무시한다. 먹으면 `pickupSFX`를 재생하고 `SetActive(false)`.
- 기존 아이템 프리팹: `Assets/07. Prefabs/Prefab_Item/SUPER_MUSHROOM.prefab`, `MINI_MUSHROOM.prefab`. 구성은 `Transform, BoxCollider, Rigidbody, PowerUpItem` + 모델 자식(이전 작업에서 HP.prefab을 복사해 만듦).

### 장애물 (`Obstacles/Obstacle.cs`)
```csharp
bool hit = isUpper ? player.IsJump : (ignoreJumpedPlayer || !player.IsJump);
if (!hit || player.IsInvincible) return;   // 무적이면 그냥 통과
```
- 이동: `FixedUpdate → virtual ObstacleMovement()` = `Rb.MovePosition(pos + back × mapSpeed × dt)`
- `OnHitPlayer(player)`: 서브클래스 연출 훅이며 실제로 맞았을 때만 불린다.
- `Obstacle` 서브클래스: `Boo`(알파 연출, `OnEnable` override), `Octoomba`(먹물, `OnEnable`/`OnHitPlayer` override), `SpikeBall`(`ObstacleMovement` override: 비행 포물선), `StoneFragment`(`ObstacleMovement` override: 솟아오름). 모두 `base.OnEnable()`/`base.ObstacleMovement()`를 호출한다.
- LASER/BARREL/BLOCKADE/FAN/ROBOT_ARM 프리팹도 `Obstacle`이다.
- **`Obstacle`이 아닌 것**: `Whomp`(본체 피격 판정 없음, 같은 레인 진입 시 자폭), `Lakitu`(상공 유영, 판정 없음). 이 둘은 부딪힘 판정 자체가 없다.
- 모든 장애물 프리팹 Rigidbody: `isKinematic 0`, `useGravity 0` (15개 모두). 이동은 전부 `MovePosition`으로 수동 처리한다.

### 레인 위치
- `LaneManager`: `xOffset = (i - centerOffset) × laneInterval`. 실측 결과 lane 0 → x=-1, 1 → x=0, 2 → x=1 (하단/상단 레인이 같은 인덱스와 x를 공유).
- `Lane.GenerateObstacle`이 `obj.currentLane = LaneIndex`, `isUpper`를 세팅한다. 패턴 스폰, Lakitu의 SpikeBall, Whomp의 StoneFragment 모두 Lane을 거치므로 **`currentLane`은 항상 유효하다**.
- 2D 모드에서는 플레이어가 마지막 레인(2, 오른쪽)에 고정되고, 2D 패턴은 레인 2만 쓴다.

### 풀
- `InteractionObjectPool.Get(type, pos, rot)`가 `SetPositionAndRotation` 후 `SetActive(true)`를 한다. `Lane.GenerateObstacle`은 `Quaternion.identity`를 넘기므로 **튕겨 나가며 회전한 루트 회전값은 재사용 시 초기화된다**.
- 반환은 `SetActive(false)`. `InteractionObject.DisposeCoroutine`은 `z < DisposePos.z`일 때만 비활성화하므로, 위나 옆으로 날아간 오브젝트는 직접 비활성화해야 한다.

## 2. 플레이어 렌더러 (에디터에서 확인)
| 렌더러 | 활성 | 머티리얼 | `_EMISSION` 키워드 |
|---|---|---|---|
| `tripo_node_b5128193` (SkinnedMeshRenderer, 마리오) | O | `creepy_mario_figure_3d_model` (URP/Lit) | 꺼짐, `_EmissionColor` 검정 |
| `char_robotGuard_body`, `char_robotGuard_helmet` | X | `New_CH_M` 등 | 꺼짐 |

- URP Lit 발광은 `_EMISSION` 키워드가 켜져 있어야 보인다. MaterialPropertyBlock으로는 키워드를 켤 수 없다 → **런타임 머티리얼 인스턴스**(`renderer.materials`)에서 키워드를 켜고 `_EmissionColor`를 바꿔야 한다. 공유 머티리얼(에셋)은 건드리지 않는다. 같은 방식이 `Boo.cs`에 있다(인스턴스 머티리얼 + `OnDestroy`에서 Destroy).

## 3. SuperStar 모델 (씬)
- `SampleScene` 루트의 `SuperStar` 오브젝트: pos (0.55, 0, 0.49), rot (270, 0, 0), scale 1, bounds 약 (0.98, 0.96, 0.46).
- 컴포넌트: `Transform, MeshFilter, MeshRenderer`. 메시 `Assets/03. Models/Static/SuperStar.fbx`, 머티리얼 `Assets/06. Materials/Item/M_SuperStar.mat` (URP/Lit).
- **씬이 저장되지 않은 상태(dirty)** 다. 디스크의 SampleScene.unity에는 SuperStar가 없다. 프리팹은 메시/머티리얼 에셋으로 만들 수 있으므로 씬을 저장하지 않아도 된다.
- 프리팹 참조 없음(어떤 .prefab/.unity도 SuperStar.fbx를 참조하지 않음).

## 4. 새 아이템 추가 절차 (CLAUDE.md + 이전 작업)
`ObjectType` 끝에 추가(`InteractionObject.cs`, CP949) → `PatternEditorWindow.GetColor` → 프리팹 → `ObjectData.asset` 등록 → 패턴 배치. 디버그는 `DebugCreateObject3D.asset`의 패턴 항목을 바꿔 테스트한다(현재 `Pattern3D_25` = 미니버섯).

## 5. 위험 요소
- `Player.cs`, `InteractionObject.cs`는 CP949 인코딩이다. 편집 시 디코드 → 편집 → CP949로 다시 인코딩하고, 기존 바이트가 유지되는지 확인해야 한다(SuperMushroom 2차 작업과 같은 방식). `Obstacle.cs`, `PlayerEffectSO.cs`는 UTF-8이다.
- 튕겨 나간 오브젝트가 날아가는 도중 플레이어와 다시 트리거되면 안 된다. `collisionables` 목록 때문에 같은 진입은 한 번만 이벤트가 오지만, 상태 플래그로 확실히 막는다.
- 맵 속도가 올라가면 `LaneManager`가 거리 기준으로 스폰하므로 간격은 유지된다(DistanceSpawn 작업).
- 별 효과 중 형태 변경(버섯)으로 `ApplyStats`가 다시 불려도 효과 배율은 계속 곱해진다(합성 구조).
