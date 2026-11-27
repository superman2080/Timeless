# 사망 시 타이틀로 이동 — Plan

근거: `docs/DeathToTitle/Research.md`
상태: **구현 완료 (2026-11-27)**

## 구현 현황
- **완료**: Phase 1~5. 단, 커밋/푸시는 요청이 없어 아직 하지 않았다.
- 컴파일 에러 0, 새 경고 0.
- `Player.cs`는 CP949/CRLF를 유지했다. diff는 해당 줄만 바뀌었다(+3/-5).
- 검증 결과
  - 2D 상태에서 강제 사망: Title로 이동, timeScale 1, pixelScale 1, 글리치 0.002.
  - Boot → Title → 게임 → ESC `타이틀로 나가기` → 예: Title로 이동.
  - Title → 게임 → 사망: Title로 이동.
- 메모
  - 게임 시작 직후 `timeScale = 0`인 시작 연출 구간이 있다(기존 동작).
  - 이 구간에 죽으면 `Invoke`가 scaled time이라 연출이 끝난 뒤 1.5초가 지나야 이동한다.
  - 같은 구간에는 ESC 메뉴도 열리지 않는다(기존 규칙).

## 설계 요약
- 사망 연출(글리치 페이드 1.5초)은 그대로 두고, 끝난 뒤 `SampleScene`을 재로드하는 대신 **Title을 로드한다**.
- "타이틀로 가기"는 `GameManager.ReturnToTitle()` 하나로 통일한다. 사망 처리와 ESC 메뉴의 `타이틀로 나가기`가 같은 함수를 호출한다.
  - `ReturnToTitle()`이 하는 일:
    1. `Time.timeScale = 1`
    2. 픽셀화 1, 글리치 `0.002f`로 리셋
    3. `PlayerPrefs.Save()`
    4. `Addressables.LoadSceneAsync("Title")`
    5. 중복 호출 방지

## Phase 1 — 공통 함수
- [x] `Assets/02. Scripts/GameManager.cs`
  - `[SerializeField] string titleSceneName = "Title"`과 `private bool returningToTitle`을 추가한다.
  - `public void ReturnToTitle()`을 추가한다(위 1~5).

## Phase 2 — 사망 처리 변경
- [x] `Assets/02. Scripts/Player/Player.cs`(CP949와 CRLF를 그대로 유지한 채 바이트 보존 편집)
  - `Invoke("Restart", 1.5f)`를 `Invoke("ReturnToTitle", 1.5f)`로 바꾼다.
  - `Restart()`를 `ReturnToTitle()`로 이름을 바꾸고 본문은 `GameManager.Instance.ReturnToTitle();` 한 줄로 한다.

## Phase 3 — ESC 메뉴 정리
- [x] `Assets/02. Scripts/UI/SettingsMenuUI.cs`
  - `ExitToTitle()` 본문을 `Stack.Clear(); GameManager.Instance.ReturnToTitle();`로 줄인다.
  - `titleSceneName` 필드를 삭제하고 Addressables using도 정리한다.
  - `allowExitToTitle` 가드는 유지한다(Title에서 싱글톤이 자동 생성되는 것을 방지).
  - 프리팹에 직렬화되어 있던 `titleSceneName` 값은 필드를 삭제하면 무시된다. 경고는 없다.

## Phase 4 — 검증 (MCP)
- [x] 컴파일, 콘솔 확인. `Player.cs`의 diff가 해당 줄만 바뀌었는지(인코딩 유지) 확인한다.
- [x] 플레이: HP를 0으로 만들어 사망시킨다. 1.5초 뒤 Title로 가는지, timeScale=1, pixelScale=1, 글리치 0.002인지 확인한다.
- [x] 2D 상태에서 사망해도 픽셀화가 리셋되는지 확인한다.
- [x] ESC 메뉴의 `타이틀로 나가기` → 예가 기존처럼 동작하는지 확인한다.
- [x] Title에서 시작 버튼을 눌러 게임이 다시 정상으로 시작되는지 확인한다.

## Phase 5 — 문서 / 커밋
- [x] CLAUDE.md "사망 시 `SampleScene` 재로드"를 "사망 시 Title로 이동(`GameManager.ReturnToTitle`)"으로 바꾼다.
- [x] Plan 상단에 구현 현황 요약.
- [ ] `dev`에 커밋하고 푸시한다(요청 시).

## 확인 필요 (기본안으로 확정: 1.5초 연출 후 바로 이동)
1. **사망 후 대기 시간**: 지금은 1.5초 글리치 연출 후 바로 Title로 간다. 이대로 둘지 정해야 한다.
   - 다른 안: 게임 오버 화면(점수 표시 + "타이틀로" 버튼)을 거쳐 가게 할지.
   - 기본안은 바로 이동이다.
2. 사망으로 Title에 돌아갔을 때 로비 BGM은 Title의 `TitleAudio`가 처음부터 재생한다(기존 Title 진입과 같다).
