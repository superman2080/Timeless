# AudioPool — Plan

근거: `docs/AudioPool/Research.md`
상태: **구현 완료 (2026-11-28)**

## 구현 현황 요약
- 완료: Phase 1~7 전 항목 (26/26). 미완료 없음.
- 컴파일 에러 0, 플레이 검증 중 새 에러/경고 0 (기존 Missing Script·큐브맵 버전 경고만).
- 계획과 달라진 점
  - 씬 연결 eval에서 `OpenScene`이 참조 없는 에셋을 언로드해 `AudioPool.data`가 비는 문제 → 씬을 연 뒤 에셋을 로드하도록 재실행해 해결.
  - `NextBgm` 페이드인은 새 핸들이 실제로 재생을 시작한 뒤(로드 완료 후) 시작하도록 대기 루프 추가.
- 작업 전부터 있던 미커밋 변경 때문에 생기는 문제 (이번 작업에서 수정하지 않음, 확인 필요)
  - `SettingsMenu.prefab`의 `UIStack.pauseGame`이 0 (HEAD는 1). GameScene에 오버라이드가 없어 **GameScene에서 ESC를 눌러도 게임과 오디오가 정지하지 않는다.**
  - GameScene의 `SettingsMenu` 인스턴스가 `m_IsActive: 0`으로 오버라이드되어 있어, GameScene에서는 ESC 메뉴 자체가 동작하지 않는다.
  - 검증은 플레이 중에만 메뉴를 켜고 `pauseGame`을 true로 바꿔서 진행했다.
  - → 사용자 확인 결과 의도하지 않은 변경. 되돌렸다.
    - 프리팹 `pauseGame`/`allowExitToTitle`을 1로 되돌렸다.
    - Title에는 두 값을 다시 0으로 오버라이드했다(HEAD와 동일).
    - GameScene `SettingsMenu`의 비활성 오버라이드를 제거했다.

## 확정된 결정 (1차 피드백)
1. BGM 전환은 지금 방식 유지: 페이드아웃 → 페이드인 순차.
2. 동시 재생 최대 24개(`maxCount = 24`). 넘으면 가장 오래된 one-shot을 빼앗아 재사용(아래 "남은 의문점" 1).
3. 3D 위치 사운드 없음. 전부 2D, API에 위치 인자 없음.
4. ESC 정지 중에는 BGM 포함 **모든 오디오 일시정지**, 닫으면 이어서 재생.
5. 구버전 오디오 시스템(`AudioPlayer.cs`, `BgmChangeSystem.prefab`, Title `TitleAudio`)은 **삭제**하고 풀 방식으로 전면 교체.
6. 오디오를 쓰는 쪽은 `SoundManager`를 거치지 않고 **풀에서 직접 가져와 재생**한다: `(Pool<PooledAudio>.Instance as AudioPool).PlaySFX(type)`. 기존 `EffectPool` 호출 방식과 같다.

## 설계 요약

### 역할 분담
| 클래스 | 역할 |
|---|---|
| `PooledAudio` | `AudioSource` 1개 = 풀 단위. 로드 대기, 재생, 자동 반환, 일시정지 |
| `AudioPool` | 소스 꺼내기/반환, 동시 재생 한도, `SfxType` → 클립 조회, 전체 일시정지/재개 |
| `SoundManager` | 볼륨(믹서 + PlayerPrefs)과 BGM 순서(시작 BGM, `NextBgm` 순환·페이드)만. BGM 소스도 풀에서 받는다 |

### `PooledAudio` (`Assets/02. Scripts/Audio/PooledAudio.cs`, 신규)
- `[RequireComponent(typeof(AudioSource))]`. 소스는 `Source`로 노출.
- `Play(clip, group, volume, loop)`
  1. 클립 로드가 끝날 때까지 기다린다(기존 `SoundManager.PlayWhenLoaded` 로직 이동).
  2. `Play`한다. 이때 풀이 일시정지 상태면 바로 `Pause`해 둔다.
  3. `loop == false`면 재생이 끝나면 스스로 풀에 반환한다.
