# ESC 설정 메뉴 (타이틀로 나가기 / 볼륨 / 키 매핑) — Research

요청: ESC를 누르면 설정 UI를 연다. 창은 2개로 나눈다.
- 창 1: 타이틀 씬으로 나가기 + 볼륨 조절
- 창 2: 키 매핑

## 1. 이미 있는 것: `VolumeSettingsUI` (ESC 볼륨 패널)

`Assets/02. Scripts/UI/VolumeSettingsUI.cs` (UTF-8). AudioManager 작업(`docs/AudioManager/`)에서 만들었다.
- ESC 입력: `new InputAction("ToggleVolumeSettings", binding: "<Keyboard>/escape")`를 `OnEnable`에서 만들고 `performed`에 `Toggle()`을 연결한다. `Keyboard.current`를 쓰지 않는 이유가 주석으로 남아 있다(마지막에 쓴 키보드만 보기 때문).
- `Open()`
  - `Time.timeScale == 0`이면 열지 않는다. 컷씬(`BossIntroState`)이나 크기 변경 연출(`Player.ResizeCoroutine`)이 이미 정지시킨 상태와 timeScale 복원이 충돌하지 않게 하려는 것이다.
  - 슬라이더 3개를 `SoundManager.GetVolume`으로 맞춘다(`SetValueWithoutNotify`).
  - `prevTimeScale`을 저장하고 `Time.timeScale = 0`으로 만든 뒤 `panel.SetActive(true)`.
- `Close()`: 패널을 끄고 timeScale을 복원한 뒤 `PlayerPrefs.Save()`.
- 슬라이더는 `onValueChanged`에서 `SoundManager.Instance.SetVolume(channel, v)`를 호출한다. 저장(PlayerPrefs `Volume.<채널>`)은 SoundManager가 한다.

씬 배치(에디터에서 `GameScene.unity` 계층을 덤프해 확인):
```
Canvas [Canvas, CanvasScaler, GraphicRaycaster]  (1920x1080)
  OctoombaScreenEffect / HPBar / Score / BulletBillWarning
  VolumeSettings [VolumeSettingsUI]
    Panel (off) [Image]                    ← 화면 전체 딤
      Box [Image] 520x340
        VOLUMELabel (TMP "VOLUME")
        MasterLabel / MasterSlider
        BGMLabel / BGMSlider
        SFXLabel / SFXSlider
        CloseButton [Button] + Text(TMP "Close (ESC)")
EventSystem
```
- 모든 TMP 텍스트의 폰트는 `LiberationSans SDF`다. 한글 TMP 폰트가 없어서 UI 텍스트는 영문이다(AudioManager Plan의 메모와 같다).

## 2. 씬 구성 / 씬 전환

- 빌드 설정(`ProjectSettings/EditorBuildSettings.asset`)에는 `Assets/01. Scenes/Title.unity` **하나만** 있다.
- 게임 씬의 실제 파일은 `Assets/01. Scenes/GameScene.unity`다(GUID `99c9720a…`). Addressables `Scenes` 그룹에 주소 **`SampleScene`**으로 등록되어 있다. CLAUDE.md의 "SampleScene"은 이 씬을 가리킨다.
- 타이틀 → 게임: `StartButton.StartLoading()`이 `Addressables.LoadSceneAsync(sceneToLoad)`를 호출한다. Title 씬 `SceneChanger` 오브젝트의 `sceneToLoad`는 `SampleScene`이다.
- 사망 → 재시작: `Player.Restart()`(`Player.cs:374`)가 다음 순서로 처리한다.
  - `CameraManager.SetGlitch(0.02f)`, `SetPixelateIntensity(1)`
  - `Addressables.LoadSceneAsync("SampleScene")`
- 게임 → 타이틀로 가는 코드는 **아직 없다**. Title은 Addressables에 등록되어 있지 않고 빌드 설정에만 있으므로 `SceneManager.LoadScene("Title")`로 로드할 수 있다.
  - Addressables로 로드한 씬을 `SceneManager`로 교체하면 Addressables 씬 핸들이 해제되지 않을 수 있다. **확인 필요** (§6 위험 요소).

### 싱글톤과 씬 전환
- `GameScene` 안의 싱글톤(GameManager, CameraManager, SoundManager, InteractionObjectPool, EffectPool)은 모두 씬에 배치되어 있다. 직렬화된 값은 `dontDestroyOnLoad: 0`이다.
  - 따라서 씬을 나가면 함께 파괴되고, 타이틀에 남는 객체는 없다.
