# ESC 설정 메뉴 — Plan

근거: `docs/SettingsMenu/Research.md` (§7은 피드백 1차 이후 추가 조사)
상태: **전 Phase 구현 완료 (2026-11-27)**

## 구현 현황 (Phase 1~8)
- **완료**: Phase 1~8 전 항목. 컴파일 에러 0, 새 경고 0(기존 Missing Script, cubemap 버전 경고만 있음).
- 플레이 검증으로 확인한 것:
  - ESC 열기/닫기와 timeScale 0/복원
  - 정지 중 A/D/Space 입력 차단
  - 확인 팝업 ESC → 팝업만 닫힘
  - 리바인딩(Jump→W), 중복 키(Jump→A 시 Move Left Primary `None`)
  - 리바인딩 중 ESC → 취소만 됨
  - 씬 재로드 후 유지, Reset
  - 2D 상태에서 Exit to Title → Title(timeScale 1, pixelScale 1, DDOL 잔여 없음)
  - Title 메뉴: Exit 버튼 숨김, 정지 안 함, BGM 볼륨 믹서 반영
  - Title에서 바꾼 키가 게임에 적용됨
  - Boot → Title → 게임 흐름
- 계획과 달라진 점:
  - 탭 2개를 `SETTINGS`/`KEY BINDINGS`로 만들었다. 선택된 탭은 `interactable=false`이고 disabledColor를 흰색으로 두어 밝게 보이게 했다.
  - 프리팹은 기존 패널의 슬라이더와 버튼을 복제해 만들었다. 기존 `Canvas/VolumeSettings`는 삭제했다.
- 검증 메모(테스트 환경 문제이며 코드 문제는 아님):
  - `simulate_key`는 에디터가 포커스를 잃으면 키 이벤트가 게임 액션에 전달되지 않는다.
  - 그래서 키를 보내기 전마다 Unity 창을 포그라운드로 올려서 테스트했다.

## 확정된 결정 (피드백 1차)
1. **창 구분은 패널 하나 + 상단 탭 2개**로 한다(`SETTINGS` / `KEY BINDINGS`).
2. **타이틀로 나가기 전에 확인 팝업을 띄운다.**
   - ESC를 누르면 팝업만 닫힌다.
   - UI는 **스택**으로 관리한다. ESC는 항상 맨 위 UI 하나만 닫는다.
3. **방향키도 바꿀 수 있다.** 동작마다 Primary/Secondary 2칸을 둔다. 기본값은 기존 그대로다.
   - Move Left: `A` / `←`
   - Move Right: `D` / `→`
   - Jump: `Space` / (없음)
4. **중복 키를 처리한다.** 새로 누른 키는 그대로 할당하고, 그 키를 쓰던 기존 칸은 **바인딩을 해제한다**(표시 `None`).
5. **씬 로드는 Addressables로 통일한다.**
   - Title을 Addressable로 만들면 빌드 목록에서 자동으로 비활성화된다(Research §7). 그래서 시작용 **Boot 씬**을 새로 만든다.
6. **Title 씬에도 설정 메뉴를 둔다.**
   - 메뉴는 프리팹으로 만들어 두 씬이 같이 쓴다.
   - Title에서는 `Exit to Title` 버튼을 숨기고, 게임 정지도 하지 않는다.

## 설계 요약

### UI 스택
- `UIPanel`(베이스): 스택에 올라가고 내려가는 UI 단위다.
  - 기본 동작은 `gameObject.SetActive`이고, `OnPush()`/`OnPop()`을 virtual로 둔다.
