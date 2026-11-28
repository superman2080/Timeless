# AudioPool — Research

목표: `AudioSource` 풀을 만들고, 지금 재생되는 모든 오디오(BGM/SFX/타이틀 BGM)가 이 풀을 거쳐 재생·관리되도록 한다.

## 1. 현재 오디오 재생 경로

| 위치 | 재생 방식 | 출력 그룹 | 비고 |
|---|---|---|---|
| `Audio/SoundManager.cs` `PlaySFX(SfxType)` | `Awake`에서 만든 SFX 전용 `AudioSource` 1개로 `PlayOneShot(clip, volume)` | `sfxGroup` | 모든 효과음이 이 소스 하나를 공유한다. 개별 정지·pitch·위치를 지정할 수 없다 |
| `SoundManager` BGM (`Start`의 `bgmStart`, `NextBgm()`) | BGM 전용 `AudioSource` 1개(`loop = true`). `PlayWhenLoaded`로 로드가 끝난 뒤 `Play` | `bgmGroup` | 전환 효과음 `BGM_TRANSITION`은 **BGM 소스**에서 `PlayOneShot`해서 페이드아웃에 같이 묻히게 한다(`SoundManager.cs:178`). 페이드는 `AudioSource.volume`을 unscaled time 기준으로 조작한다 |
| Title 씬 `TitleAudio` GameObject (`Title.unity:500`) | 씬에 직접 둔 `AudioSource`. `m_Resource` = `Assets/08. Audio/Title_Bgm/Lobby_music.mp3`, `playOnAwake`, `Loop` | BGM 그룹(`a8041db…` MainMixer) | 스크립트 없이 재생된다. 이전 Research에 "clip 비어 있음"이라고 적혀 있었지만, Unity 6에서는 클립이 `m_audioClip`이 아니라 `m_Resource`에 들어 있어 실제로는 재생된다 |
| `Audio/AudioPlayer.cs` (CP949) + `07. Prefabs/Sound/BgmChangeSystem.prefab` | 구버전 BGM 시스템 | — | 게임 코드는 이제 쓰지 않는다. 참조하는 곳은 `JHWTestScene.unity`(개인 테스트 씬)와 Addressables `Prefabs_FX` 그룹뿐이다. 지난 작업(`docs/AudioManager`)에서 삭제를 보류했다 |

### SFX 호출처 (전부 `SoundManager.Instance.PlaySFX(SfxType)`)
- `Obstacles/Obstacle.cs:72`(hitSfx), `:90`(knockSfx)
- `Obstacles/HealObject.cs:34`
- `Obstacles/PowerUpItem.cs:48`
- `Boss/States/BossIntroState.cs:69`(roar), `:83`(ceiling), `:89`(landing)
- BGM: `Player/Player.cs:294` `SoundManager.Instance.NextBgm()`
- 볼륨: `UI/VolumeSettingsUI.cs`의 `Get/SetVolume`(믹서만 다루고 소스와는 무관)

호출처는 모두 반환값을 쓰지 않는다. 시그니처를 유지하면 호출처를 고칠 필요가 없다.

### 다른 AudioSource
- `EffectData.asset`이 쓰는 이펙트 프리팹 4개(`FX_TinyExplosion`, `FX_FireBuff`, `FX_DustExplosion`, `FX_Explosion`)에는 `AudioSource`가 **없다**.
- Hovl Studio 벤더 프리팹과 ParticlePack `Player.prefab`에는 AudioSource가 있지만 게임에서 쓰지 않는다. 범위 밖이다(지난 작업에서도 제외).

## 2. 씬 배치 현황
- `SoundManager`가 GameScene과 Title에 각각 직접 배치되어 있다(프리팹 아님).
  - 두 씬 모두 `mixer`, `bgmGroup`, `sfxGroup`이 연결되어 있다.
  - `data`: GameScene = `AudioData.asset`, Title = 비어 있음(볼륨 반영만 한다).
- `SoundManager.Awake`에서 `dontDestroyOnLoad = false`. 씬이 바뀌거나 재로드되면 같이 파괴되고 새로 만들어진다.

## 3. 기존 풀 구조 (재사용 대상)
- `Pool/Pool.cs`: `Pool<T> : Singleton<Pool<T>>`. `Awake`에서 미리 배치된 자식을 수집한다. `Create(prefab)`로 자식을 생성하고 `items`에 등록한다. `Return`은 transform을 초기화하고 `SetActive(false)`. `Prefab`은 추상 프로퍼티다.
- `Pool/EffectPool.cs`: `Pool<PooledEffect>` 상속. 데이터 SO, 타입별 리스트, `Start`에서 prewarm, `Take`로 비활성 상태로 꺼낸 뒤 위치를 정하고 활성화한다.
- `Effect/PooledEffect.cs`: `OnEnable`에서 재생하고 수명 코루틴으로 스스로 `SetActive(false)` → 풀로 복귀한다.
- 접근 방식: `(Pool<PooledEffect>.Instance as EffectPool)`.

