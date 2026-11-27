# Title 설정 버튼 — Plan

근거: `docs/TitleSettingsButton/Research.md`
상태: **구현 완료 (2026-11-27)**

## 구현 현황
- **완료**: Phase 1~4. 결정은 기본안대로 했다: 글자 `Settings`, 위치 y=80.
- 컴파일 에러 0, 새 경고 0.
- 검증 결과
  - Title 캡처에서 시작 버튼과 같은 디자인으로 바로 아래에 배치된 것을 확인했다.
  - 버튼 클릭: 메뉴가 열림(stack 1, timeScale 1 유지). ESC로 닫힘.
  - 시작 버튼: 게임 씬 로드 정상.
  - 리바인딩 수정 확인: 키 버튼을 선택한 상태에서 Jump를 W로 바꾼 뒤 다시 Space로 지정했다. 선택이 해제되어 Space가 정상으로 할당됐다.
- 메모
  - 작업 중 Title 씬에 타이틀 이미지 교체(`title_IMG.png` → `Title.png`, `super_mario_transparent.png`)가 같이 저장되어 있었다. 이 변경은 이 작업 범위가 아니다(에디터에서 별도로 진행된 변경).

## 설계 요약
- 시작 버튼 `Button (Legacy)`를 **복제**해서 `SettingsButton`을 만든다. 스프라이트, 색, 버튼 색, 폰트, 글자 크기와 색이 모두 같다.
  - 위치: 시작 버튼 바로 아래. `pos=(0, 80)`, 시작 버튼(y=200, 높이 100)과 20px 간격.
  - 텍스트: `"Settings"`. 시작 버튼 "Press to Start"와 같은 영문 톤이다(아래 결정 1).
  - onClick: Title 씬 `SettingsMenu` 인스턴스의 `UIStack.OpenRoot()`. 기존 `StartLoading` 연결은 제거한다.
- 클릭하면 ESC 메뉴와 같은 설정 창(설정 / 키 설정 탭)이 열린다. ESC나 닫기로 닫는다.

## Phase 1 — 코드
- [x] `Assets/02. Scripts/UI/UIStack.cs`: `public void OpenRoot() => Push(rootPanel);` 추가. 버튼 등 외부에서 메뉴를 열 때 쓴다.
- [x] `Assets/02. Scripts/UI/KeyBindingUI.cs`: `StartRebind`에서 `EventSystem.current?.SetSelectedGameObject(null)` 호출.
  - 리바인딩 중 Space/Enter가 UI Submit으로 키 버튼을 다시 누르는 것을 방지한다(Research 참고, GameScene 메뉴에도 적용됨).

## Phase 2 — Title 씬
- [x] eval 에디터 스크립트로 Title을 additive로 열어 작업하고 저장한 뒤 닫는다.
  - `Button (Legacy)`를 Instantiate하고 이름을 `SettingsButton`으로 바꾼다. 시작 버튼 바로 다음 형제로 둔다.
  - `anchoredPosition = (0, 80)`.
  - 자식 Text를 `"Settings"`로 바꾼다.
  - onClick persistent 리스너를 비우고 `UIStack.OpenRoot`를 등록한다(`UnityEventTools.AddVoidPersistentListener`).

## Phase 3 — 검증 (MCP)
- [x] 컴파일, 콘솔 확인.
- [x] Title 플레이
  - Settings 버튼 onClick으로 메뉴가 열리는지(stack=1, timeScale 1 유지)
  - ESC/닫기로 닫히는지
  - 시작 버튼은 그대로 게임을 시작하는지
- [x] 리바인딩 후 Space 입력: Jump를 Space가 아닌 키로 바꾼 상태에서 다시 Space로 지정할 때 정상 완료되는지.
- [x] Title 캡처로 두 버튼의 배치와 디자인이 같은지 확인한다.

## Phase 4 — 문서
- [x] CLAUDE.md ESC 메뉴 항목에 "Title에는 Settings 버튼(`UIStack.OpenRoot`)으로도 연다" 추가.
- [x] Plan 상단 구현 현황.

## 확인 필요 (기본안으로 확정)
1. **버튼 글자**
   - 기본안: `Settings`(시작 버튼 "Press to Start"와 맞춤).
   - 대안: `설정`. 레거시 Text 폰트(LegacyRuntime)는 Windows에서 OS 폰트로 한글을 표시하지만, 다른 플랫폼에서 보장되지 않는다.
2. **위치**
   - 기본안: 시작 버튼 바로 아래 20px 간격(y=80).
   - Title Canvas가 ConstantPixelSize라 해상도가 낮으면 하단에 붙어 보일 수 있다. 간격을 따로 원하면 알려준다.