- 대기는 매 프레임 확인(`yield return null`) → `timeScale = 0`에서도 코루틴이 돈다.
- **일시정지 중 오반환 방지**: `Pause()` 상태에서는 `isPlaying`이 false가 되므로, `IsPaused` 플래그가 켜져 있는 동안은 반환 판정을 건너뛴다.
- `Pause()` / `Resume()`: `Source.Pause/UnPause` + 플래그.
- `loop == true` 핸들은 자동 반환하지 않는다. 소유자가 `Stop()`.
- `Stop()` = 풀에 반환. `OnDisable`에서 소스 `Stop` + 클립·코루틴·볼륨·pitch·플래그 초기화.
- `StartTime`(unscaled) 기록 → 빼앗기 대상 선정용.

### `AudioPool` (`Assets/02. Scripts/Pool/AudioPool.cs`, 신규, `Pool<PooledAudio>` 상속)
- 인스펙터
  - `prefab`: `PooledAudio.prefab`
  - `data`: `AudioDataSO` (SFX 클립 조회용. `SoundManager`에서 이동)
  - `sfxGroup`, `bgmGroup`: `AudioMixerGroup` (`SoundManager`에서 이동)
  - `prewarmCount` = 8, `maxCount` = 24
- `Awake`: `dontDestroyOnLoad = false`(SoundManager 오브젝트가 DDOL로 끌려가는 것 방지, Research §4).
- `Start`
  - `prewarmCount`개를 비활성으로 미리 생성.
  - `data`의 모든 클립(`sfx`, `bgmStart`, `bgmCycle`) `LoadAudioData` (`SoundManager.Start`에서 이동).
- 공개 API
  - `PlaySFX(SfxType type)` → `PooledAudio` 반환. `data`에서 클립·볼륨을 찾아 `sfxGroup`으로 재생. 없으면 경고 후 null.
  - `PlayBGM(AudioClip clip, float volume)` → `bgmGroup`, loop 재생. 핸들 반환.
  - `Play(clip, group, volume = 1, loop = false)` → 범용.
  - `TryGetSfx(SfxType, out SfxData)` (BGM 전환 효과음 조회용으로 `SoundManager`가 사용).
  - `PauseAll()` / `ResumeAll()` / `StopAll()`.
  - `IsPaused`: `PauseAll` 이후 새로 재생되는 소스도 일시정지 상태로 시작.
- 꺼내기(`Take`)
  1. 비활성 인스턴스
  2. 없으면 `maxCount` 미만일 때 새로 생성
  3. 그래도 없으면 가장 오래된 one-shot(loop 아님)을 빼앗아 재사용
  4. 전부 loop면 경고 + null
- `prefab`이 null이면(자동 생성된 빈 풀 등) 경고 후 null 반환 → 호출처는 `?.`로 안전하게 무시.

### `PooledAudio.prefab` (`Assets/07. Prefabs/Sound/PooledAudio.prefab`, 신규)
- `AudioSource`(playOnAwake off, spatialBlend 0 = 2D, loop off) + `PooledAudio`.

### `SoundManager` 축소
- 삭제: `bgmSource`, `sfxSource`, `CreateSource`, `PlayWhenLoaded`, `PlaySFX`, `TryGetSfx`, `bgmGroup`/`sfxGroup`/`data` 필드, 클립 일괄 로드.
- 유지: 볼륨(`mixer`, `Get/SetVolume`, PlayerPrefs, `OnValidate`), `fadeIn/fadeOut`, `NextBgm` 순환.
- BGM 클립은 풀의 `data`에서 가져온다(`pool.Data.bgmStart`, `bgmCycle`).
- `Start`: `bgmStart`가 있으면 `bgm = pool.PlayBGM(bgmStart, 1)`.
- `NextBgmCoroutine`
  1. 전환 효과음을 `bgm.Source.PlayOneShot` — 기존처럼 BGM 페이드아웃에 묻힘.
  2. `bgm` 볼륨 페이드아웃 → `bgm.Stop()`(반환).
  3. `bgm = pool.PlayBGM(next, 0)` → 로드 후 페이드인.
- 페이드는 기존대로 unscaled. 정지 중에는 HP가 변하지 않아 `NextBgm`이 호출되지 않는다.