## 3-1. ESC 정지 (`UI/UIStack.cs`)
- 첫 `Push`에서 `Pause(true)`, 스택이 비면 `Pause(false)`. `pauseGame`일 때만 `timeScale`을 0/복원한다(Title은 `pauseGame = false`).
- 오디오는 건드리지 않는다 → 정지 중에도 BGM/SFX가 계속 난다.
- `AudioSource.Pause()` 중에는 `isPlaying == false`. 자동 반환을 `isPlaying`으로 판정하면 일시정지된 소스가 반환되어 버린다.

## 3-2. 구버전 시스템 참조 (GUID 검색)
- `BgmChangeSystem.prefab`(guid `70f736d4…`): `JHWTestScene.unity`의 프리팹 인스턴스(`m_SourcePrefab`), Addressables `Prefabs_FX.asset` 엔트리(주소 `BgmChangeSystem`, 라벨 `fx`). 이 주소를 로드하는 코드는 없다.
- `AudioPlayer.cs`(guid `b76e6f69…`): 위 프리팹 안에서만 쓰인다. C# 코드 참조 없음.
- `CLAUDE.md` 92·113·118행이 `AudioPlayer`/`TitleAudio`를 언급한다.

## 3-3. 호출처 파일 인코딩
- UTF-8: `Obstacle.cs`, `HealObject.cs`, `PowerUpItem.cs`, `BossIntroState.cs`, `UIStack.cs`.
- CP949: `Player.cs` (`NextBgm` 호출만 있음 — 수정 대상 아님).

## 4. 위험 요소 / 제약
- **DDOL 연쇄**: `Singleton.Awake`는 `dontDestroyOnLoad`가 true면 **루트 GameObject**를 DDOL로 만든다. AudioPool을 SoundManager 오브젝트(또는 자식)에 두면서 기본값 true를 그대로 두면, SoundManager까지 DDOL이 되어 씬마다 중복된다. AudioPool도 `Awake`에서 `dontDestroyOnLoad = false`로 해야 한다(SoundManager와 같은 방식).
- **자동 생성**: 씬에 `AudioPool`이 없을 때 `Pool<PooledAudio>.Instance`에 접근하면 빈 GameObject가 자동 생성된다. 이때 `Prefab`이 null이면 `Create`가 실패한다 → 프리팹 필드를 연결하거나 프리팹 없이도 동작하는 장치가 필요하다.
- **정적 instance**: `Singleton<T>.instance`는 정적이다. 씬이 파괴되면 Unity null 비교로 다시 `Find`하므로 재로드해도 문제없다(SoundManager도 같은 방식으로 동작 중).
- **클립 로드**: 모든 클립이 `preloadAudioData=false`다. 로드 전에 `Play`하면 무음이 된다. SoundManager가 `Start`에서 일괄 `LoadAudioData`하고, BGM은 `PlayWhenLoaded`로 로드 완료를 기다린다. 이 동작을 유지해야 한다.
- **timeScale 0**: ESC 메뉴는 `timeScale = 0`이다. AudioSource는 timeScale의 영향을 받지 않고, 현재는 정지 중에도 소리가 계속 난다. 풀의 자동 반환 대기는 `WaitForSeconds`(scaled) 대신 매 프레임 `isPlaying`을 확인하거나 unscaled time을 써야 정지 중에 반환이 멈추지 않는다.
- **동시 재생 수**: `PlayOneShot`은 소스 하나에서 무제한으로 겹친다. 풀로 바꾸면 소스 수만큼만 동시 재생된다 → 최대 개수와 초과 시 정책이 필요하다. `ProjectSettings/AudioManager.asset`에서 `m_RealVoiceCount: 32`, `m_VirtualVoiceCount: 512`로 확인.
- **인코딩**: `SoundManager.cs`, `AudioDataSO.cs`는 UTF-8. `AudioPlayer.cs`, `Player.cs`는 CP949(이번 작업에서 수정하지 않음).
- **씬 YAML**: Title의 `TitleAudio` 삭제와 SoundManager 필드 변경은 씬 파일 diff를 만든다. MCP/에디터 API로 편집하고 저장해야 한다.