- 단, `Singleton.Instance`는 인스턴스가 없으면 **새로 만든다**. 이렇게 자동 생성된 객체는 기본값 `dontDestroyOnLoad = true`라서 DDOL이 된다.
  - 씬 전환 중에 `Instance`에 접근하는 코드가 생기지 않도록 주의해야 한다.

### 렌더러 에셋 상태
- 글리치(`_Alpha`)와 픽셀화(`pixelScale`)는 **에셋을 직접 수정**하는 방식이다.
- `CameraManager.OnApplicationQuit`은 `SetGlitch(0.002f)`, `SetPixelateIntensity(1)`로 되돌린다.
- 2D 모드에서 타이틀로 나가면 픽셀화 값이 남은 채 타이틀이 렌더된다. 나가기 전에 리셋해야 한다(Restart와 같은 처리).

## 3. 입력 시스템

- `Assets/02. Scripts/Input/PlayerInput.inputactions`(자동 생성 래퍼 `PlayerInput.cs`, 직접 수정 금지)
  - 맵 `Player`
    - `Move`: 1D Axis composite 2개
      - `negative=<Keyboard>/a`, `positive=<Keyboard>/d`
      - `negative=<Keyboard>/leftArrow`, `positive=<Keyboard>/rightArrow`
    - `Jump`: `<Keyboard>/space`
  - 맵 `UI`: `New action`(바인딩 없음, 사용처 없음)
- `InputHandler`(`Assets/02. Scripts/Player/InputHandler.cs`, Player 오브젝트에 부착)
  - `Awake`에서 `new PlayerInput()`으로 **자기 전용 인스턴스**를 만든다. 이 래퍼는 내부에 InputActionAsset 복사본을 갖는다.
  - `OnEnable`에서 `input.Enable()`.
  - 외부에 노출하는 것은 `MoveInput`/`Jump`뿐이다. 액션 인스턴스는 private이다.
  - 따라서 리바인딩은 **이 인스턴스의 액션에 적용**해야 실제 조작에 반영된다. 다른 `new PlayerInput()`에 적용하면 반영되지 않는다.
- 사용처: `Player.Start`에서 `GetComponent<InputHandler>()`로 연결한다.
  - `Player.Update → Move()`에서 `handler.MoveInput`, `handler.Jump`를 읽는다(`Player.cs:181`, `:204`).
- **기존 버그(정지 중 입력)**:
  - `Move()`는 `Update`에서 돌기 때문에 `timeScale = 0`이어도 실행된다.
  - 그래서 지금도 볼륨 패널이 열린 상태에서 A/D/Space를 누르면 `ChangeLane`/`Jump` 코루틴이 시작된다. 코루틴은 정지 중에 대기하다가 재개 후 진행된다.
  - 리바인딩 중에 누르는 키도 같은 경로로 들어간다. 메뉴가 열려 있는 동안 `Player` 맵을 꺼야 한다.
- 바인딩 저장 기능은 현재 없다. Input System 1.20.0은 `SaveBindingOverridesAsJson`/`LoadBindingOverridesFromJson`과 `PerformInteractiveRebinding`을 지원한다.

## 4. 정지(timeScale)를 쓰는 다른 곳

| 위치 | 동작 |
|---|---|
| `BossIntroState.cs:24,104` | 컷씬 동안 0, 끝나면 이전 값으로 복원 |
| `Boss.cs:111` | `Time.timeScale = 1f` |
| `Player.ResizeCoroutine` (`Player.cs:332`) | 크기 변경 연출 동안 0, `WaitForSecondsRealtime` 사용 |
| `VolumeSettingsUI` | 패널이 열려 있는 동안 0 |

- 메뉴는 기존처럼 "이미 정지 중이면 열지 않음" 규칙을 유지하면 된다.
- 타이틀로 나갈 때는 `Time.timeScale = 1`을 반드시 복원해야 한다. timeScale은 씬을 넘어 유지되는 전역 값이다.
- uGUI(EventSystem/Slider/Button)는 unscaled time으로 동작하므로 정지 중에도 조작할 수 있다. 인터랙티브 리바인딩도 timeScale과 무관하다.

## 5. 영향 범위

