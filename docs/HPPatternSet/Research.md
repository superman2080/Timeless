# HPPatternSet — Research

목표: 뷰 모드(2D/3D)만으로 고르던 스폰 데이터셋(`CreateObjectSO`)을 **HP 비율 구간별로** 고를 수 있게 한다. (저체력에서 회복 패턴 비중을 높이는 등)

## 관련 파일

| 경로 | 역할 |
|---|---|
| `Assets/02. Scripts/Map/LaneManager.cs` (CP949) | `objectDataSet3D`/`objectDataSet2D` 필드, 스폰 루프 |
| `Assets/02. Scripts/Datas/CreateObjectSO.cs` (UTF-8) | 패턴 목록 + 확률, `rowDistance`, `PickRandom()` |
| `Assets/02. Scripts/Editor/CreateObjectSOEditor.cs` | `CreateObjectSO` 인스펙터 (합계 %, 정규화, 새 패턴) |
| `Assets/02. Scripts/Player/Player.cs` (CP949) | `Stat<Player> stat` (public get), `HP`/`MaxHP` public get, `CheckChangeView()` |
| `Assets/02. Scripts/GameManager.cs` | `player`, `laneManager`, `currentViewMode`, `threshold2DView=0.5`, `threshold3DView=0.7` |
| `Assets/01. Scenes/SampleScene.unity` | LaneManager(`&690753020`)에 `objectDataSet3D: CreateObjectData3D`(guid 5a9b…), `objectDataSet2D: CreateObjectData2D`(guid f463…) |
| `Assets/11. Datas/CreateObjectData3D/2D.asset`, `DebugCreateObject3D/2D.asset` | 데이터셋 에셋 (Debug는 인스펙터에서 바꿔 끼워 쓰는 용도로 보임) |

`objectDataSet*` 필드를 참조하는 씬/프리팹은 `SampleScene.unity`뿐이다 (grep 확인).

## 현재 흐름

```
LaneManager.Start → GenerateObjectCoroutine (무한 루프)
  ├ currentViewMode == View2D ? objectDataSet2D : objectDataSet3D
  ├ GeneratePatternCoroutine(dataSet)
  │    pattern = dataSet.PickRandom()      // 패턴 시작 시점에 1회 선택
  │    행마다 GenerateRow → WaitDistance(dataSet.rowDistance)
  └ WaitDistance(patternDistance)
```

- 데이터셋은 **패턴을 시작할 때마다** 다시 고른다. 그래서 HP 구간이 바뀌면 다음 패턴부터 바로 적용된다 (진행 중인 패턴은 끝까지 감).
- HP 비율 = `stat.HP / stat.MaxHP` (`CheckChangeView`는 `Mathf.InverseLerp(0, MaxHP, HP)`를 쓴다). `MaxHP`는 형태(`PlayerFormSO`)에 따라 바뀐다.
- 뷰 전환에는 히스테리시스가 있다: 3D는 HP 0.5까지, 2D는 HP 0.7까지 유지된다. 그래서 **3D 구간은 0.5~1.0, 2D 구간은 0~0.7** 범위에서만 의미가 있다.
- 뷰가 바뀌면 `RemoveAllLaneObjects()`로 레인 오브젝트를 전부 회수한다.

## 2D/3D 전환 임계값

- `GameManager.threshold2DView`(0.5), `threshold3DView`(0.7): `[Range(0,1)]` public 필드. 씬 값도 0.5 / 0.7 (`SampleScene.unity:4895`).
- 이 값을 읽는 곳은 `Player.CheckChangeView()` 한 곳뿐이다 (`Player.cs:257, 263`, `GameManager.Instance.threshold*`로 접근).
- 초기화 순서: `GameManager`(`[DefaultExecutionOrder(-100)]`)의 `Start`가 `laneManager`를 찾고, 그다음 `Player.Start`에서 `stat`을 만들고 `onHPChanged → CheckChangeView`를 구독한다. 그래서 `CheckChangeView`가 처음 불릴 때는 이미 `laneManager`가 연결되어 있다.
- 인코딩: `GameManager.cs`는 UTF-8, `LaneManager.cs`와 `Player.cs`는 CP949다.

## 위험 요소

- `LaneManager.cs`는 CP949라서 Edit 도구로 고칠 수 없다. 인코딩을 유지한 채 셸로 수정하거나, 새 코드는 UTF-8 파일에 넣어야 한다.
- 필드 두 개를 리스트 하나로 바꾸면 기존 씬 직렬화 값을 `FormerlySerializedAs`로 옮길 수 없다. 구현 후 MCP로 씬 값을 다시 넣어야 한다.
- HP가 0일 때도 비율이 정의되므로 0 나눗셈은 없다 (`MaxHP > 0` 보장, `SetMaxHP`가 0 이하 거부).
