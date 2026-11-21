# AudioManager + 쿠파 포효 — Plan

근거: `docs/AudioManager/Research.md`
상태: **구현 완료** (2026-11-20)

## 구현 현황
- **완료**: Phase 1~6 전 항목. 단, Phase 2의 `AudioPlayer.cs` / `BgmChangeSystem.prefab` 삭제는 **보류**했다(아래 메모).
- 컴파일 에러 0. 플레이 중 새 에러/경고 0이며, 기존 Missing Script와 MCP 타임아웃 로그만 남아 있다.
- 계획과 달라진 점:
  - **클래스명을 `SoundManager`로 바꿨다.** `AudioManager.cs`라는 파일명은 Unity 내장 AudioManager(`ProjectSettings/AudioManager.asset`)와 겹쳐 컴파일 대상(rsp)에서 아예 빠진다. 파일명만 바꾸면 컴파일되는 것으로 확인했다. MonoBehaviour는 파일명과 클래스명이 같아야 붙일 수 있어 둘 다 `SoundManager`로 맞췄다.
  - 믹서는 에디터 내부 API(리플렉션)로 자동 생성했다. 사용자 작업은 필요 없었다.
  - 모든 클립이 `preloadAudioData=false`라서 로드 전 `Play()`는 무음이었다. 시작 BGM이 실제로 재생되지 않는 문제였다. 그래서 시작할 때 전체 클립을 `LoadAudioData`하고, BGM은 로드 완료 후 재생한다.
  - 기존 BGM 전환은 정지 상태에서 볼륨을 올린 뒤 재생하는 구조였다(실제로는 페이드인이 아님). 이를 **재생 후 실제 페이드인**으로 바꿨다. 페이드는 unscaled time을 쓴다.
  - ESC 입력은 `Keyboard.current`가 아니라 `InputAction("<Keyboard>/escape")`으로 받는다. 모든 키보드에 바인딩된다.
  - UI 텍스트는 영문이다(프로젝트에 한글 TMP 폰트가 없음).

## 확정된 결정 (피드백 1차)
- 1, 2, 4, 6번은 기본안대로 한다. AudioMixer, Master/BGM/SFX, SFX는 2D로 통일, 벤더 이펙트 사운드는 범위 밖.
- 3번 **볼륨 설정 UI를 만든다.** ESC로 열고 닫으며, 슬라이더 3개를 둔다. 열려 있는 동안 게임을 정지하고, 값은 PlayerPrefs에 저장한다.
- 5번 AudioManager는 **SampleScene에만** 두고 DDOL을 끈다.

## 설계 요약
- **볼륨 조절: AudioMixer**
  - `Assets/08. Audio/MainMixer.mixer`를 만든다. 그룹은 `Master` → `BGM`, `SFX` 두 개다.
  - 그룹마다 볼륨 파라미터를 노출한다(`MasterVolume`, `BGMVolume`, `SFXVolume`).
  - `AudioManager`가 0~1 값을 dB로 변환해 반영한다(`20·log10(v)`, 0이면 -80dB).
  - 이렇게 하면 기존 BGM 페이드(AudioSource.volume 직접 조작)와 충돌하지 않는다.
- **클립 공급: `AudioDataSO` + `SfxType` enum**
  - 기존 `EffectType`/`EffectDataSO`와 같은 구조로, 타입 → 클립 + 기본 볼륨을 매핑한다.
  - 에셋: `Assets/11. Datas/AudioData.asset`.
  - 각 시스템은 클립을 직접 들지 않고 `AudioManager.Instance.PlaySFX(SfxType.HIT)`처럼 **타입으로 요청**한다.
  - `SfxType` 초기값: `NONE`, `HIT`(Attack_Sound), `PICKUP`(keycard_pickUp), `BGM_TRANSITION`(laser_hum), `BOSS_ROAR`(kaiju_boss_roar_v2).
