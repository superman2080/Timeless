# PatternData — Research

목표: 스폰 패턴(묶음) 하나를 별도 `PatternDataSO` 에셋으로 분리하고, `CreateObjectSO`는 패턴 목록 + 패턴별 등장 확률만 관리. 패턴은 레인/상하단을 한눈에 보며 편집하는 전용 에디터 창으로 수정.

## 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Datas/CreateObjectSO.cs` | `ObjectSpawnData`(laneIndex, type, upper) / `ObjectSpawnRow` / `ObjectSpawnDataSet` / `CreateObjectSO`(`objectSpawnDatas`, `rowDistance`) 정의. CP949 + CRLF |
| `Assets/02. Scripts/Map/LaneManager.cs` | 유일한 사용처. 패턴 선택(`GeneratePatternCoroutine`), 행 생성(`GenerateRow`). CP949 + CRLF |
| `Assets/02. Scripts/Obstacles/InteractionObject.cs` | `ObjectType` enum: `NONE, LASER, BARREL, BLOCKADE, FAN, ROBOT_ARM, HP, KEY_CARD` |
| `Assets/11. Datas/CreateObjectData3D.asset` | 20 패턴 / 59 행 |
| `Assets/11. Datas/CreateObjectData2D.asset` | 5 패턴 / 14 행 |
| `Assets/01. Scenes/SampleScene.unity` | `LaneManager.objectDataSet3D/2D`가 위 두 에셋 참조 |

## 현재 데이터 구조

```
CreateObjectSO
 ├ objectSpawnDatas: List<ObjectSpawnDataSet>   ← 패턴 목록 (인라인)
 │   └ rows: List<ObjectSpawnRow>               ← 행 (rowDistance 간격으로 순서대로 생성)
 │       └ spawnDatas: List<ObjectSpawnData>    ← (laneIndex, type, upper)
 └ rowDistance: float
```

- 인스펙터에서 3단 중첩 리스트를 펼쳐 `laneIndex`/`upper`를 숫자·체크박스로 입력해야 함 → 어느 레인 위/아래에 무엇이 있는지 시각적으로 알 수 없음.
- 패턴 선택은 균등 랜덤(`Random.Range(0, Count)`), 확률 조절 수단 없음.

## 현재 동작 (LaneManager)

- `GeneratePatternCoroutine(dataSet)`: 랜덤 패턴 → 행마다 `GenerateRow(row.spawnDatas)` → 마지막 행 제외 `WaitDistance(dataSet.rowDistance)`.
- `GenerateRow`: `type == NONE` 스킵, `upper`면 `upperLanes`, 아니면 `lanes`에서 `laneIndex` 범위 체크 후 `Lane.GenerateObstacle(type)`.
- 레인 수: 씬 기준 하단 3 + 상단 3 (`LaneManager.lanes`, `upperLanes`). 2D 모드에선 플레이어가 마지막 레인(index 2)에 고정 → 2D 패턴은 전부 laneIndex 2만 사용.

## 기존 데이터 분석 (마이그레이션 근거)

- 모든 laneIndex는 0~2 범위.
- **한 행 안에서 (laneIndex, upper)가 중복되는 칸 없음** (3D/2D 전수 확인) → "행 × 레인 × {하단, 상단}" 격자(칸당 ObjectType 1개)로 손실 없이 변환 가능.
- 3D 패턴 행 수: 19개가 3행, 1개가 2행. 2D: 4개 3행, 1개 2행.
- `type: 0`(NONE) 칸이 67개 존재 — 빈 칸 표시용. 격자 모델에선 그냥 빈 칸.
- 상단(`upper: 1`) 사용: 3D 3칸, 2D 2칸.

## 에디터 인프라

- 프로젝트 자체 에디터 스크립트 없음(`Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`만 존재, 템플릿).
- asmdef 없음 → `Editor` 이름 폴더에 두면 자동으로 `Assembly-CSharp-Editor`로 컴파일됨.

## 위험 요소

- **직렬화 구조 변경 시 데이터 유실**: `objectSpawnDatas`를 바로 지우면 기존 25개 패턴이 사라짐. 새 필드 추가 → 변환 → 검증 → 옛 타입 삭제 순서로 해야 함.
- **인코딩**: `CreateObjectSO.cs`, `LaneManager.cs`는 CP949. `CreateObjectSO.cs`는 거의 전부 다시 쓰게 됨.
- 새 에셋은 `.meta`와 함께 생성되므로 GUID 참조는 Unity API(`AssetDatabase.CreateAsset`)로만 생성.
- 확률이 전부 0이거나 목록이 비면 패턴 선택 불가 → 런타임 가드 필요.