- `UIStack`(메뉴 루트 컴포넌트): ESC 입력, 스택, 정지/입력 차단을 담당한다.
  - ESC 처리
    - 스택이 비어 있으면 `rootPanel`(설정 메뉴)을 Push한다.
    - 비어 있지 않으면 맨 위를 Pop한다.
    - 리바인딩 중이거나 리바인딩이 끝난 프레임이면 무시한다. 이때 ESC는 리바인딩 취소 용도다.
  - 첫 Push: `pauseGame`이면 timeScale을 저장하고 0으로 만든다. `InputHandler`가 있으면 게임플레이 입력을 끈다.
    - 기존 규칙도 유지한다. 이미 timeScale 0이면(컷씬, 크기 변경 연출) 열지 않는다.
  - 스택이 비면: timeScale과 입력을 복원하고 `PlayerPrefs.Save()`.
  - `Push(UIPanel)`, `Pop()`, `Clear()`(전부 닫기), `Count`.
- 레이어 순서: `[SettingsMenuUI] → [ConfirmPopupUI]`.
  - 팝업에는 전체 화면 차단용 이미지를 두어 아래 메뉴를 클릭할 수 없게 한다.
  - Close 버튼은 `Clear()`를 호출한다.

### 기능 컴포넌트
- `SettingsMenuUI : UIPanel`
  - 탭 전환. 열 때마다 `SETTINGS` 탭부터 보여준다.
  - `OnPush`에서 볼륨/키 표시를 새로 고친다.
  - `Exit to Title` 버튼을 누르면 `ConfirmPopupUI.Show("Return to title?\nProgress will be lost.", ExitToTitle)`를 Push한다.
  - `allowExitToTitle`이 false이면(Title 씬) 버튼을 숨긴다.
- `ConfirmPopupUI : UIPanel`: 메시지, `Yes`/`No` 버튼. `No`나 ESC는 Pop, `Yes`는 콜백을 실행한다.
- `VolumeSettingsUI`(축소): 슬라이더와 SoundManager 연결, `Refresh()`만 남긴다.
- `KeyBindingUI` + `KeyRebindButton`(칸 하나 = 바인딩 하나)
- `KeyBindingStore`(static): 오버라이드 JSON을 PlayerPrefs에 저장한다(키 `Input.Bindings`).
- 키 매핑 대상
  - GameScene: `InputHandler.Actions`. 바꾸면 즉시 반영된다.
  - Title: InputHandler가 없으므로 `KeyBindingUI`가 자체 `PlayerInput`을 만든다. 저장한 값은 게임 시작 시 InputHandler가 로드한다.

### 씬 흐름
- `Boot`(빌드 목록 유일) → Addressables `Title` → Addressables `SampleScene`(=GameScene)
- GameScene → Title: `Exit to Title`

### 프리팹 구조
`Assets/07. Prefabs/UI/SettingsMenu.prefab`. 자체 Canvas를 가진다(Overlay, sortingOrder 100, 1920x1080 Scaler).

```
SettingsMenu [Canvas, CanvasScaler, GraphicRaycaster, UIStack]
  MenuPanel (off) [SettingsMenuUI]  + 딤 Image
    Box 720x500
      Tabs: SettingsTabButton "SETTINGS" / KeysTabButton "KEY BINDINGS"
      SettingsPage [VolumeSettingsUI]
        Master/BGM/SFX Label+Slider
        ExitTitleButton "Exit to Title"
      KeysPage (off) [KeyBindingUI]
        Header:       ""          "Primary"   "Secondary"
        MoveLeftRow:  "Move Left"  [A]         [Left Arrow]
        MoveRightRow: "Move Right" [D]         [Right Arrow]
        JumpRow:      "Jump"       [Space]     (빈칸)
        ResetButton "Reset to Default"
      CloseButton "Close (ESC)"
  ConfirmPopup (off) [ConfirmPopupUI] + 전체 차단 Image
    Box: MessageText / YesButton "Yes" / NoButton "No"
```

