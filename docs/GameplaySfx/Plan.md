# GameplaySfx — Plan

근거: `docs/GameplaySfx/Research.md`
상태: **구현 완료 (2026-11-28)** — 피드백 반영: 무적 피격음 추가, 나머지 기본안 확정

## 구현 현황 요약
- 완료: Phase 1~4 전 항목 (16/16). 미완료 없음.
- 플레이 검증 결과 (풀에서 실제 재생된 클립)
  - 레인 이동 → Move_LR. 이동 중 재입력, 끝 레인에서 더 누르기, 강제 이동(2D 전환 경로) → 무음.
  - 점프 → Jump. 점프 중 재입력 → 무음.
  - ChainChomp → Bite, BulletBill → Blast, Boo → Chill, SpikeBall → Explode.
  - Octoomba → InkBlind, Goomba → Bump, SpikeRock → Thwomp_Crush, Blockade → Attack_Sound.
  - 무적(스타) 상태 충돌 → Attack_Sound + 적이 튕겨 나감.
  - Lakitu 투척 → SpikeDrop. Whomp 자연 발동(플레이어 레인 앞) → Whomp_Fall.
- `Player.cs`는 CP949 바이트를 보존한 채 수정했다(diff 15줄).
- 계획과 달라진 점
  - `Gm_Obstacle_Fan.prefab`은 원래 있던 Missing Script(`prop_fan_large`) 때문에 다시 저장할 수 없었다.
  - 다만 이 프리팹은 `hitSfx`/`knockSfx`가 직렬화되어 있지 않아 코드 기본값(HIT/KNOCK)이 그대로 적용된다. 검증 eval에서 HIT/KNOCK을 확인했다.
  - 저장 시도에서 에러 로그 1건이 남았다.
- 콘솔: 새 에러 0. NRE 1건은 테스트 스크립트가 막 생성한 Whomp에 초기화 전 코루틴을 강제로 실행해 생긴 부산물이다. 게임 경로에서는 발생하지 않는다.

## 설계 요약

### SfxType 추가 (끝에) + `AudioData.asset` 등록
| SfxType | 클립 | volume |
|---|---|---|
| PLAYER_JUMP | `SFX_Jump.wav` | 0.5 |
| PLAYER_MOVE | `SFX_Move_LR.wav` | 0.4 |
| HIT_CHAIN_CHOMP | `SFX_Hit_02_ChainChomp_Bite.wav` | 0.7 |
| HIT_WHOMP_FALL | `SFX_Hit_03_Whomp_Fall.wav` | 0.7 |
| HIT_BULLET_BILL | `SFX_Hit_04_BulletBill_Blast.wav` | 0.7 |
| HIT_BOO | `SFX_Hit_05_Boo_Chill.wav` | 0.7 |
| HIT_SPIKE_BALL | `SFX_Hit_06_SpikeBall_Explode.wav` | 0.7 |
| HIT_LAKITU_THROW | `SFX_Hit_07_Lakitu_SpikeDrop.wav` | 0.7 |
| HIT_OCTOOMBA | `SFX_Hit_08_Blooper_InkBlind.wav` | 0.7 |
| HIT_GOOMBA | `SFX_Hit_09_Goomba_Bump.wav` | 0.7 |
| HIT_STONE | `SFX_Hit_01_Thwomp_Crush.wav` | 0.7 |
| KNOCK | `Effect Audio/Attack_Sound.mp3` (HIT과 같은 클립, 볼륨 따로) | 0.5 |

### 조작 — `Player.cs` (CP949 바이트 보존 수정)
- 필드: `public SfxType jumpSfx = SfxType.PLAYER_JUMP;`, `public SfxType moveSfx = SfxType.PLAYER_MOVE;`
- `Jump()`: 가드 통과 후 `jumpSfx` 재생.
- `Move()`: 입력으로 `ChangeLane`이 실제로 시작됐을 때만 `moveSfx` 재생.
  - `ChangeLane`이 `bool`(이동 시작 여부)을 반환하도록 바꾼다.
  - 시작 배치와 2D 강제 이동에서는 소리가 나지 않는다.
  - 레인 끝에서 더 누르거나 이동 중에 누르면 소리가 나지 않는다.