### 호출처 교체 (전부 UTF-8 파일)
`SoundManager.Instance.PlaySFX(x)` → `(Pool<PooledAudio>.Instance as AudioPool)?.PlaySFX(x)`
- `Obstacles/Obstacle.cs:72, :90`
- `Obstacles/HealObject.cs:34`
- `Obstacles/PowerUpItem.cs:48`
- `Boss/States/BossIntroState.cs:69, :83, :89`
- `Player.cs:294`의 `SoundManager.Instance.NextBgm()`은 그대로 둔다(BGM 순서는 SoundManager 담당). `Player.cs`는 CP949라 수정하지 않음.
- `VolumeSettingsUI.cs`는 볼륨만 다루므로 그대로.

### ESC 정지 연동
- `UIStack.Pause(bool on)`의 `pauseGame` 블록 안에서 `AudioPool.PauseAll()` / `ResumeAll()` 호출.
  - Title은 `pauseGame = false`라 설정 메뉴를 열어도 BGM이 계속 난다(남은 의문점 2).
- 풀 접근은 `Pool<PooledAudio>.Instance`. 풀이 없는 씬에서 빈 오브젝트가 자동 생성되지만 `PauseAll`은 아무것도 만들지 않으므로 무해.

### Title
- 새 `Assets/11. Datas/AudioData_Title.asset`: `bgmStart` = `Lobby_music`, 나머지 비움.
- Title `SoundManager` 오브젝트에 `AudioPool` 추가, `data` = `AudioData_Title`, 믹서 그룹 연결.
- `TitleAudio` GameObject 삭제.

### 구버전 시스템 삭제
- `Assets/02. Scripts/Audio/AudioPlayer.cs` + `.meta` 삭제. 코드 참조 없음(Research §1).
- `Assets/07. Prefabs/Sound/BgmChangeSystem.prefab` + `.meta` 삭제.
  - Addressables `Prefabs_FX` 그룹의 `BgmChangeSystem` 엔트리 제거. 이 주소를 로드하는 코드는 없다.
  - `JHWTestScene.unity`의 프리팹 인스턴스(`m_SourcePrefab` 70f736d4…) 제거 후 씬 저장.
- 제외: Hovl/ParticlePack 벤더 프리팹의 `AudioSource`(게임 미사용, 벤더 에셋).

## Phase 1 — 풀 코드
- [x] `Audio/PooledAudio.cs` 작성(UTF-8): `Source`, `StartTime`, `IsLooping`, `IsPaused`, `Play`, `Pause/Resume`, `Stop`, 로드 대기 + 자동 반환 코루틴(일시정지 중 반환 판정 제외), `OnDisable` 초기화.
- [x] `Pool/AudioPool.cs` 작성(UTF-8): 필드(`prefab`/`data`/`sfxGroup`/`bgmGroup`/`prewarmCount`/`maxCount`), `Awake`(DDOL off), `Start`(prewarm + 클립 로드), `PlaySFX`/`PlayBGM`/`Play`/`TryGetSfx`, `Take`(빼앗기), `PauseAll`/`ResumeAll`/`StopAll`, prefab null 가드.
- [x] 컴파일 확인: `console_status` 에러 0.

## Phase 2 — 프리팹
- [x] `07. Prefabs/Sound/PooledAudio.prefab` 생성(eval): AudioSource(playOnAwake off, 2D) + PooledAudio. `.meta` 생성 확인.

## Phase 3 — SoundManager 축소 + 호출처 교체
- [x] `SoundManager.cs`: 위 "축소" 항목대로 정리, BGM을 풀 핸들(`bgm`)로 재생. 상단 주석 갱신.
- [x] `Obstacle.cs`, `HealObject.cs`, `PowerUpItem.cs`, `BossIntroState.cs`: `PlaySFX` 호출을 풀로 교체.
- [x] `UIStack.cs`: `Pause`에서 `PauseAll/ResumeAll` 호출.
- [x] 컴파일 + 콘솔 확인.