- **AudioManager** (`Assets/02. Scripts/Audio/AudioManager.cs`, `Singleton<AudioManager>`)
  - 인스펙터: `masterVolume`, `bgmVolume`, `sfxVolume`(0~1 슬라이더, 플레이 중 변경 즉시 반영), `mixer`, `data`(AudioDataSO).
  - SFX: SFX 그룹으로 출력하는 2D AudioSource 하나로 `PlayOneShot(clip, 기본볼륨)`을 호출한다. 동시 재생도 가능하다.
  - BGM: BGM 그룹으로 출력하는 AudioSource를 쓴다. 기존 `AudioPlayer`의 시작 BGM, 전환 효과음, 페이드아웃/인, 순환 로직을 **AudioManager로 옮긴다** (`NextBgm()`). BGM 클립 목록도 `AudioDataSO`에 둔다.
  - `Player.SwitchView`의 `audioPlayer.trigger = true`를 `AudioManager.Instance.NextBgm()`으로 바꾼다.
- **쿠파 포효**: `BossIntroState`에서 `Scream` 트리거를 거는 순간 `PlaySFX(boss.roarSfx)`를 호출한다(기본 `BOSS_ROAR`).
  - 이 순간은 Screaming 애니 시작과 같고, 정지 중에도 재생된다.

## Phase 1 — 데이터 / 믹서
- [x] `Assets/02. Scripts/Datas/AudioDataSO.cs`
  - `enum SfxType`.
  - `struct SfxData { type, clip, volume(0~1, 기본 1) }`.
  - `AudioDataSO { SfxData[] sfx; AudioClip bgmStart; AudioClip[] bgmCycle; }`.
- [x] 믹서 `Assets/08. Audio/MainMixer.mixer`: Master/BGM/SFX 그룹, 볼륨 파라미터 3개 노출.
  - 메모: 내부 `AudioMixerController` API(리플렉션)로 생성했다. 노출 파라미터 `MasterVolume`/`BGMVolume`/`SFXVolume` 확인.
- [x] `Assets/11. Datas/AudioData.asset`을 생성하고, 현재 프리팹/`AudioPlayer`의 클립 값을 그대로 채운다.
  - `bgmStart` = Fix-3d 1.
  - `bgmCycle` = [2..5].
  - `BGM_TRANSITION` = laser_hum, 기존 볼륨 0.7.
  - `HIT`, `PICKUP`은 기존 0.5.

## Phase 2 — AudioManager
- [x] `AudioManager.cs`: 볼륨 반영(`OnValidate` + `Start`), `PlaySFX(SfxType)`, `NextBgm()`(기존 FadeOutIn 동작 유지).
- [x] SampleScene에 `AudioManager`를 배치한다. `BgmChangeSystem`은 삭제하거나 AudioManager로 대체한다.
- [x] `GameManager.audioPlayer` 필드를 제거하고 `Player.SwitchView`를 수정한다(Player.cs는 CP949라 바이트 단위로 편집).
- [ ] `AudioPlayer.cs`와 `BgmChangeSystem.prefab`을 삭제한다(사용처가 없어진 뒤).
  - 메모(보류): SampleScene에서는 제거했다. 하지만 프리팹을 **`JHWTestScene.unity`(개인 테스트 씬)과 Addressables `Prefabs_FX` 그룹**이 아직 참조하고 있어 삭제하지 않았다. 해당 씬 소유자가 확인한 뒤 삭제하면 된다. `GameManager`의 참조는 제거해서 게임 코드는 더 이상 쓰지 않는다.

## Phase 3 — 기존 SFX 이전
- [x] `Obstacle`, `HealObject`, `PowerUpItem`의 `AudioClip` 필드를 `SfxType` 필드로 교체하고, 재생을 `AudioManager.Instance.PlaySFX`로 바꾼다.
- [x] 에디터 스크립트로 프리팹 17개의 값을 이전한다. `Attack_Sound` → `HIT`, `keycard_pickUp` → `PICKUP`, 빈 값 → `NONE`.
  - 메모: 기존 할당값이 종류별로 모두 같아서 필드 기본값(`hitSfx=HIT`, `knockSfx=NONE`, `healSfx=PICKUP`, `pickupSfx=PICKUP`)으로 이전했다. 프리팹을 로드해 장애물 11 HIT, 회복 2 PICKUP, 파워업 4 PICKUP인 것을 확인했다.