### 적 피격 — 프리팹 `hitSfx` 지정 (코드 수정 없음)
| 프리팹 | hitSfx |
|---|---|
| ChainChomp | HIT_CHAIN_CHOMP |
| BulletBill | HIT_BULLET_BILL |
| Boo | HIT_BOO |
| SpikeBall | HIT_SPIKE_BALL |
| Octoomba | HIT_OCTOOMBA |
| Goomba | HIT_GOOMBA |
| SpikeRock (StoneFragment) | HIT_STONE |
| Blockade / Fan / Pipe / Robot_Arm | HIT 유지 (의문점 2) |

### 무적 상태 피격음 (피드백)
- `Obstacle.knockSfx` 기본값을 `NONE`에서 `KNOCK`으로 바꾼다.
- `Obstacle`을 쓰는 프리팹 전부(적 7종과 일반 장애물 4종)의 `knockSfx`를 `KNOCK`으로 저장한다.
- 무적 상태에서 부딪혀 적이 튕겨 나갈 때 `Attack_Sound`가 재생된다.

### 피격 판정이 없는 적의 행동음 (코드 소량)
- `Whomp.cs`: `public SfxType fallSfx = SfxType.HIT_WHOMP_FALL;` 필드 추가. `EruptCoroutine`에서 넘어짐이 끝나 폭발하는 시점에 재생.
- `Lakitu.cs`: `public SfxType throwSfx = SfxType.HIT_LAKITU_THROW;` 필드 추가. `ThrowBall()`에서 공을 생성하면 재생.

## Phase 1 — 코드
- [x] `AudioDataSO.cs`: SfxType 12개 추가(KNOCK 포함).
- [x] `Obstacle.cs`: `knockSfx` 기본값을 KNOCK으로.
- [x] `Player.cs`(CP949 보존): `jumpSfx`/`moveSfx`, `ChangeLane` bool 반환, `Jump`/`Move`에서 재생. 수정 후 파일이 여전히 CP949인지 확인.
- [x] `Whomp.cs`: `fallSfx` 필드 + 재생.
- [x] `Lakitu.cs`: `throwSfx` 필드 + 재생.
- [x] 컴파일 + 콘솔 확인. `ChangeLane` 반환형 변경으로 다른 호출처가 깨지지 않는지 확인.

## Phase 2 — 데이터 / 프리팹 (eval)
- [x] `AudioData.asset`에 12개 항목 추가.
- [x] 적 프리팹 7개 `hitSfx` 지정. Obstacle 프리팹 11개 `knockSfx` = KNOCK. Whomp/Lakitu는 기본값이 프리팹에 저장되도록 재저장.
- [x] 에디트 모드 eval: 프리팹 → SfxType → 클립 매핑 확인.

## Phase 3 — 플레이 검증
- [x] 점프/레인 이동 입력(키 시뮬레이션 또는 `Jump`/`ChangeLane` 리플렉션) 시 해당 클립이 풀에서 재생되는지.
  - 시작 배치와 2D 강제 이동에서는 소리가 나지 않는지.
- [x] 적 7종: 충돌 이벤트를 호출해 각 hit 클립이 재생되는지.
- [x] 무적 상태에서 충돌하면 Attack_Sound가 재생되고 적이 튕겨 나가는지.
- [x] Whomp 넘어짐, Lakitu 투척 시 소리가 나는지(`GenerateRow`나 풀 생성 후 트리거).
- [x] 콘솔 새 에러/경고 0. `runInBackground` 원복.

## Phase 4 — 문서
- [x] `CLAUDE.md` 오디오 단락에 조작/적 효과음 지정 위치를 한 줄 추가.
- [x] Plan.md 상단에 구현 현황 요약.

## 결정 (피드백 반영)
1. 무적 상태에서 부딪혀 적이 튕겨 나갈 때 → `Effect Audio/Attack_Sound.mp3`(KNOCK).
2~5. 기본안대로 확정.