## Phase 4 — 씬 / 데이터
- [x] GameScene `SoundManager` 오브젝트: `AudioPool` 추가, `prefab`/`data`(`AudioData.asset`)/믹서 그룹 연결. SoundManager에서 빠진 필드 값이 AudioPool로 옮겨졌는지 확인. 씬 저장.
- [x] `11. Datas/AudioData_Title.asset` 생성(`bgmStart` = Lobby_music).
- [x] Title `SoundManager` 오브젝트: `AudioPool` 추가 + 연결(`data` = `AudioData_Title`). `TitleAudio` 삭제. 씬 저장.
- [x] Missing 참조 없음 확인.

## Phase 5 — 구버전 삭제
- [x] `JHWTestScene.unity`에서 `BgmChangeSystem` 인스턴스 제거, 저장.
- [x] Addressables `Prefabs_FX`에서 `BgmChangeSystem` 엔트리 제거(Addressables API로).
- [x] `BgmChangeSystem.prefab`, `AudioPlayer.cs` 삭제(`AssetDatabase.DeleteAsset`으로 `.meta`까지).
- [x] GUID 잔여 참조 grep 0건, 컴파일 에러 0.

## Phase 6 — MCP 플레이 검증
(`runInBackground` 등은 메모리의 플레이 검증 셋업을 따른다)
- [x] GameScene: 시작 BGM이 풀 인스턴스 1개(loop)로 재생.
- [x] 장애물 피격 → SFX 인스턴스 활성 → 재생 끝나면 비활성.
- [x] eval `Player.SwitchView`: 전환 효과음 + 페이드 후 새 BGM 핸들, 이전 핸들 반환.
- [x] eval로 `PlaySFX` 30회 연속: 인스턴스 ≤ 24, BGM은 빼앗기지 않음.
  - 결과: count=24, active=24(loop 1 = BGM), null 반환 0.
- [x] ESC: 모든 활성 소스 `IsPaused`, 정지 중 반환 안 됨. 닫으면 이어서 재생 + 이후 정상 반환.
  - 결과: 정지 7초 동안 BGM·SFX 모두 `IsPaused`, 반환 없음. 정지 중 새 요청도 일시정지 상태로 시작. 닫은 뒤 재개, SFX는 끝나고 반환.
  - 메모: 작업 전부터 있던 미커밋 변경(`SettingsMenu` 비활성, `pauseGame` 0) 때문에 런타임에서만 메뉴를 켜고 `pauseGame`을 true로 바꿔 검증.
- [x] Title: Lobby_music이 풀로 재생, BGM 볼륨 슬라이더 반영.
  - 결과: BGM 0.25 → 믹서 -12.0dB. 검증 후 원래 값으로 복원.
- [x] Title → GameScene: SoundManager/AudioPool이 DDOL로 남지 않음(중복 없음).
- [x] 콘솔 새 에러/경고 0 (기존 Missing Script, `Ray does not intersect` 제외).

## Phase 7 — 문서
- [x] `CLAUDE.md`
  - 오디오 구조 단락 추가: `AudioPool`(재생·SFX 조회·일시정지) / `PooledAudio` / `SoundManager`(볼륨·BGM 순서), 호출은 `(Pool<PooledAudio>.Instance as AudioPool)?.PlaySFX`, loop 핸들은 소유자가 `Stop`.
  - `AudioPlayer.trigger` 문장(92행), `TitleAudio` 문장(113행), 인코딩 예시의 `AudioPlayer.cs`(118행) 수정.
- [x] Plan.md 상단에 구현 현황 요약.

## 남은 의문점
1. **24개 초과 시 정책**: 기본안은 "가장 오래된 효과음을 끊고 새 소리 재생". 새 요청을 무시하는 쪽이 좋으면 알려 주세요.
2. **Title 설정 메뉴**: Title은 게임 정지가 없는 화면이라(`pauseGame = false`) 기본안은 BGM을 계속 재생. Title에서도 메뉴를 열면 BGM을 멈출까요?
3. **SFX 데이터 위치**: 기본안은 `AudioDataSO`·믹서 그룹을 `AudioPool`로 옮겨 풀이 `SfxType` 조회까지 담당. `SoundManager`는 볼륨과 BGM 순서만 남는다. 이 분담이면 괜찮을까요?
