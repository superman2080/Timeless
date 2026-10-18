# HPPatternSet — Plan

근거: [Research.md](Research.md)

## 구현 현황 (2026-10-19)

- **완료**: Phase 1~3 전 항목.
- **미완료**: 없음.
- **계획과 달라진 점**
  - SampleScene은 원래 `DebugCreateObject3D/2D`를 쓰고 있었다(커밋된 씬도 같음). 그래서 씬에는 `DebugPatternSet.asset`(Debug 데이터셋)을 연결해 지금과 같은 동작을 유지했다. `PatternSet.asset`(일반 `CreateObjectData3D/2D`)은 따로 만들어 두었다. 실제 플레이용으로 바꾸려면 LaneManager의 `patternSet`을 `PatternSet.asset`으로 바꾸면 된다.
  - `LaneManager.Start`가 `Player.Start`보다 먼저 실행되면 `player.stat`이 아직 null이다. 이때는 HP 비율을 1로 처리하도록 null 처리를 추가했다.
  - 경계값 정확히 일치(예: 3D 전체 HP 0.6, 상한 0.2)일 때는 float 오차로 위 구간이 선택될 수 있다. HP는 연속적으로 변하므로 게임 영향은 없다고 판단해 그대로 두었다.

## 설계

### 새 에셋 `PatternSetSO`
2D/3D 전환 임계값과 HP 구간별 데이터셋을 **SO 에셋 하나**에 모은다. `CreateObjectSO`(패턴과 확률)는 그대로 쓴다.

```csharp
// Assets/02. Scripts/Datas/PatternSetSO.cs (새 파일, UTF-8)
[Serializable]
public class PatternSetTier
{
    [Tooltip("뷰 모드 안의 HP 비율(0~1)이 이 값 이하일 때 사용")]
    [Range(0, 1)] public float maxRatio = 1f;
    public CreateObjectSO dataSet;
}

[CreateAssetMenu(fileName = "PatternSet", menuName = "Scriptable Objects/PatternSet")]
public class PatternSetSO : ScriptableObject
{
    [Tooltip("3D → 2D 전환: 전체 HP 비율이 이 값 이하가 되면")]
    [Range(0, 1)] public float threshold2DView = 0.5f;
    [Tooltip("2D → 3D 전환: 전체 HP 비율이 이 값 이상이 되면")]
    [Range(0, 1)] public float threshold3DView = 0.7f;

    public List<PatternSetTier> tiers3D = new();
    public List<PatternSetTier> tiers2D = new();

    // 전체 HP 비율 → 해당 뷰 모드 안의 0~1 비율
    public float ToLocalRatio(ViewMode mode, float hpRatio) => mode == ViewMode.View2D
        ? Mathf.InverseLerp(0f, threshold3DView, hpRatio)
        : Mathf.InverseLerp(threshold2DView, 1f, hpRatio);

    // 로컬 비율 이하 조건을 만족하는 구간 중 maxRatio가 가장 작은 것
    public CreateObjectSO Select(ViewMode mode, float hpRatio)
    {
        float local = ToLocalRatio(mode, hpRatio);
        PatternSetTier best = null;
        foreach (var t in mode == ViewMode.View2D ? tiers2D : tiers3D)
            if (t.dataSet != null && local <= t.maxRatio && (best == null || t.maxRatio < best.maxRatio))
                best = t;
        return best?.dataSet;
    }
}
```

### 로컬 비율 ↔ 전체 HP 비율 변환
각 뷰 모드가 실제로 유지되는 HP 범위를 0~1로 늘려서 쓴다.

| 뷰 모드 | 유지되는 전체 HP 범위 | 로컬 0 | 로컬 1 | 변환 |
|---|---|---|---|---|
| 2D | 0 ~ `threshold3DView` | HP 0 | HP 0.7 | 전체 = 로컬 × 0.7 |
| 3D | `threshold2DView` ~ 1 | HP 0.5 | HP 1.0 | 전체 = 0.5 + 로컬 × 0.5 |

예시: 2D에서 `maxRatio 0.5`는 전체 HP 0.35 이하, 3D에서 `maxRatio 0.5`는 전체 HP 0.75 이하를 뜻한다.
임계값을 바꿔도 구간은 비율대로 함께 늘어나거나 줄어들기 때문에 구간 값을 다시 잡을 필요가 없다.