| 파일 | 변경 |
|---|---|
| `UI/VolumeSettingsUI.cs` | ESC/정지 로직을 메뉴 컨트롤러로 이전하고, 볼륨 슬라이더 바인딩만 남김 |
| `Player/InputHandler.cs` | 저장된 바인딩 오버라이드 로드, 액션 노출, 게임플레이 입력 on/off |
| 신규 `UI/SettingsMenuUI.cs` | ESC 토글, 정지/복원, 창(탭) 전환, 타이틀로 나가기 |
| 신규 `UI/KeyBindingUI.cs`, `UI/KeyRebindRow.cs` | 키 매핑 창, 리바인딩 |
| 신규 `Input/KeyBindingStore.cs` | 오버라이드 JSON을 PlayerPrefs에 저장/로드 |
| `GameScene.unity` | `VolumeSettings` 계층 재구성(창 2개 + 탭 버튼) |

- 수정할 파일 중 CP949 파일은 없다. `InputHandler.cs`는 한글이 없고, `VolumeSettingsUI.cs`는 UTF-8이다. `Player.cs`(CP949)는 수정하지 않는 방향으로 설계한다.
- `PlayerInput.cs`/`.inputactions`는 수정하지 않는다.

## 6. 위험 요소

1. **ESC 이중 처리**
   - 리바인딩 대기 중 ESC는 "리바인딩 취소"로 쓴다(`WithCancelingThrough`). 같은 ESC가 메뉴 토글 액션에도 들어가 메뉴가 닫힐 수 있다.
   - 두 콜백의 실행 순서는 보장되지 않는다. 리바인딩이 끝난 프레임에는 토글을 무시하는 가드가 필요하다.
   - ESC 자체는 리바인딩 대상에서 제외한다.
2. **키 중복**
   - Left/Right/Jump를 같은 키로 지정하면 한 번 누를 때 두 동작이 일어난다.
   - Move composite에서 negative와 positive가 같으면 축 값이 0이 되어 이동하지 않는다.
3. **바인딩 인덱스**
   - `Move`의 바인딩 순서는 `[0]=composite, [1]=a, [2]=d, [3]=composite, [4]=←, [5]=→`다(`.inputactions` 기준). `Jump`는 `[0]=space`.
   - `.inputactions`를 수정하면 인덱스가 바뀌므로 인덱스를 하드코딩하지 않고 composite 파트 이름으로 찾는다.
   - 저장된 오버라이드 JSON은 바인딩 id로 매칭되므로 일반적인 수정에는 견딘다.
4. **Addressables 씬 핸들**
   - `SceneManager.LoadScene("Title")`로 나가면 Addressables로 로드한 GameScene 핸들이 Release되지 않을 수 있다(빌드에서 번들이 계속 유지될 수 있음). **확인 필요**
   - 대안: Title을 Addressables에 등록하고 `Addressables.LoadSceneAsync("Title")`로 로드한다. 이 경우 빌드 설정의 첫 씬과 중복 등록 경고가 날 수 있다.
5. **타이틀 이동 시 잔여 상태**
   - timeScale, 픽셀화/글리치 에셋 값, BGM 코루틴(SoundManager와 함께 파괴되므로 문제 없음)이 남을 수 있다.
6. **한글 폰트 없음**
   - 버튼과 라벨은 영문으로 만든다.
   - 키 이름은 `InputControlPath.ToHumanReadableString`이나 `GetBindingDisplayString` 결과(예: "A", "Space", "Left Arrow")를 그대로 표시한다.

## 7. 피드백 1차 이후 추가 조사

### Title 씬 구성 (에디터에서 additive로 열어 덤프)
```
Main Camera [Camera, AudioListener]
Directional Light
Canvas [Canvas(ScreenSpaceOverlay, sort 0), CanvasScaler, GraphicRaycaster]
  BackGround [Image] / Glitch [Image] / Title [Image]
  Button (Legacy) [Button] + Text (Legacy)   ← 시작 버튼 (StartButton.StartLoading 연결)
EventSystem [EventSystem, InputSystemUIInputModule]
SceneChanger [StartButton] sceneToLoad="SampleScene"
TitleAudio [AudioSource] clip=Lobby_music, outputAudioMixerGroup=null, playOnAwake
```
- Title에는 `SoundManager`가 **없다**.
  - Title에서 `SoundManager.Instance`에 접근하면 싱글톤이 믹서 없이 자동 생성된다. 이 경우 볼륨 조절이 동작하지 않는다.
  - `TitleAudio`는 믹서 그룹에 연결되어 있지 않아 BGM 볼륨이 적용되지 않는다.
