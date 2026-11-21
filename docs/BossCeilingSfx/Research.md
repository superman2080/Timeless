# BossCeilingSfx — Research

요청: 쿠파가 천장을 깨고 내려올 때(천장 폭발 이펙트 생성 시점) `Assets/08. Audio/Effect Audio/building_explosion_v3.wav`를 재생한다.

## 1. 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Boss/States/BossIntroState.cs` | `Fall`에서 천장 교차 프레임에 `SpawnCeilingEffect` 호출 (직전 작업 `docs/BossCeilingExplosion`) |
| `Assets/02. Scripts/Boss/Boss.cs` | 컷씬 설정값. 이미 `roarSfx = SfxType.BOSS_ROAR` 필드 패턴이 있음 |
| `Assets/02. Scripts/Datas/AudioDataSO.cs` | `SfxType` enum: `NONE, HIT, PICKUP, BGM_TRANSITION, BOSS_ROAR` + `SfxData{type, clip, volume}` |
| `Assets/02. Scripts/Audio/SoundManager.cs` | `PlaySFX(SfxType)` → `AudioData`에서 클립 찾아 `sfxSource.PlayOneShot(clip, volume)`. SFX 믹서 그룹 경유(볼륨 설정 반영) |
| `Assets/11. Datas/AudioData.asset` | `sfx` 목록: type 1~4 등록(볼륨 0.5/0.5/0.7/1) |
| `Assets/08. Audio/Effect Audio/building_explosion_v3.wav` | 대상 클립. guid `be8469cfa2ccf4c428f5eb6534fd8fb5`. `preloadAudioData: 0`. 다른 에셋에서 참조 없음 |

## 2. 확인한 동작

- 효과음은 각 시스템이 클립 대신 `SfxType`으로 요청하는 구조다. 보스 울부짖음도 `SoundManager.Instance.PlaySFX(boss.roarSfx)`로 재생한다.
- `preloadAudioData: 0` 클립은 로드 전 `PlayOneShot`이 무음이 된다. 하지만 `SoundManager.Start`가 `data.sfx`의 모든 클립에 `LoadAudioData()`를 호출하므로, `AudioData.asset`에 등록만 하면 해결된다.
- 컷씬은 timeScale 0이다. AudioSource 재생은 timeScale의 영향을 받지 않는다. 같은 컷씬의 `BOSS_ROAR`도 정지 중에 정상 재생된다.
- 직전 작업에서 Explosion 원본 사운드(`HS_EffectSound`)는 껐다. 그래서 소리가 겹치지 않는다.

## 3. 위험 요소

- `SfxType`은 정수로 직렬화된다. 반드시 **끝에** 추가해야 기존 에셋의 값이 밀리지 않는다.
- 이 시점의 소리는 이후 착지 쉐이크나 울부짖음과 겹칠 수 있다. 낙하 시간은 9m → 6.3m 구간 기준 약 0.37초이고, 그 뒤 착지한다. 볼륨은 실측해서 조정한다.
