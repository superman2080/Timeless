# PatternData — Plan

## 구현 현황 (2026-10-14)
- **완료**: Phase 1~5 전 항목. 미완료 없음.
- 컴파일 에러/경고 0. 콘솔 에러는 MCP 파이프라인 타임아웃 로그뿐(게임 코드 무관).
- 마이그레이션: 3D 20개 / 2D 5개 패턴, 칸 121개 전부 일치.
- 계획 외 변경: "행 추가" 버튼을 격자 아래 → 격자 위로 이동, 빈 칸 색을 창 배경보다 밝게(아래 Phase 4 메모).
- 한계: 플레이 모드가 에디터 백그라운드에서 2프레임 이후 진행되지 않아 장시간 플레이 관찰은 못 함. 칸 클릭 메뉴 등 마우스 조작은 사용자 확인 필요.

근거: `docs/PatternData/Research.md`

## 설계 요약

```
CreateObjectSO (3D / 2D 데이터셋)
 ├ patterns: List<PatternEntry>
 │    └ PatternEntry { PatternDataSO pattern; float percent; }   ← 등장 확률(%), 합 100
 └ rowDistance: float

PatternDataSO (패턴 1개 = 에셋 1개)
 ├ viewMode: ViewMode              ← View2D면 에디터에서 레인 2만 표시
 └ rows: List<PatternRow>          ← rows[0]이 가장 먼저 생성 (에디터에서 맨 아래)
      └ PatternRow { ObjectType[] lower = new ObjectType[3]; ObjectType[] upper = new ObjectType[3]; }
```

- **격자 모델**: 행 × 레인(3 고정, `PatternDataSO.LaneCount = 3` 상수) × {하단, 상단} 칸마다 ObjectType 1개. 기존 데이터는 칸 중복이 없어 손실 없이 변환됨(Research 참고).
- **확률**: 패턴마다 %를 직접 입력, 합이 100이어야 함. 인스펙터에 합계 표시, 100이 아니면 빨간 경고 + "100%로 맞추기"(비율 유지하며 스케일) 버튼. 런타임은 합이 100이 아니어도 동작하도록 합계 기준으로 뽑음(안전장치).
- **2D 패턴**: 2D에선 플레이어가 레인 2에 고정 → `viewMode == View2D`인 패턴은 에디터에서 레인 2 열만 그림. 런타임 생성 로직은 3D와 동일(다른 레인 칸은 비어 있음).

## 에디터 창 레이아웃

```
┌ Pattern Editor ──────────────────────────────────────┐
│ Pattern: [Pattern3D_05 (PatternDataSO) ◎]  [새 패턴]  │
│ View Mode: [3D ▼]                                    │
│                                                      │
│            Lane 0     Lane 1     Lane 2              │
│ Row 2   상 │          │          │   HP     │ [⧉][✕] │  ← 나중에 도착 (멀리)
│         하 │ BARREL   │          │ BARREL   │        │
│ Row 1   상 │          │          │          │ [⧉][✕] │
│         하 │          │  LASER   │          │        │
│ Row 0   상 │          │          │          │ [⧉][✕] │  ← 먼저 도착 (가까이)
│         하 │ BARREL   │          │ BARREL   │        │
│                                  [+ 행 추가 (위로)]   │
└──────────────────────────────────────────────────────┘
```
- 칸 = 버튼. **비어 있으면 아무 글자도 출력하지 않음**(빈 회색 칸). 채워진 칸은 타입별 고정 색 + 타입명.
- **칸 클릭 → 드롭다운 메뉴(`GenericMenu`)**: ObjectType 목록(NONE 제외)에서 골라 넣기. 채워진 칸을 클릭하면 같은 메뉴 + 맨 위 "비우기". 우클릭 = 바로 비우기.
- 상단 칸은 하단 칸보다 연한 색/얇은 높이로 구분, 행 왼쪽에 `상`/`하` 라벨.
- **행 추가 = 맨 위에 쌓임**(`rows.Add` → 가장 늦게 도착하는 행). 행별 버튼: 복제(바로 위에 삽입) / 삭제.
- 2D 패턴이면 Lane 2 열만 표시.
- 모든 수정은 `Undo.RecordObject` + `SetDirty` → Ctrl+Z 지원.
- `PatternDataSO` 에셋 더블클릭 시 이 창으로 열림(`OnOpenAsset`). `PatternDataSO` 인스펙터에는 "Pattern Editor 열기" 버튼 + 읽기 전용 미리보기 격자.

## CreateObjectSO 인스펙터

```
Row Distance  [1.8]
Patterns                                    합계 100.0% ✔
 ≡ [Pattern3D_00 ◎]  [ 5.0] %  [편집]
 ≡ [Pattern3D_01 ◎]  [15.0] %  [편집]
 ...
 [+]  [-]          [100%로 맞추기]  [새 패턴 만들어 추가]
```
- `ReorderableList`. `[편집]` = Pattern Editor에서 해당 패턴 열기.
- 합계 ≠ 100 → 합계 글자 빨강 + HelpBox 경고. 0%는 회색(등장 안 함), 패턴 비어 있는 항목은 경고 색.
- "새 패턴 만들어 추가": 저장 위치 선택 → 빈 패턴(1행) 생성, viewMode는 **이 데이터셋에 이미 있는 패턴들의 viewMode**를 따라감(없으면 3D) → 목록에 0%로 추가.