- `SoundManager.Start`는 볼륨을 PlayerPrefs에서 읽어 믹서에 반영한다. 그 뒤 `data == null`이면 바로 return한다.
  - 따라서 Title에 `data` 없이 배치하면 **볼륨 반영만 하고 BGM은 재생하지 않는다**.
- EventSystem은 `InputSystemUIInputModule`을 쓴다. 두 씬이 같으므로 프리팹 UI를 그대로 쓸 수 있다.
- Title에는 `InputHandler`가 없다. 키 매핑 UI는 자체 `PlayerInput` 인스턴스에 오버라이드를 적용하고 저장해야 한다. 게임 씬의 InputHandler가 Awake에서 그 값을 로드한다.

### Addressables와 빌드 씬 목록 (확인함)
- `Library/PackageCache/com.unity.addressables@*/Editor/Settings/AddressableScenesManager.cs`
  - `CheckForScenesInBuildList`(:83): 빌드 목록에 있는 씬을 Addressable로 지정하면 경고를 내고 빌드 목록에서 **비활성화**한다(`enabled = false`).
  - `OnScenesChanged`: 반대로 Addressable 씬을 빌드 목록에 추가하면 Addressable 엔트리를 **삭제**한다.
- 지금 빌드 목록에는 Title 하나뿐이다. Title을 Addressable로 만들면 **빌드에 시작 씬이 없어진다**.
  - 해결책: 빌드 목록 0번에 빈 `Boot` 씬을 두고, 이 씬이 `Addressables.LoadSceneAsync("Title")`로 Title을 로드한다.
  - 그러면 Title과 GameScene이 모두 Addressables로 로드된다. Single 모드 로드는 이전에 Addressables로 로드한 씬 핸들을 자동으로 정리한다.
- 에디터에서는 Play Mode Script 설정에 따라 Addressable 씬도 로드된다. 지금 `StartButton`과 `Player.Restart`도 같은 방식으로 동작하고 있다.
- Title을 에디터에서 직접 열고 Play하는 기존 작업 방식은 그대로 동작한다. 열려 있는 씬부터 시작하기 때문이다.

## 8. 피드백 3차 이후 추가 조사: UI 한글화

- 프로젝트 안의 폰트는 `Assets/UnityTechnologies/ParticlePack/TextMesh Pro/Fonts/LiberationSans.ttf`와 그 TMP 에셋(`LiberationSans SDF`, `- Fallback`)뿐이다. **한글 글리프가 없다.**
  - 이 폰트에 한글 문자열을 넣으면 □로 표시된다.
- TextMesh Pro는 `com.unity.ugui`(2.6.0)에 포함되어 있다. `TMP_FontAsset.CreateFontAsset`과 Dynamic 아틀라스를 쓸 수 있다.
- 이 PC의 `C:/Windows/Fonts`에 있는 한글 폰트:
  - `NotoSansKR-VF.ttf`: Noto Sans KR, SIL OFL 1.1. 재배포와 임베딩이 가능하다.
  - `NanumSquareRound{L,R,B,EB}.ttf`: 나눔스퀘어라운드, SIL OFL 1.1. 재배포와 임베딩이 가능하다.
  - `NGULIM.TTF`, 맑은 고딕 계열: Microsoft 폰트라 재배포할 수 없다.
  - 위 라이선스 내용은 일반적으로 알려진 배포 조건을 기준으로 적었다. 폰트 파일에 포함된 라이선스 문구는 **확인 필요**다.
- 외부 다운로드는 필요 없다. 시스템 폰트 파일을 프로젝트에 복사하면 된다.
- 한글을 바꿔야 하는 곳:
  - 프리팹 `Assets/07. Prefabs/UI/SettingsMenu.prefab`의 TMP 텍스트 21개(탭, 라벨, 버튼, 헤더, 키 버튼, 팝업)
  - 코드 문자열 3개: `SettingsMenuUI`(팝업 메시지), `KeyRebindButton`(`Press a key...`, `None`)
- 키 버튼 텍스트는 `InputAction.GetBindingDisplayString`이 런타임에 영문 키 이름을 넣는다. 한글 폰트에도 라틴 글리프가 있으므로 그대로 표시된다.
