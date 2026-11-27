# Title 설정 버튼 — Research

요청: Title 씬의 시작 버튼 아래에, 시작 버튼과 같은 디자인의 "설정" 버튼을 만든다. 누르면 설정 창(ESC 메뉴와 같은 창)이 열린다.

## Title 씬 Canvas (`Assets/01. Scenes/Title.unity`, 에디터에서 덤프)
- Canvas: ScreenSpaceOverlay, sort 0, CanvasScaler **ConstantPixelSize**.
- 자식 순서
  1. `BackGround`: 전체 화면, `Title.png`
  2. `Title`: 로고, `super_mario_transparent.png`
  3. `Glitch`: 전체 화면, `GlitchMetetrial _Lobby`
  4. `Button (Legacy)`
- **시작 버튼 `Button (Legacy)`**
  - RectTransform: anchor/pivot 하단 중앙 (0.5, 0)/(0.5, 0.5), `pos=(0, 200)`, `size=800x100`.
  - Image: `Assets/10. Sprites/hpBar.png`의 `hpBar_0`, color `(1, 0.844, 0.844, 1)`, Simple.
  - Button: ColorTint. normal 흰색, highlighted 0.961, pressed 0.784.
  - onClick(persistent): `SceneChanger (StartButton).StartLoading`.
  - 자식 `Text (Legacy)`: uGUI `Text`, "Press to Start".
    - 폰트 `LegacyRuntime`, 크기 30, 색 `(0.604, 0.481, 0.481)`, MiddleCenter, 부모 크기에 맞춰 stretch.
  - Outline/Shadow 같은 효과 컴포넌트는 없다.
- `SettingsMenu` 프리팹 인스턴스(Sort 100, 자체 Canvas)가 씬 루트에 있다.
  - `UIStack`의 `pauseGame=false`, `SettingsMenuUI.allowExitToTitle=false`.

## 설정 창을 여는 경로
- 지금은 `UIStack.OnEscape()`(private)가 스택이 비어 있을 때 `Push(rootPanel)`로 여는 방법뿐이다.
- `Push(UIPanel)`는 public이지만, 버튼 onClick(persistent)에 연결하려면 인자 없는 public 메서드가 있는 편이 단순하다.
  - 예: `UIStack.OpenRoot()`
- 메뉴가 열리면 딤 이미지(`MenuPanel`, 전체 화면 Image)가 레이캐스트를 막는다. 뒤에 있는 시작/설정 버튼은 클릭되지 않는다.

## 발견한 문제: 리바인딩과 UI Submit 충돌 (기존 메뉴에도 해당)
- `EventSystem`은 `InputSystemUIInputModule`을 쓴다(Title/GameScene 동일). 기본 UI 액션 `Submit`은 Enter/Space 등에 바인딩되어 있다.
- 키 버튼을 클릭하면 그 버튼이 EventSystem의 **선택 오브젝트**가 된다.
- 리바인딩 대기 중 **Space나 Enter**를 누르면 두 가지가 동시에 일어날 수 있다.
  - 리바인딩이 그 키를 받는다.
  - UI Submit이 선택된 키 버튼을 다시 클릭한다.
  - 결과: `StartRebind`가 다시 불려 진행 중인 리바인딩이 취소되거나 재시작될 수 있다.
  - Space는 기본 Jump 키라 실제로 쓰일 가능성이 크다. 아직 재현하지 않았다. **확인 필요**
- Title에서 설정 버튼을 클릭한 뒤에도 그 버튼이 선택된 채로 남는다. 메뉴가 열린 상태에서 Enter/Space를 누르면 설정 버튼에 Submit이 간다.
  - `Push`는 이미 열려 있으면 무시하므로 동작에는 문제가 없다.
- 대응: 리바인딩을 시작할 때 `EventSystem.current.SetSelectedGameObject(null)`로 선택을 해제하면 Submit이 갈 곳이 없어진다.

## 영향 범위
- `Assets/02. Scripts/UI/UIStack.cs`: `OpenRoot()` 추가
- `Assets/02. Scripts/UI/KeyBindingUI.cs`: 리바인딩 시작 시 선택 해제
- `Assets/01. Scenes/Title.unity`: 버튼 복제, 텍스트 변경, onClick 연결
- 시작 버튼, `StartButton.cs`는 수정하지 않는다.