## Phase 1 — Boot 씬 / Addressables
- [x] 신규 `Assets/02. Scripts/BootLoader.cs`: `Start`에서 `Addressables.LoadSceneAsync(titleSceneName)` 호출(기본값 `"Title"`).
- [x] 신규 씬 `Assets/01. Scenes/Boot.unity`: `BootLoader` 오브젝트 하나와 검은 배경 카메라 하나.
- [x] Addressables `Scenes` 그룹에 `Title.unity`를 주소 `Title`로 추가한다.
- [x] 빌드 목록을 `[Boot]`로 설정한다. Title은 Addressables가 자동으로 비활성화하는데, 비활성 항목이 남아 있으면 목록에서 정리한다.
- [x] 확인 항목: 에디터에서 Boot로 Play하면 Title이 뜨는지, 시작 버튼으로 게임 씬이 로드되는지. (Phase 7에서 확인함)

## Phase 2 — 바인딩 저장 / InputHandler
- [x] 신규 `Assets/02. Scripts/Input/KeyBindingStore.cs`(static)
  - `Load(InputActionAsset)`, `Save(InputActionAsset)`(`SaveBindingOverridesAsJson`).
  - `ResetAll(InputActionAsset)`: `RemoveAllBindingOverrides` 후 PlayerPrefs 키를 삭제한다.
- [x] `Assets/02. Scripts/Player/InputHandler.cs`
  - `Awake`: `new PlayerInput()` 직후 `KeyBindingStore.Load(input.asset)`.
  - `public PlayerInput Actions => input;`
  - `public void SetGameplayEnabled(bool on)`: `input.Player.Enable()/Disable()`. 끌 때는 `MoveInput = 0`, `Jump = false`로 초기화한다.

## Phase 3 — UI 스택 기반
- [x] 신규 `Assets/02. Scripts/UI/UIPanel.cs`: `virtual OnPush()`/`OnPop()`(기본 SetActive), `UIStack Stack { get; set; }`.
- [x] 신규 `Assets/02. Scripts/UI/UIStack.cs`
  - 직렬화 필드: `rootPanel`, `pauseGame`(GameScene true / Title false).
  - ESC `InputAction("<Keyboard>/escape")`: 기존 `VolumeSettingsUI` 방식을 옮긴다.
  - `Push`, `Pop`, `Clear`.
  - 정지/복원, `InputHandler.SetGameplayEnabled`(`FindAnyObjectByType<InputHandler>()`, 없으면 생략).
  - 리바인딩 가드: `KeyBindingUI.IsRebinding || KeyBindingUI.LastRebindEndFrame == Time.frameCount`이면 ESC를 무시한다.
- [x] 신규 `Assets/02. Scripts/UI/ConfirmPopupUI.cs`: `Show(string message, Action onConfirm)`가 텍스트를 설정하고 `Stack.Push(this)`한다. Yes는 `onConfirm`, No는 `Stack.Pop()`.

## Phase 4 — 설정 창
- [x] 신규 `Assets/02. Scripts/UI/SettingsMenuUI.cs`(`UIPanel` 상속)
  - 직렬화 필드: `settingsPage`, `keysPage`, 탭 버튼 2개, `closeButton`, `exitTitleButton`, `volumeSettings`, `keyBinding`, `confirmPopup`, `allowExitToTitle`, `titleSceneName = "Title"`.
  - `OnPush`: Refresh 후 `ShowPage(Settings)`. `OnPop`: `keyBinding.CancelRebind()`.
  - 선택된 탭 버튼은 `interactable=false`로 표시한다.
  - `ExitToTitle()`(중복 실행 방지 플래그)
    1. `Stack.Clear()`
    2. `Time.timeScale = 1`
    3. `CameraManager.Instance.SetPixelateIntensity(1)`, `SetGlitch(0.002f)`
    4. `PlayerPrefs.Save()`
    5. `Addressables.LoadSceneAsync(titleSceneName)`
  - 주의: `CameraManager`는 GameScene에만 있다. Title에서는 `allowExitToTitle=false`라 이 코드가 호출되지 않는다. 싱글톤 자동 생성이 일어나지 않도록 `allowExitToTitle` 가드 안에서만 접근한다.
- [x] `Assets/02. Scripts/UI/VolumeSettingsUI.cs` 축소
  - ESC/timeScale/panel/closeButton 코드를 삭제한다.
  - 슬라이더 바인딩과 `Refresh()`만 남긴다.