### 임계값 이동
- `GameManager`에 직렬화된 `threshold2DView`/`threshold3DView` 필드를 지우고, 같은 이름의 읽기 전용 프로퍼티로 바꿔 `laneManager.patternSet`의 값을 그대로 넘긴다.
  → `Player.cs`(CP949)의 `CheckChangeView`는 **수정하지 않는다.**
- 초기화 순서상 `CheckChangeView`가 처음 불릴 때는 이미 `laneManager`가 연결되어 있다 (Research 참고).

### 예시 설정 (`PatternSet.asset`)
| 리스트 | maxRatio | 전체 HP 환산 | dataSet |
|---|---|---|---|
| tiers3D | 1.0 | ≤ 1.0 | CreateObjectData3D |
| tiers3D | 0.2 | ≤ 0.6 | (저체력 3D, 추후) |
| tiers2D | 1.0 | ≤ 0.7 | CreateObjectData2D |
| tiers2D | 0.5 | ≤ 0.35 | (저체력 2D, 추후) |

- 리스트 순서는 상관없다. 조건을 만족하는 구간 중 가장 좁은 것이 선택된다.
- 맞는 구간이 없으면 그 패턴 차례를 건너뛴다. 지금 `PickRandom()`이 null을 반환할 때와 같은 동작이다.
- 데이터셋은 패턴을 시작할 때마다 다시 고르므로, 구간이 바뀌면 다음 패턴부터 바로 적용된다.

### 인스펙터 (`PatternSetSOEditor`)
입력하는 값은 `maxRatio`(상한) 하나지만, 하한은 **같은 리스트에서 바로 아래 구간의 상한**으로 정해진다. 그래서 각 항목이 실제로 쓰이는 범위를 계산해서 함께 보여준다.

```
 2D → 3D 전환 (threshold3DView)  [=======|---] 0.7
 3D → 2D 전환 (threshold2DView)  [=====|-----] 0.5

 ▼ 3D 구간 (전체 HP 0.50 ~ 1.00)
 ┌─────────────────────────────────────────────────────────────┐
 │ 상한 [====|------] 0.20   [CreateObjectData3D_LowHP   ◎]   │
 │   로컬 0.00 ~ 0.20  →  전체 HP 0.50 ~ 0.60                  │
 │ 상한 [==========] 1.00   [CreateObjectData3D         ◎]   │
 │   로컬 0.20 ~ 1.00  →  전체 HP 0.60 ~ 1.00                  │
 └─────────────────────────────────────────────────────────────┘
 [ 0.5 ▒▒▒▒▒▒|██████████████████████████████████████ 1.0 ]   ← 구간 막대
 ⚠ (경고가 있을 때만)

 ▼ 2D 구간 (전체 HP 0.00 ~ 0.70)
   ... 같은 형태 ...
```

- 항목마다 두 줄로 그린다.
  - 1줄: 상한 슬라이더와 데이터셋 필드.
  - 2줄: `로컬 a ~ b → 전체 HP A ~ B`. 하한 a는 같은 리스트에서 상한이 더 작은 항목 중 가장 큰 상한값이고, 없으면 0이다.
- 리스트 아래에 **구간 막대**를 하나 둔다. 0~1 범위를 각 항목 구간 비율로 나눠 칠하고, 칸마다 데이터셋 이름을 쓴다(칸이 좁으면 생략). 칸 색은 항목 순서에 따라 정한다.
- 경고(HelpBox)를 띄우는 경우:
  - 상한 1.0인 항목이 없어서 위쪽 HP 구간에 패턴이 나오지 않을 때
  - 상한이 같은 항목이 둘 이상이라 앞의 것만 쓰일 때
  - 데이터셋이 비어 있을 때
- 항목은 `ReorderableList`로 그린다. `CreateObjectSOEditor`와 같은 방식이다.
- 계산에 필요한 값은 SO에서 가져온다.
  - `PatternSetSO`에 `ToGlobalRatio(mode, local)`를 추가한다. `ToLocalRatio`의 역변환으로 2D는 `Lerp(0, t3D, local)`, 3D는 `Lerp(t2D, 1, local)`이다.
  - 하한 계산은 `GetLowerBound(list, tier)`로 런타임 `Select`와 같은 규칙을 쓴다.

## Phase 1 — 코드

