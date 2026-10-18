# Score — Plan

> 상태: **구현 완료** (Phase 1~3 전부). 플레이 검증 통과, 신규 콘솔 에러/경고 0.

## 구현 현황 요약

완료
- Phase 1 `Assets/02. Scripts/UI/ScoreUI.cs`
- Phase 2 `Canvas/Score` (TextMeshProUGUI 60pt + ScoreUI + EventListener), anchor (1,1) / pivot (1,0.5) / pos (-50,-100) / size 400x100 — HPBar와 같은 높이. 씬 저장.
- Phase 3 검증: 3.63초에 364점(초당 100), 999→1000 통과 시 강조 발동(scale 1.2에서 감소 확인), 사망 시 `ScoreUI.enabled=false`로 정지, 재로드 후 0부터 다시 시작

미완료: 없음

참고
- 콘솔 에러 1건은 MCP eval 타임아웃 로그(게임 코드 무관). 경고는 기존 배경 프리팹 Missing Script뿐.
- 강조 확인 중에만 리플렉션으로 `punchDuration`을 5초로 늘렸고 씬/에셋에는 반영되지 않음.
- 피드백 반영: 확대 배율을 %(`punchScalePercent` 120) → 배율(`punchScale`, 기본 1.3배)로 변경, 씬 재저장.
- 확인할 점 1~3은 기본안(1000점 간격 / 선형 복귀 / 숫자만)으로 진행.

## 설계 요약
- 컴포넌트 1개 `ScoreUI`가 점수 누적 + 표시 + 강조를 모두 담당 (ScoreManager 분리 안 함 — 점수를 읽는 다른 시스템이 아직 없음).
- 점수: `score += scorePerSecond × Time.deltaTime` (float 누적, 정수로 표시).
- 강조: 점수가 `milestoneInterval`의 배수를 넘을 때마다 크기를 `punchScale`배로 즉시 키운 뒤 `punchDuration` 동안 100%로 선형 복귀. 강조 중 또 넘으면 다시 최대 크기부터 시작.
- 사망 시 정지: `EventListener`(onPlayerDied) → `ScoreUI.enabled = false` 인스펙터 연결. 재로드 시 씬 오브젝트라 자동 0 리셋.

### 인스펙터 값 (기본값)
| 필드 | 기본값 | 설명 |
|---|---|---|
| `scorePerSecond` | 100 | 초당 획득 점수 |
| `milestoneInterval` | 1000 | 강조가 발생하는 점수 간격 |
| `punchScale` | 1.3 | 강조 확대 배율(배) |
| `punchDuration` | 0.3 | 원래 크기로 돌아오는 시간(초) |

## Phase 1 — 스크립트
- [x] `Assets/02. Scripts/UI/ScoreUI.cs` 신규 (UTF-8)
  - 필드: `TMP_Text scoreText`, 위 표의 4개 값, `public int Score` 읽기 프로퍼티
  - `Update`: 점수 누적 → 이전/현재 `score / milestoneInterval` 비교로 강조 트리거 → 텍스트 갱신 → `localScale = Lerp(1, punchScale, 남은시간/punchDuration)`

## Phase 2 — 씬 UI (SampleScene)
- [x] `Canvas` 아래 `Score` 오브젝트 생성 (`TextMeshProUGUI` + `ScoreUI` + `EventListener`)
  - RectTransform: 앵커/pivot 우상단 기준 — anchor (1,1), pivot (1, 0.5), 화면 우상단에서 여백을 둔 위치
  - TMP Alignment: Horizontal **Right**, Vertical **Middle**
  - 폰트: `LiberationSans SDF`, 기본 텍스트 `0`
  - `EventListener.eventChannel` = onPlayerDied 채널, response = `ScoreUI.enabled` false
- [x] 씬 저장

## Phase 3 — MCP 검증
- [x] 컴파일/콘솔 에러 확인
- [x] 플레이: 점수 초당 100 증가, 1000마다 확대 → 원래 크기 강조, 사망 시 정지, 재시작 시 0
- [x] 게임뷰 캡처로 우상단 배치 확인 (캡처 파일 삭제)

## 확인할 점
1. **"특정 점수"** 를 "N점마다(기본 1000)"로 해석했습니다. 1000/5000/10000처럼 고정 목록이 필요하면 알려주세요.
2. 복귀 곡선은 선형. 튕기는 느낌을 원하면 `AnimationCurve`로 바꿀 수 있습니다.
3. 점수 표시 형식: 그냥 숫자(`12345`). 천 단위 콤마(`12,345`)나 자릿수 고정(`000123`)이 필요하면 알려주세요.
