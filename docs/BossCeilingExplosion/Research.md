# BossCeilingExplosion — Research

요청: 쿠파 첫 등장 컷씬에서 보스가 떨어질 때 **천장에 닿는 순간** 천장 위치에 `Explosion.prefab` 이펙트를 생성. 이펙트 루프는 끈다.

## 1. 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Boss/Boss.cs` | 보스 설정값(`dropHeight`, `fallGravity`, `introLeadTime`, `landEffect` 등) + FSM 보유 |
| `Assets/02. Scripts/Boss/States/BossIntroState.cs` | 첫 등장 컷씬. `Lead → Fall → Hold` 단계. 낙하·착지 이펙트 처리 |
| `Assets/02. Scripts/Datas/EffectDataSO.cs` | `EffectType` enum (`NONE, DUST_EXPLOSION, TINY_EXPLOSION, FIRE_BUFF`) |
| `Assets/02. Scripts/Pool/EffectPool.cs` | 타입별 이펙트 풀. `Get(type, pos[, rot])` |
| `Assets/02. Scripts/Effect/PooledEffect.cs` | 활성화 시 `Clear+Play`, `lifetime` 후 `WaitForSeconds`로 자기 비활성화 |
| `Assets/11. Datas/EffectData.asset` | `EffectType → 래퍼 프리팹` 매핑 (type 1~3 등록됨) |
| `Assets/07. Prefabs/Effect/FX_FireBuff.prefab` | 기존 래퍼 예시: Hovl 원본 `Fire buff.prefab` 프리팹 인스턴스 + `PooledEffect`(guid `8c583638...`) |
| `Assets/07. Prefabs/Effect/Hovl Studio/Epic Toon VFX 2/Prefabs/Explosion/Explosion.prefab` | 대상 이펙트 원본 (guid `9747e55c...`) |
| `Assets/07. Prefabs/Effect/Hovl Studio/HSFiles/Scripts/HS_EffectSound.cs` | Hovl 사운드 스크립트 (Explosion 루트에 부착) |
| `Assets/07. Prefabs/Boss/BossBowser.prefab` | 보스 프리팹. `dropHeight: 6`, `fallGravity: 40`, `introLeadTime: 0.4`, `groundY: 0`, `chaseZ3D: -14`, `chaseZ2D: -10.8` |

## 2. 현재 컷씬 흐름 (BossIntroState)

1. `Enter`: `timeScale = 0`, 보스를 `(플레이어 X, groundY + dropHeight, ChaseZ)`에 놓고 **바로 보이게** 함, `Idle` 재생, `vCam_BossIntro` 컷 전환.
2. `Lead`: `introLeadTime`(0.4s, unscaled) 동안 공중에 정지한 채로 보여 줌.
3. `Fall`: unscaled dt로 `fallSpeed += fallGravity*dt`, `pos.y -= fallSpeed*dt`. `pos.y <= groundY`가 되는 프레임에 `Land()`.
4. `Land`: `EffectPool.Get(landEffect=FIRE_BUFF, 발 위치)` + unscaled 카메라 쉐이크 → `Hold`.
5. `Hold`: Scream → Walk 블렌드 시작 시 `ChaseState`로, `Exit`에서 timeScale 복원.

- 보스 위치(`transform.position`)는 **발 기준**(착지 시 y = groundY).
- 낙하 시작 높이 6m는 `docs/BowserBoss/Plan.md`에 따르면 "복도 아치 천장(3.7~6.9m) 아래"로 의도적으로 둔 값. 즉 **현재 보스는 천장을 통과하지 않는다** — 천장 아래 공중에서 나타나 떨어짐.

## 3. Explosion.prefab 구조

- 루트 `Explosion`(localPos y=1) + 자식 11개: WaveBorders, Sparks, EndlDesbis, SideTrails, ColorCenter, DarkSmoke, Glow, Light, ShockWave, Flare, Shine.
- ParticleSystem 12개 전부 `looping: 1`, `lengthInSec: 2`, `useUnscaledTime: 0`, `playOnAwake: 1`.
- 루트에 `HS_EffectSound` (`Repeating: 1`, `RepeatTime: 2`, `StartTime: 0`) + AudioSource.
- 원본은 데모 씬(`Demo Toon VFX 2.unity`)에서만 참조됨. 게임 코드 참조 없음.

## 4. 위험 요소 (코드에서 확인)

- **timeScale 0**: 천장 통과는 컷씬(timeScale 0) 중에 일어난다. ParticleSystem 기본값은 scaled time → **재생 안 됨**. 기존 `Fire buff.prefab`은 원본의 `useUnscaledTime`을 19곳 켜서 해결했음(같은 방식 필요).
- **루프**: `looping: 1`이면 `PooledEffect`가 `lifetime` 후 끄기 전까지 계속 재방출. `looping: 0`으로 바꿔야 1회 재생. `PooledEffect.CalculateLifetime`은 `duration + startLifetime.constantMax`라 루프 off와 호환.
- **HS_EffectSound**: `Repeating: 1`이면 `InvokeRepeating`으로 2초마다 무한 재생(이것도 "루프"). 또 `Start`에서만 시작하므로 풀링 재사용 시 동작이 일정하지 않고, `Invoke`는 scaled time이라 timeScale 0 중엔 첫 재생도 지연됨. `SoundManager`의 볼륨 설정도 거치지 않음. 기존 `Fire buff.prefab`에도 같은 컴포넌트가 1개 있음.
- **PooledEffect 비활성화 타이머**는 `WaitForSeconds`(scaled) → 정지 중엔 안 꺼짐. 컷씬 재개 후 꺼지므로 실사용 문제는 없음(FireBuff와 동일).
- **천장 높이**: 복도(트랙)는 런타임에 생성되어 에디트 모드 씬에 없음. 에디트 모드 레이캐스트 결과 히트 0. 아치라서 X에 따라 높이가 다름(3.7~6.9m). 레인 X 근처의 정확한 천장 Y는 플레이 중 측정 필요. 복도 메시에 콜라이더가 있는지도 미확인.
- **카메라**: `vCam_BossIntro`(1.6, 1.0, -6.5)가 LookAt 쿠파. 낙하 시작점을 천장 위로 올리면 Lead 동안 카메라가 천장 위(복도 밖)를 올려다봐 천장 메시가 보일 수 있음 — 플레이 확인 필요.
- 현재 콘솔: 컴파일 실패 없음, 에러 0.