- [x] `Assets/02. Scripts/Datas/PatternSetSO.cs` 새로 작성 (위 코드)
- [x] `Assets/02. Scripts/Map/LaneManager.cs` (**CP949 유지**, 셸로 수정):
  - `objectDataSet3D`/`objectDataSet2D` 필드 → `public PatternSetSO patternSet;`
  - `GenerateObjectCoroutine`: 선택 부분 → `patternSet.Select(GameManager.Instance.currentViewMode, stat.HP / stat.MaxHP)` (`stat` = `GameManager.Instance.player.stat`)
  - `GeneratePatternCoroutine`: `dataSet == null`이면 `yield break`
- [x] `Assets/02. Scripts/GameManager.cs`: `threshold2DView`/`threshold3DView` 필드 → `public float threshold2DView => laneManager.patternSet.threshold2DView;` (3D도 동일하게)
- [x] `PatternSetSO.cs`에 `ToGlobalRatio`, `GetLowerBound` 추가
- [x] `Assets/02. Scripts/Editor/PatternSetSOEditor.cs` 새로 작성: 임계값 필드, 3D/2D 리스트(항목마다 구간 표시), 구간 막대, 경고
- [x] 컴파일 → 콘솔 에러 0 확인 (MCP)

## Phase 2 — 에셋 / 씬

- [x] `Assets/11. Datas/PatternSet.asset` 생성: 임계값 0.5 / 0.7, `tiers3D: 1.0 → CreateObjectData3D`, `tiers2D: 1.0 → CreateObjectData2D` (지금과 동작 동일)
- [x] SampleScene LaneManager의 `patternSet`에 연결하고 씬 저장
  - 실제로는 `DebugPatternSet.asset`을 연결함 (기존 씬이 Debug 데이터셋을 쓰고 있었음)
- [x] git diff 확인: LaneManager의 `objectDataSet3D/2D`가 `patternSet`으로 바뀌고, GameManager의 `threshold*` 줄이 사라졌는지

## Phase 3 — 검증

- [x] 플레이 모드: 3D에서 스폰 정상, HP 0.5 이하에서 2D로 전환, 0.7 이상에서 3D로 복귀 (임계값이 에셋에서 읽히는지)
- [x] `ToLocalRatio` 확인 (eval): 2D·0.35 → 0.5, 3D·0.75 → 0.5, 3D·0.4 → 0 (클램프)
- [x] 임시로 `tiers3D: 0.6 → DebugCreateObject3D`를 넣어 전체 HP 0.8 이하에서 데이터셋이 바뀌는지 확인한 뒤 제거
  - 씬이 Debug 셋을 쓰고 있어서 반대로 `0.6 → CreateObjectData3D`를 넣고 확인했다. HP 1.0에서는 Debug, 0.78에서는 CreateObjectData3D가 선택됐고, 확인 후 제거했다.
- [x] 인스펙터 확인: 예시 설정(3D 0.2/1.0, 2D 0.5/1.0)에서 표시값이 `3D: 0.50~0.60 / 0.60~1.00`, `2D: 0.00~0.35 / 0.35~0.70`인지 확인한다. 임계값을 바꾸면 표시값도 함께 바뀌는지, 경고가 조건대로 뜨는지 본다. 확인용 항목은 끝나면 되돌린다.
  - 임시 에셋으로 캡처해서 구간 값, 막대, 경고 3종이 정상인 것을 확인했고, 임시 에셋은 삭제했다. 임계값 변경에 따른 표시 갱신은 따로 조작해 보지 않았다(표시값은 같은 `ToGlobalRatio`로 계산된다).
- [x] 콘솔에 새 에러/경고 없는지 확인 (기존 Missing Script, `Ray does not intersect` 경고는 제외)

## 제외한 것

- **저체력 데이터셋 에셋 제작/밸런싱**: 기획 판단이라 뺐다. 구조가 완성되면 `CreateObjectData3D`를 복제해서 확률만 조정하면 된다.

## 확정 사항

- 구간 범위: 3D는 전체 HP 0.5~1, 2D는 전체 HP 0~0.7 (사용자 확인).
- 전체 HP 비율 = `HP / 현재 MaxHP`로 계산한다. `CheckChangeView`와 같은 기준이다. 현재 형태는 `Form_Normal`(maxHP 100) 하나뿐이라 MaxHP가 바뀌는 경우가 없다. MaxHP를 바꾸는 형태를 추가할 때 다시 검토한다.
