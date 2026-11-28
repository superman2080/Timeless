# ItemPickupSfx — Research

목표: 아이템마다 획득 시 고유 효과음이 나도록 연결한다. 재생은 AudioPool 구조(`docs/AudioPool`)를 그대로 쓴다.

## 1. 현재 획득 효과음 흐름
- 아이템 스크립트는 두 종류다. 둘 다 충돌 시 `(Pool<PooledAudio>.Instance as AudioPool)?.PlaySFX(타입)` 후 `SetActive(false)`.
  - `Obstacles/PowerUpItem.cs`: 필드 `public SfxType pickupSfx = SfxType.PICKUP;` (48행에서 재생).
    - 플레이어가 `scoreInsteadForm` 형태면 효과 대신 점수(`scoreInstead`)를 준다. 이 경우에도 같은 `pickupSfx`를 재생한다.
  - `Obstacles/HealObject.cs`: 필드 `public SfxType healSfx = SfxType.PICKUP;` (34행에서 재생).
- `SfxType` (`Datas/AudioDataSO.cs`): `NONE, HIT, PICKUP, BGM_TRANSITION, BOSS_ROAR, BOSS_CEILING_BREAK, BOSS_LANDING`. 값이 int로 직렬화되므로 새 값은 끝에 추가해야 한다.
- `AudioData.asset`의 `PICKUP`(2) = `Effect Audio/keycard_pickUp.ogg`, volume 0.5.
- **결과: 모든 아이템이 같은 keycard 효과음을 낸다.**

## 2. 아이템 프리팹 현황 (`Assets/07. Prefabs/Prefab_Item/`)
| 프리팹 | 스크립트 | 내용 | 현재 SFX |
|---|---|---|---|
| `HP.prefab` | HealObject | 회복 3 | `healSfx: 2` (PICKUP) |
| `KEY_CARD.prefab` | HealObject | 회복 5 | 직렬화 값 없음 → 기본값 PICKUP |
| `SUPER_MUSHROOM.prefab` | PowerUpItem | 형태 변경 + 풀 회복, 이미 큰 형태면 점수 | `pickupSfx: 2` |
| `MINI_MUSHROOM.prefab` | PowerUpItem | 형태 변경, 이미 작은 형태면 점수 | `pickupSfx: 2` |
| `P_SWITCH.prefab` | PowerUpItem | 효과 추가 | `pickupSfx: 2` |
| `SUPER_STAR.prefab` | PowerUpItem | 효과 추가 | `pickupSfx: 2` |

- `KEY_CARD.prefab`에는 예전 필드 `healSFX: {AudioClip keycard_pickUp}`이 직렬화 잔재로 남아 있다(현재 코드에 없는 필드, 동작 영향 없음).
- 아이템 프리팹에는 `AudioSource`가 없다.

## 3. 사용 가능한 클립 (`Assets/08. Audio/Effect Audio/`, 커밋 `57ee03a sfx item`)
| 클립 | 대응 아이템 | 현재 사용처 |
|---|---|---|
| `SFX_Item_01_SuperMushroom_v2.wav` | SUPER_MUSHROOM | 없음 |
| `SFX_Item_02_SuperStar_v2.wav` | SUPER_STAR | 없음 |
| `SFX_Item_04_MiniMushroom_v2.wav` | MINI_MUSHROOM | 없음 |
| `SFX_Item_06_PSwitch_v2.wav` | P_SWITCH | 없음 |
| `SFX_Item_09_ScoreBonus.wav` | 점수 대체 획득 | 없음 |
| `keycard_pickUp.ogg` | KEY_CARD | `AudioData.asset`(PICKUP) |
| `SFX_Item_03_BlueShell`, `05_PowBlock`, `07_NightmareFlower`, `08_GhostMushroom` | 대응 아이템 없음 | 없음 |

- HP 전용 클립은 없다.
- 새 클립은 모두 `preloadAudioData = 0`이다. `AudioPool.Start`가 `data.sfx` 전체를 `LoadAudioData`하고, `PooledAudio`가 로드 완료를 기다리므로 추가 처리는 필요 없다.

## 4. 위험 요소
- `SfxType` 중간 삽입 금지(기존 에셋·프리팹 값이 밀림). 끝에 추가한다.
- 프리팹 수정은 에디터 API(`LoadPrefabContents` → `SaveAsPrefabAsset`)로 한다. 직접 YAML을 고치지 않는다.
- `AudioData.asset` 수정은 SerializedObject로 한다. `OpenScene` 뒤에 로드한 에셋만 유효하다(AudioPool 작업에서 확인: `OpenScene`이 참조 없는 에셋을 언로드).
- 인코딩: `PowerUpItem.cs`, `HealObject.cs`, `AudioDataSO.cs` 모두 UTF-8.
