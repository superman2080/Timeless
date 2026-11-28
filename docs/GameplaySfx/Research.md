# GameplaySfx — Research

목표: 조작(레인 이동, 점프) 효과음과 적별 피격 효과음을 연결한다. 재생은 AudioPool(`(Pool<PooledAudio>.Instance as AudioPool)?.PlaySFX`)을 쓴다.

## 1. 조작 — `Player/Player.cs` (CP949)
- `Move()` (204행): `handler.MoveInput != 0`이고 3D면 `ChangeLane(currentLane ± 1)`. `handler.Jump`면 `Jump()`.
- `Jump()` (216행): 이미 점프 중이면 return. 아니면 `JumpCoroutine` + `playerAnimator.Jump`.
- `ChangeLane(laneIndex, force)` (227행)
  - 범위 밖이면 return.
  - `force`가 아니면 다음 경우 return: 2D 모드, 이동 중(`changeLaneCor != null`), 점프 중 이동 불가 설정.
  - 통과하면 `ChangeLaneCoroutine` 시작.
  - `force = true`는 2D 전환 시 마지막 레인으로 강제 이동할 때만 쓴다(Player 195행, `GameManager.cs:52`). 175행 시작 배치는 force가 아니다.
  - 외부 호출처는 `GameManager.cs:52` 하나다. 반환값을 쓰지 않으므로 반환형을 bool로 바꿔도 호환된다.
- → 실제 이동이 일어나는 지점: `ChangeLane`에서 코루틴을 시작하는 줄. 점프는 `Jump()`의 가드 통과 후.
- 시작 시 `ChangeLane(currentLane)`(175행)도 같은 경로라, 입력이 아닌 호출과 구분해야 한다(시작 배치 때 소리 방지).
- 파일이 CP949다. 바이트를 보존하며 수정해야 한다(실제 Python `C:\Users\GACHON\AppData\Local\Python\bin\python.exe`로 cp949 디코드/인코드).

## 2. 적 피격 — `Obstacles/Obstacle.cs`
- `public SfxType hitSfx = SfxType.HIT;` 플레이어가 맞으면(72행) 재생. 무적 상태에서 부딪히면 `knockSfx`(기본 NONE)를 재생하고 적이 튕겨 나간다.
- `HIT` = `GeneratedAudio/Attack_Sound.mp3` (vol 0.5). 모든 적이 이 소리를 쓴다(ChainChomp는 명시적으로 1, 나머지는 기본값).

### 적 프리팹 (`Gm_Obstacle_*.prefab`)
| 프리팹 | 스크립트 | 피격 판정 | 비고 |
|---|---|---|---|
| ChainChomp | ChainChomp : Obstacle | 있음 | |
| BulletBill | BulletBill : Obstacle | 있음 | |
| Boo | Boo : Obstacle | 있음 | |
| SpikeBall | SpikeBall : Obstacle | 있음 | Lakitu가 던짐 |
| Octoomba | Octoomba : Obstacle | 있음 | `OnHitPlayer`에서 화면 먹물 연출 |
| Goomba | Obstacle | 있음 | |
| SpikeRock | StoneFragment : Obstacle | 있음 | Whomp가 자폭할 때 까는 돌 파편 |
| Whomp | Whomp : InteractionObject | **없음** | 넘어짐(`EruptCoroutine`, 118행) → 폭발 + 파편 생성 |
| Lakitu | Lakitu : InteractionObject | **없음** | `ThrowBall()`(119행)로 SpikeBall 투척 |
| Blockade / Fan / Pipe / Robot_Arm | Obstacle | 있음 | 일반 장애물 |

## 3. 사용 가능한 클립 (`Assets/08. Audio/Effect Audio/`, 전부 미사용)
| 클립 | 대응 |
|---|---|
| `SFX_Jump.wav` | 점프 |
| `SFX_Move_LR.wav` | 레인 이동 |
| `SFX_Hit.wav` | 일반 피격 |
| `SFX_Hit_01_Thwomp_Crush.wav` | 대응 적 없음(Thwomp 없음) |
| `SFX_Hit_02_ChainChomp_Bite.wav` | ChainChomp |
| `SFX_Hit_03_Whomp_Fall.wav` | Whomp 넘어짐 |
| `SFX_Hit_04_BulletBill_Blast.wav` | BulletBill |
| `SFX_Hit_05_Boo_Chill.wav` | Boo |
| `SFX_Hit_06_SpikeBall_Explode.wav` | SpikeBall |
| `SFX_Hit_07_Lakitu_SpikeDrop.wav` | Lakitu 투척 |
| `SFX_Hit_08_Blooper_InkBlind.wav` | 먹물 실명 → Octoomba(먹물 연출) |
| `SFX_Hit_09_Goomba_Bump.wav` | Goomba |

## 4. 위험 요소
- `SfxType`은 끝에만 추가.
- 점프/이동은 입력마다 나므로 연타 시 동시 재생 수가 늘어난다. 풀 한도 24 + 가장 오래된 one-shot 빼앗기로 제한된다.
- `Player.cs`는 CP949다. 인코딩이 바뀌면 diff 전체가 바뀐다.
- 프리팹 수정은 `LoadPrefabContents` → `SaveAsPrefabAsset`.