## Phase 1 — 새 데이터 타입 추가 (옛 필드는 유지)
- [x] `Assets/02. Scripts/Datas/PatternDataSO.cs` 신규 (UTF-8): `const int LaneCount = 3`, `PatternRow`(길이 3 배열 2개), `PatternDataSO`(`viewMode`, `rows`). `[CreateAssetMenu]`.
- [x] `CreateObjectSO.cs`: `PatternEntry { PatternDataSO pattern; [Range(0,100)] float percent; }`, `List<PatternEntry> patterns`, `PatternDataSO PickRandom()`(누적 % 기준, 합 0/비어 있으면 null) 추가. 옛 `objectSpawnDatas`와 옛 타입은 **아직 유지**.
- [x] MCP: 컴파일 에러 0 확인.

## Phase 2 — 기존 데이터 마이그레이션 (1회성, MCP eval로 실행 — 코드로 남기지 않음)
- [x] 3D 20개 → `Assets/11. Datas/Patterns/3D/Pattern3D_00~19.asset`(viewMode 3D), 2D 5개 → `Assets/11. Datas/Patterns/2D/Pattern2D_00~04.asset`(viewMode 2D) 생성. 각 `(laneIndex, upper, type)`을 격자 칸으로 변환.
- [x] 각 `CreateObjectSO.patterns`에 순서대로 등록, 확률 균등(3D 각 5%, 2D 각 20%) = 현재 균등 랜덤과 동일.
- [x] 검증: 옛 데이터와 새 격자를 다시 비교해 칸 수/타입 전부 일치하는지 eval로 확인.

## Phase 3 — 런타임 전환 + 옛 구조 삭제
- [x] `LaneManager.cs` (CP949 유지): `GeneratePatternCoroutine`이 `dataSet.PickRandom()` 사용, null이면 `WaitDistance(patternDistance)`만 하고 넘어감. `GenerateRow(PatternRow)`로 변경 — `lower[i]`는 `lanes[i]`, `upper[i]`는 `upperLanes[i]`, 범위 밖/NONE 스킵.
- [x] `CreateObjectSO.cs`: `ObjectSpawnData`, `ObjectSpawnRow`, `ObjectSpawnDataSet`, `objectSpawnDatas` 삭제. 파일 대부분을 새로 쓰므로 UTF-8로 변환.
- [x] 두 데이터 에셋 재저장 → YAML에서 `objectSpawnDatas` 키 사라졌는지 확인.
- [x] MCP: 컴파일 에러 0, 씬의 `LaneManager.objectDataSet3D/2D` 참조 유지 확인.

## Phase 4 — 에디터 (`Assets/02. Scripts/Editor/`, UTF-8)
- [x] `PatternEditorWindow.cs`: 위 레이아웃. 메뉴 `Tools/Timeless/Pattern Editor`, `Open(PatternDataSO)` 정적 메서드, `[OnOpenAsset]` 더블클릭 연결. 빈 칸 무출력, 클릭 시 `GenericMenu`로 아이템 선택, 우클릭 비우기, 행 추가는 위로 쌓임, 2D면 레인 2만 표시, viewMode를 2D로 바꾸면 레인 0/1 칸 비움. "새 패턴" = 저장 위치 선택 후 1행 빈 패턴 생성.
  - 메모: 더블클릭은 Unity 6.6의 `OnOpenAsset(EntityId, int)` 시그니처 사용. "행 추가" 버튼은 창 맨 아래에서 잘려 보여 격자 **위**로 옮김(행이 위로 쌓이는 방향과도 맞음). 빈 하단 칸이 창 배경색과 같아 안 보여서 빈 칸 색을 0.3 회색으로 변경.
- [x] 격자 그리기 함수를 `static`(편집 가능 여부 인자)으로 두어 `PatternDataSO` 인스펙터 미리보기에서 재사용.
- [x] `PatternDataSOEditor.cs`: "Pattern Editor 열기" 버튼 + 읽기 전용 미리보기.
- [x] `CreateObjectSOEditor.cs`: `ReorderableList` + % 입력 + 합계/경고 + "100%로 맞추기" + `[편집]` + "새 패턴 만들어 추가".
- [x] MCP: 컴파일 에러 0. 창을 열어 마이그레이션된 3D/2D 패턴이 올바르게 표시되는지 스크린샷 확인(확인 후 이미지 삭제).
  - 메모: `InternalEditorUtility.ReadScreenPixel`로 창/인스펙터 캡처(스크래치 폴더, 프로젝트에 파일 안 남김). 3D 패턴(LASER/LASER/HP) 3열, 2D 패턴 레인 2만 표시, 인스펙터 합계 100% 녹색 확인.

## Phase 5 — 검증
- [x] 편집 모드 eval: `PickRandom()`을 10만 번 호출해 입력한 %와 실제 빈도가 맞는지 확인(테스트용으로 25/75 설정 후 원복).
  - 메모: 25/75 → 24.9%/75.1%. 0% 항목·null 패턴은 한 번도 안 뽑힘, 빈 목록은 null. 임시 인스턴스로 테스트해 원복 불필요. `Clear2DHiddenLanes`도 레인 0/1만 비우는 것 확인.
- [x] 플레이 가능하면 3D/2D 패턴 생성 확인, 불가하면 Plan에 기록.
  - 메모: 플레이 진입 후 새 경로로 하단/상단 오브젝트 생성 확인(Blockade/Laser/HP, upper=True 포함), 에러 없음. 장시간 관찰은 에디터가 진행을 멈춰 불가.
- [x] 콘솔 신규 에러/경고 0.
- [x] `CLAUDE.md`의 "레인 & 스폰" 섹션을 새 구조로 갱신(새 장애물 추가 절차의 "패턴 에셋에 배치" 부분 포함).

## 결정 사항
- viewMode를 2D로 바꾸면 레인 0/1 칸을 자동으로 비움.