## Phase 5 — 키 매핑 창
- [x] 신규 `Assets/02. Scripts/UI/KeyRebindButton.cs`
  - 직렬화 필드: `actionName`("Move"/"Jump"), `compositePart`("negative"/"positive"/빈 값), `slot`(0=Primary, 1=Secondary), `button`, `label`(TMP).
  - 바인딩 인덱스는 조건에 맞는 바인딩 중 `slot`번째로 찾는다. 조건은 `compositePart`가 있으면 `isPartOfComposite && name == compositePart`, 없으면 composite가 아닌 바인딩이다. 하드코딩하지 않는다.
    - 결과: Move negative slot0=a, slot1=←, positive slot0=d, slot1=→, Jump slot0=space.
  - 대응하는 바인딩이 없는 칸(Jump Secondary)은 프리팹에서 빈칸으로 두고 이 컴포넌트를 붙이지 않는다.
  - `Refresh()`: `effectivePath`가 비어 있으면 `None`, 아니면 `GetBindingDisplayString(index)`.
- [x] 신규 `Assets/02. Scripts/UI/KeyBindingUI.cs`
  - `static bool IsRebinding`, `static int LastRebindEndFrame`.
  - 액션 소스는 `FindAnyObjectByType<InputHandler>()?.Actions`다. 없으면 자체 `new PlayerInput()`을 만들고 `KeyBindingStore.Load`한다(`OnDestroy`에서 Dispose).
  - `StartRebind(btn)`
    - 진행 중인 리바인딩을 취소하고 칸 텍스트를 `"Press a key..."`로 바꾼다.
    - 리바인딩 설정: `PerformInteractiveRebinding(index).WithControlsHavingToMatchPath("<Keyboard>").WithControlsExcluding("<Keyboard>/escape").WithCancelingThrough("<Keyboard>/escape").OnMatchWaitForAnother(0.1f)`.
    - 액션이 켜져 있으면(Title의 자체 인스턴스는 꺼져 있음, GameScene은 Player 맵이 꺼져 있음) Disable했다가 끝나면 원래 상태로 되돌린다.
  - 완료 처리(**중복 키 해제**)
    - 새 `effectivePath`와 같은 키를 쓰는 **다른 모든 칸**(5칸 중 자신 제외)에 `ApplyBindingOverride(index, "")`를 적용해 바인딩을 해제한다.
    - 그다음 `KeyBindingStore.Save`, 전체 `Refresh`, Dispose.
  - 취소 처리: `Refresh`, Dispose.
  - 완료와 취소 모두 `IsRebinding=false`, `LastRebindEndFrame=Time.frameCount`로 설정한다.
  - `CancelRebind()`, Reset 버튼(`KeyBindingStore.ResetAll` + `Refresh`), `OnDisable`/`OnDestroy`에서 오퍼레이션을 정리한다.
  - 참고: 해제된 칸은 `None`으로 표시된다. 한 방향의 두 칸이 모두 해제되면 그 방향으로 이동할 수 없다. 사용자가 다시 지정하거나 Reset하면 된다.

## Phase 6 — 프리팹 / 씬 반영
- [x] `SettingsMenu.prefab`을 만든다(위 구조, TMP LiberationSans, 기존 패널 스타일 재사용). eval_file 에디터 스크립트로 작성한다.
- [x] GameScene
  - 기존 `Canvas/VolumeSettings` 계층을 삭제한다.
  - 프리팹 인스턴스를 배치한다(`pauseGame=true`, `allowExitToTitle=true`).
- [x] Title
  - 프리팹 인스턴스를 배치한다(`pauseGame=false`, `allowExitToTitle=false`).
  - `SoundManager` 오브젝트를 추가한다. `mixer`, `bgmGroup`, `sfxGroup`은 연결하고 `data`는 비워서 볼륨만 반영되게 한다.
  - `TitleAudio.outputAudioMixerGroup`을 BGM 그룹으로 연결한다.

