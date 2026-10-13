# PlayerState — Research

목표: 플레이어에 상태머신을 도입해, 앞으로 추가할 마리오식 아이템(슈퍼버섯, 슈퍼스타, 미니버섯 등)이 플레이어의 크기·체력·이동속도 등을 바꿀 수 있는 기반을 만든다. 이번 작업 범위는 **기반 + 기존 동작 이전**이고, 개별 아이템(모델/프리팹/ObjectType)은 이후 작업.

## 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Player/Player.cs` | `Stat<T>` 클래스 + `Player`. CP949 + CRLF |
| `Assets/02. Scripts/Player/PlayerAnimator.cs` | `Jump(airTime)` — 클립 속도를 체공시간에 맞춤. UTF-8 |
| `Assets/02. Scripts/Player/InputHandler.cs` | `MoveInput`, `Jump` |
| `Assets/02. Scripts/Obstacles/Obstacle.cs` | `player.IsJump`, `player.stat.isInvincible`, `player.stat.TakeDamage` 사용. UTF-8 |
| `Assets/02. Scripts/Obstacles/HealObject.cs` | `player.IsJump`, `player.stat.Heal` 사용. ASCII |
| `Assets/02. Scripts/Obstacles/InteractionObject.cs` | `ObjectType` enum, 트리거 → `onTargetHitEvent` |
| `Assets/02. Scripts/UI/HPBar.cs` | `player.stat.HP / MaxHP` 사용 |
| `Assets/02. Scripts/GameManager.cs` | `mapSpeed`, `ChangeViewMode` |
| `Assets/01. Scenes/SampleScene.unity` | Player: 위치 (0,0,-9), 스케일 0.6, CapsuleCollider(트리거, r 0.25, h 1.8, center y 0.9), `maxHP 100`, `hpDecrement 3`, `airTime 0.5`, `invincibleSpeed 10`. GameManager `mapSpeed 9` |
| `Assets/07. Prefabs/Prefab_Item/HP.prefab`, `KEY_CARD.prefab` | 둘 다 `HealObject` (회복량 3 / 5) |

## 현재 Player 구조

### `Stat<T>` (Player.cs 상단)
- HP, MaxHP, 무적(`isInvincible`), 무적 쿨다운 관리. 이벤트: `onHPChanged`, `onTakeDamaged`, `onHealed`, `onDied`, `onInvincibleStart/End`.
- `TakeDamage`: 무적이면 무시 → HP 감소 → 0 이하면 `onDied`.
- `Heal`: HP 증가, **MaxHP에 처음 도달하면 무적 코루틴 시작**(무적 중/쿨다운 중 제외). 무적 `invincibleDuration`(5s) → 쿨다운 `invincibleCooldown`(10s).
- `SetMaxHP`: 존재하지만 사용처 없음.
- **버그**: `HP` setter가 `onHPChanged`를 값 대입 **전에** 호출 → 리스너(`CheckChangeView`, HPBar 이벤트)가 직전 HP를 봄. 최대 HP를 바꾸는 아이템이 생기면 2D/3D 전환 판정이 한 박자 늦게 됨.

### `Player`
- 스탯 관련 필드: `maxHP`, `invincibleDuration`, `invincibleCooldown`, `invincibleSpeed`, `hpDecrement`, `airTime`, `jumpCurve`, `allowLaneChangeWhileJumping`.
- 무적 시작 시 `GameManager.mapSpeed = invincibleSpeed`, 종료 시 `originSpeed`(Start 시점 mapSpeed)로 복구 → **슈퍼스타와 사실상 같은 동작이 이미 하드코딩되어 있음.**
- 레인 이동 시간: `ChangeLane`에서 `0.1f` 하드코딩.
- 점프: `airTime` 동안 `jumpCurve`로 `jumpOffsetY` 계산, `FixedUpdate`에서 `(laneX, groundY + jumpOffsetY, z)`로 `MovePosition` 1회.
- 시간당 HP 감소(`hpDecrement`)는 무적이 아닐 때만.
- 사망: 글리치, 콜라이더 끔, 1.5초 후 `SampleScene` 재로드.

### 외부에서 Player를 건드리는 곳 (전부)
- `Obstacle.OnCollded`: `IsJump`로 상/하단 판정, `stat.isInvincible`이면 무시, `stat.TakeDamage(damageAmount)`.
- `HealObject.OnCollded`: `IsJump != isUpper`면 무시, `stat.Heal`.
- `HPBar`: `stat.HP / stat.MaxHP`.
- `GameManager.ChangeViewMode`: `player.ChangeLane(...)`.

## "크기·체력·이동속도"가 닿는 지점

| 바뀔 값 | 현재 위치 | 비고 |
|---|---|---|
| 크기 | Player 루트 `transform.localScale` 0.6 | 피벗이 발밑(콜라이더 center y 0.9) → 스케일해도 발은 지면에 붙음. 콜라이더도 같이 커짐 |
| 최대 체력 | `Stat.MaxHP` (`SetMaxHP` 있음) | HP 비율로 2D/3D 전환 → 최대 HP 변경 시 비율이 변해 뷰 전환이 일어날 수 있음 |
| 받는 피해 | `Obstacle.damageAmount` → `stat.TakeDamage` | 마리오식 "맞으면 작아짐"은 데미지 대신 형태 변경으로 처리해야 함 |
| 무적 | `Stat.isInvincible` | `Obstacle`이 직접 읽음 |
| 이동속도(월드) | `GameManager.mapSpeed` | 무적이 이미 이 값을 바꿈. 생성 간격은 거리 기준이라 속도 바뀌어도 패턴 유지 |
| 레인 변경 속도 | `ChangeLane`의 `0.1f` | |
| 점프 | `airTime`, `jumpCurve` | `PlayerAnimator.Jump(airTime)`가 클립 속도 맞춤 |
| 시간당 HP 감소 | `hpDecrement` | |

## 위험 요소

- **인코딩**: `Player.cs`는 CP949. 편집 시 유지.
- **mapSpeed 소유권**: 지금은 Player가 `originSpeed`를 Start에서 캡처해 복구. 여러 효과가 속도를 바꾸게 되면 "원래 값으로 복구"가 서로 덮어씀 → 기준 속도 × 배율 합성으로 한 곳에서 계산해야 함.
- **최대 HP 변경과 뷰 전환**: 위 표 참고.
- **무적 판정 위치**: `Obstacle`이 `stat.isInvincible`을 직접 읽음 → 무적의 출처가 상태머신으로 바뀌면 같이 고쳐야 함.
- 씬 재로드로 리셋되므로 상태 저장/복구는 필요 없음.
- 플레이 모드 검증: 이전 작업에서 에디터가 백그라운드에서 플레이 프레임을 진행하지 않았음 → eval 기반 검증 위주가 될 수 있음.
