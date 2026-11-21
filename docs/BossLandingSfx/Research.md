# BossLandingSfx — Research

요청: 쿠파가 착지할 때 `Assets/08. Audio/Effect Audio/monster_landing_v2.wav`를 재생한다.

## 1. 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Boss/States/BossIntroState.cs` | `Fall`에서 `pos.y <= groundY`가 되는 프레임에 `Land(pos)`를 호출한다. `Land`는 `landEffect`(FIRE_BUFF) 이펙트를 내고 unscaled 쉐이크를 건 뒤 `Hold`로 넘어간다 |
| `Assets/02. Scripts/Boss/Boss.cs` | 컷씬 설정값. `landEffect`, `roarSfx`(BOSS_ROAR), `ceilingSfx`(BOSS_CEILING_BREAK) 필드가 있다 |
| `Assets/02. Scripts/Datas/AudioDataSO.cs` | `SfxType`: `NONE, HIT, PICKUP, BGM_TRANSITION, BOSS_ROAR, BOSS_CEILING_BREAK` |
| `Assets/02. Scripts/Audio/SoundManager.cs` | `PlaySFX(SfxType)`. `Start`에서 `AudioData`에 등록된 클립을 전부 `LoadAudioData()`한다 |
| `Assets/11. Datas/AudioData.asset` | sfx type 1~5가 등록돼 있다 |
| `Assets/07. Prefabs/Boss/BossBowser.prefab` | 보스 설정값을 직렬화한다 |
| `Assets/08. Audio/Effect Audio/monster_landing_v2.wav` | 대상 클립. guid `c0958a23c38a3b6469623e387c5c75f1`, `preloadAudioData: 0`. **아직 git에 추가되지 않은(untracked) 새 파일** |

## 2. 확인한 동작

- 착지 처리는 `BossIntroState.Land` 한 곳뿐이다. 두 번째 등장부터 쓰는 `BossEnterState`는 뒤에서 달려 들어오는 방식이라 착지가 없다. 따라서 착지 효과음은 **첫 등장 컷씬에서 1회** 재생된다.
- 직전 작업(`docs/BossCeilingSfx`)과 같은 구조다. `SfxType`을 추가하고 `AudioData`에 등록한 뒤 `PlaySFX`를 호출한다. timeScale 0 상태에서도 재생되는 것을 이미 확인했다.
- `Land`가 실행되는 프레임은 착지 이펙트·쉐이크와 같다. 그래서 소리 타이밍도 정확히 맞는다.

## 3. 위험 요소

- `SfxType`은 반드시 끝에 추가해야 한다(정수 직렬화).
- 천장 효과음(5초 클립)은 낙하 중에 시작된다. 그래서 착지 시점에는 천장 효과음이 아직 재생 중이고, 착지 효과음과 겹친다. 두 소리는 `PlayOneShot`으로 함께 재생되며, 서로 끊기지 않는다.
