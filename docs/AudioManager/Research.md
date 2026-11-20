# AudioManager + 쿠파 포효 — Research

작업 목표:
1. 쿠파가 착지 후 울부짖는(Screaming) 타이밍에 `kaiju_boss_roar_v2.wav`를 재생한다.
2. 모든 사운드의 크기를 조절할 수 있는 `AudioManager`를 만든다. 각 시스템은 이 매니저에서 오디오를 받아 재생한다.

## 1. 현재 오디오 사용처 (코드에서 확인)

| 위치 | 방식 | 클립 출처 | 볼륨 |
|---|---|---|---|
| `Audio/AudioPlayer.cs` (CP949) | BGM. 자기 `AudioSource`로 `Play`. `trigger`가 켜지면 `FadeOutIn`(전환 효과음 `audioClips[0]`을 `PlayOneShot`(0.7)한 뒤 페이드아웃 → 다음 BGM 페이드인) | 인스펙터 `audioClips[]` | `audioSource.volume`을 직접 0↔1로 조작 |
| `Obstacles/Obstacle.cs` | 피격 `hitSFX`, 튕김 `knockSFX` | 프리팹 필드 | `AudioSource.PlayClipAtPoint(clip, 플레이어 위치, 0.5f)` |
| `Obstacles/HealObject.cs` | `healSFX` | 프리팹 필드 | `PlayClipAtPoint(..., 0.5f)` |
| `Obstacles/PowerUpItem.cs` | `pickupSFX` | 프리팹 필드 | `PlayClipAtPoint(..., 0.5f)` |
| `Player.SwitchView` | `GameManager.Instance.audioPlayer.trigger = true` (2D/3D 전환 시 BGM 교체) | — | — |
| 벤더 이펙트 `HS_EffectSound` (Hovl) | 이펙트 프리팹 안의 `AudioSource.PlayOneShot` | 벤더 프리팹 | 개별 |

- `PlayClipAtPoint`는 매번 임시 GameObject를 만들고, **3D 사운드(spatialBlend 1)** 로 플레이어 위치에서 재생한다. 카메라(AudioListener)와의 거리에 따라 실제 크기가 달라진다.
- **AudioMixer 에셋이 프로젝트에 없다.** 모든 AudioSource의 output group은 null이다.

## 2. 씬 / 프리팹
- SampleScene `BgmChangeSystem`(프리팹 `Assets/07. Prefabs/Sound/BgmChangeSystem.prefab`)
  - `AudioSource`: loop, playOnAwake, vol 1, 2D.
  - `AudioPlayer` 클립: `[0] laser_hum`(전환 효과음), `[1] Fix-3d 1 add factory sound`(시작 BGM), `[2..5] Fix-2d 1 (only) / Fix-3d 2 / Fix-2d 1 (only) / Fix-3d 3`(전환마다 순환). `clipNumber` 2, fade 1/1.
  - `GameManager.audioPlayer`가 이 컴포넌트를 참조한다.
- Title 씬 `TitleAudio`: AudioSource가 있지만 **clip이 비어 있다**(loop, playOnAwake). 실제 소리는 나지 않는다.
- SFX 할당 현황(프리팹 17개):
  - 장애물 11종(`Gm_Obstacle_*`): `hitSFX = Attack_Sound`, `knockSFX`는 모두 비어 있음.
  - `HP`, `KEY_CARD`(HealObject): `keycard_pickUp`.
  - `MINI_MUSHROOM`, `P_SWITCH`, `SUPER_MUSHROOM`, `SUPER_STAR`(PowerUpItem): `keycard_pickUp`.

## 3. 쿠파 포효 타이밍
- `kaiju_boss_roar_v2.wav`: 5.0s, 스테레오 44.1kHz, DecompressOnLoad.
- 0.25s 단위 피크 진폭 엔벨로프:

  | 구간 | 피크 진폭 |
  |---|---|
  | 0 ~ 2.25s | 0.73 ~ 0.90 (큰 소리) |
  | 2.25 ~ 3.0s | 0.50 → 0.32 |
  | 3.0s 이후 | 0.16 이하로 감쇠 |

  → **시작부터 바로 크다.** Screaming 클립(2.8s)의 시작에 맞춰 트리거하면 길이도 거의 맞는다.
- 울부짖는 시점은 `BossIntroState`에서 `Scream` 트리거를 거는 순간이다. 착지 후 `landIdleTime`(0.3s)이 지난 뒤이고, 전이 블렌드는 0.25s다.
- 컷씬 중에는 `Time.timeScale = 0`이다. AudioSource 재생은 timeScale의 영향을 받지 않는다(pitch 미변경). 그래서 정지 중에도 재생된다.

## 4. 의존 관계와 영향 범위
- `Singleton<T>`: 기본 DDOL이다. 중복 인스턴스를 처리하지 않으므로, 씬마다 배치하면 **DDOL 중복**이 생길 수 있다. Title → SampleScene 전환이나 사망 시 SampleScene 재로드에서 중복된다.
  - `CameraManager`, `GameManager`는 SampleScene에만 있다. 그런데 SampleScene 재로드 때도 중복이 생기는지는 이 작업 범위 밖이다(기존 구조).
  - `AudioManager`를 Singleton으로 만들면 같은 문제가 생긴다. **중복 처리를 하거나 DDOL을 끄는 결정**이 필요하다.
- `AudioPlayer.FadeOutIn`이 `audioSource.volume`을 직접 0↔1로 설정한다. 그래서 "볼륨 배율"을 AudioSource.volume에 곱하는 방식은 페이드와 충돌한다. **AudioMixer 그룹 볼륨으로 조절하면 충돌하지 않는다.**
- 인코딩: `AudioPlayer.cs`, `Player.cs`는 CP949다. `Obstacle.cs`, `HealObject.cs`, `PowerUpItem.cs`는 UTF-8로 읽힌다.

## 5. 위험 요소
- 프리팹 17개의 `AudioClip` 필드를 다른 형식(enum 등)으로 바꾸면 기존 할당값이 사라진다. 에디터 스크립트로 일괄 이전해야 한다.
- 현재 SFX는 3D 공간 사운드라 거리 감쇠가 있다. 2D(spatialBlend 0)로 바꾸면 체감 크기가 달라진다(대체로 커진다). 기본 볼륨을 다시 맞춰야 할 수 있다.
- 벤더 이펙트(`HS_EffectSound`)의 소리는 매니저를 거치지 않는다. AudioSource의 output group을 SFX 그룹으로 지정하면 볼륨 조절에는 포함시킬 수 있다.