## Phase 4 — 쿠파 포효
- [x] `Boss.cs`에 `roarSfx`(기본 `BOSS_ROAR`)를 추가하고, `BossIntroState`의 Scream 트리거 시점에 재생한다.

## Phase 5 — 볼륨 설정 UI
- [x] `Assets/02. Scripts/UI/VolumeSettingsUI.cs`: ESC로 패널을 토글한다. 열면 timeScale 0, 닫으면 원래 값으로 복원한다. 컷씬 등으로 이미 정지 중이면 열지 않는다. 슬라이더 3개는 `AudioManager.SetVolume`에 연결한다.
- [x] `AudioManager`: 볼륨을 PlayerPrefs에 저장하고 불러온다.
- [x] SampleScene Canvas에 패널을 만든다. 제목, Master/BGM/SFX 라벨과 슬라이더, 닫기 버튼을 둔다. 기본 상태는 비활성이다.

## Phase 6 — 검증
- [x] 컴파일과 콘솔 확인.
- [x] 프리팹 참조(이전 값)가 맞는지 확인한다.
- [x] 플레이 확인: BGM 시작, 2D/3D 전환 시 BGM 교체, 피격/아이템 SFX, 컷씬 포효(정지 중 재생).
  - 결과(AudioSource 출력 RMS로 측정):
    - BGM 시작 출력 0.15~0.22.
    - 전환 시 클립 교체와 페이드인(vol 0.16→1.0).
    - HIT 출력 0.197.
    - 컷씬 Screaming 시점(timeScale 0)에 포효 출력 0.241.
- [x] 볼륨 슬라이더(Master/BGM/SFX)를 플레이 중에 바꿔 믹서 값이 반영되는지 확인한다.
  - 결과: UI를 열면 timeScale 0, BGM 슬라이더 0.5 → 믹서 -6.02dB, PlayerPrefs 0.5를 확인했다. 닫으면 timeScale 1로 복원된다(테스트 후 값은 1로 되돌림). 캡처로 패널 표시도 확인했다.
  - 메모: 에디터 창에 OS 포커스가 없어 Input System이 입력을 받지 않았다. 그래서 시뮬레이션 ESC 키로 직접 열지는 못했다(Space 점프도 동일하게 미반응). 대신 ESC 액션이 `/Keyboard/escape`에 바인딩되어 활성 상태인 것과, 토글 로직이 동작하는 것을 확인했다.

---

## (해결됨) 확인이 필요했던 점
1. **볼륨 조절 수단**: 기본안은 AudioMixer다. 믹서를 만들지 않고 코드에서 배율만 곱하는 방식도 가능하다. 다만 이 경우 BGM 페이드 코드도 배율을 반영하도록 고쳐야 한다.
2. **볼륨 카테고리**: 기본안은 Master/BGM/SFX 3개다. 환경음(Ambience) 등 더 나눌지 알려 달라.
3. **볼륨 저장**: 기본안은 인스펙터 조절만 하는 것이다. 설정 UI나 PlayerPrefs 저장이 필요하면 추가한다.
4. **SFX 2D/3D**: 기본안은 2D(거리 감쇠 없음)로 통일하는 것이다. 지금은 3D(PlayClipAtPoint)라 체감 크기가 달라질 수 있다.
5. **AudioManager 수명**: 기본안은 SampleScene에만 두고 DDOL을 끄는 것이다(사망 재로드 시 BGM도 처음부터, 기존과 동일). Title 씬 BGM까지 관리해야 하면 DDOL과 중복 제거가 필요하다(Title의 `TitleAudio`는 현재 clip이 비어 있다).
6. **벤더 이펙트 사운드**(`HS_EffectSound`): 기본안은 범위 밖으로 두는 것이다. 원하면 이펙트 프리팹 AudioSource의 출력 그룹만 SFX로 지정해 볼륨 조절에 포함한다.