## Phase 7 — 검증 (MCP, 단계마다)
- [x] 스크립트를 수정할 때마다 컴파일 에러/경고 0인지 확인한다.
- [x] Boot에서 시작해 Title → 게임 → `Exit to Title` → 다시 게임까지 이어지는 흐름이 정상인지 확인한다.
- [x] GameScene에서 확인할 항목:
  - ESC로 메뉴가 열리면 timeScale이 0이 되는지
  - 메뉴가 열린 상태에서 A/D/Space를 눌러도 레인이 바뀌지 않는지
  - 탭 전환과 볼륨 슬라이더가 동작하는지
  - Exit를 누르면 팝업이 뜨고, ESC를 누르면 팝업만 닫히는지
  - 메뉴 상태에서 ESC를 누르면 메뉴가 닫히고 timeScale이 복원되는지
  - 팝업에서 Yes를 누르면 Title로 이동하고 timeScale=1, 픽셀화 1인지
- [x] 키 매핑에서 확인할 항목:
  - Jump를 `W`로 바꾸면 W로 점프하는지
  - Jump를 `A`로 바꾸면 Move Left Primary가 `None`이 되는지
  - 리바인딩 중 ESC를 누르면 리바인딩만 취소되는지
  - 씬을 재로드해도 바인딩이 유지되는지
  - Reset이 동작하는지
- [x] Title에서 확인할 항목:
  - ESC 메뉴에 Exit 버튼이 없고 게임이 정지되지 않는지
  - 볼륨이 Lobby BGM에 반영되는지
  - Title에서 바꾼 키가 게임에 적용되는지
- [x] 새 에러가 없는지 확인한다. 기존 Missing Script, `Ray does not intersect` 경고는 제외한다.
- [x] 테스트로 바꾼 PlayerPrefs를 정리하고, 렌더러 에셋 diff를 확인한다.

## Phase 8 — 문서
- [x] CLAUDE.md를 갱신한다.
  - 씬 흐름(Boot → Title → SampleScene, 모두 Addressables)
  - 입력(`KeyBindingStore`, `InputHandler.Actions`)
  - UI 스택(`UIStack`/`UIPanel`)
- [x] Plan 상단에 구현 현황을 요약한다.

## Phase 9 — UI 한글화 (피드백 3차)
근거: Research §8. 결정은 모두 기본안으로 했다: Noto Sans KR, 키 이름은 영문, `전체 음량 / 배경음 / 효과음`.

구현 메모:
- `Assets/Fonts/NotoSansKR-VF.ttf`로 Dynamic SDF 에셋(`NotoSansKR-VF SDF.asset`, Multi Atlas)을 만들었다.
  - TMP는 가변 폰트의 축을 고를 수 없어 기본 인스턴스인 **Thin**으로 로드된다.
  - 그래서 폰트 머티리얼에 `_FaceDilate = 0.35`를 줘서 Regular~Medium 굵기로 보정했다. 캡처로 가독성을 확인했다.
- 커밋 전에 동적 아틀라스 데이터를 비웠다(`ClearFontAssetData`). 런타임에 필요한 글자만 생성된다.
  - 에디터에서 플레이하면 이 에셋에 글리프가 다시 쌓여 git diff가 생길 수 있다.
- 라이선스: Noto Sans KR은 SIL OFL 1.1이다. 라이선스 문구는 폰트 파일의 메타데이터(name 테이블)에 포함되어 있다.

- [x] 한글 폰트 추가
  - 시스템 폰트를 `Assets/Fonts/`에 복사한다.
  - **TMP Dynamic 폰트 에셋**을 만든다. Atlas Population Mode = Dynamic, Multi Atlas 켬.
    - 한글 11,172자를 미리 굽지 않고, 쓰인 글자만 런타임에 아틀라스에 추가하는 방식이다.
  - 후보 폰트(아래 결정 1).
- [x] `SettingsMenu.prefab`의 모든 TMP 텍스트에 새 폰트를 지정하고 문구를 바꾼다.

  | 위치 | 기존 | 변경 |
  |---|---|---|
  | 탭 | SETTINGS / KEY BINDINGS | 설정 / 키 설정 |
  | 볼륨 라벨 | Master / BGM / SFX | 전체 음량 / 배경음 / 효과음 |
  | 버튼 | Exit to Title | 타이틀로 나가기 |
  | 버튼 | Close (ESC) | 닫기 (ESC) |
  | 키 헤더 | Primary / Secondary | 기본 키 / 보조 키 |
  | 키 행 | Move Left / Move Right / Jump | 왼쪽 이동 / 오른쪽 이동 / 점프 |
  | 버튼 | Reset to Default | 기본값으로 초기화 |
  | 팝업 버튼 | Yes / No | 예 / 아니오 |

- [x] 코드 문자열 한글화
  - `SettingsMenuUI`: 팝업 메시지 `"타이틀로 나가시겠습니까?
진행 상황은 저장되지 않습니다."`
  - `KeyRebindButton`: `"Press a key..."` → `"키를 누르세요..."`, `"None"` → `"없음"`
- [x] 키 이름 표시
  - 기본안: `GetBindingDisplayString` 결과(`A`, `Space`, `Left` 등)를 그대로 쓴다. 키보드 각인과 같기 때문이다.
  - 결정 2에 따라 바뀔 수 있다.
- [x] 레이아웃 확인
  - 한글은 폭이 넓어서 라벨이 넘칠 수 있다. 라벨 폭을 조정하거나 TMP Auto Size를 쓴다.
  - 확인은 캡처로 한다.
- [x] 검증
  - 컴파일, 콘솔 확인.
  - GameScene/Title에서 메뉴, 팝업, 키 탭을 캡처해 글자 깨짐(□)이 없는지 확인한다.
- [x] CLAUDE.md: "UI 텍스트는 영문" 관련 언급을 한글 폰트 사용으로 갱신한다.

### Phase 9 결정 (기본안으로 확정)
1. **폰트**: 둘 다 OFL이라 게임에 포함해 배포할 수 있다.
   - (추천) **Noto Sans KR**(`C:/Windows/Fonts/NotoSansKR-VF.ttf`, 가변 폰트, 기본 굵기만 사용): 깔끔한 고딕체다.
   - **나눔스퀘어라운드**(`NanumSquareRoundR.ttf`/`B.ttf`): 둥근 고딕체라 캐주얼 게임에 어울린다. 정적 폰트라서 굵기별 파일이 따로 있다.
   - 다른 폰트를 원하면 파일을 주거나 경로를 알려주면 된다.
   - 참고: `맑은 고딕`, `굴림`은 Windows 폰트라 게임에 포함해 배포할 수 없다. 제외한다.
2. **키 이름 표시**
   - 기본안: 영문 그대로(`A`, `Space`, `Left`).
   - 대안: 일부를 한글/기호로 바꾼다(`Space`→`스페이스`, `Left`/`Right`→`←`/`→`).
3. **볼륨 라벨 문구**
   - 기본안: `전체 음량 / 배경음 / 효과음`.
   - 대안: `마스터 / BGM / 효과음`.

## 남은 확인 사항
- **방향키 해석**: "방향키 바인딩 추가"를 **Primary/Secondary 2열 구조로, 방향키가 Secondary 칸에 기본값으로 들어가는 것**으로 이해했다. Jump는 원래 바인딩이 하나뿐이라 Secondary 칸이 비어 있다. Jump에도 보조 키가 필요하면 `.inputactions`에 바인딩을 추가해야 한다(래퍼 재생성 포함).
- **Boot 씬 추가**: 빌드 시작 씬이 Boot로 바뀐다. 에디터에서 Title이나 GameScene을 직접 열고 Play하는 기존 방식은 그대로 동작한다.
- **메뉴 열기 거부**: 쿠파 컷씬처럼 이미 정지된 상태에서는 ESC 메뉴가 열리지 않는다(기존 규칙 유지).
