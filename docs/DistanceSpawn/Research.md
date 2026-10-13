# DistanceSpawn — Research

목표: 장애물/아이템 스폰 간격을 **시간(초)** 기준에서 **이동 거리(월드 유닛)** 기준으로 변경.

## 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Map/LaneManager.cs` | 스폰 코루틴(`GenerateObjectCoroutine`, `GeneratePatternCoroutine`), `createInterval` |
| `Assets/02. Scripts/Datas/CreateObjectSO.cs` | `patternIntervalTime`(패턴 내 행 간격, 초) |
| `Assets/02. Scripts/GameManager.cs` | `mapSpeed`(월드 이동 속도) |
| `Assets/02. Scripts/Player/Player.cs` | 무적 시 `mapSpeed`를 `invincibleSpeed`로 교체 |
| `Assets/02. Scripts/Obstacles/Obstacle.cs`, `HealObject.cs` | `FixedUpdate`에서 `mapSpeed * fixedDeltaTime`만큼 이동 |
| `Assets/01. Scenes/SampleScene.unity` | `LaneManager.createInterval: 1`, `GameManager.mapSpeed: 9`, `Player.invincibleSpeed: 10` |
| `Assets/11. Datas/CreateObjectData3D.asset` | `patternIntervalTime: 0.2` |
| `Assets/11. Datas/CreateObjectData2D.asset` | `patternIntervalTime: 0.1` |

## 현재 동작

- `LaneManager.Start` → `GenerateObjectCoroutine` 무한 루프:
  1. 현재 뷰 모드로 2D/3D 데이터셋 선택
  2. `GeneratePatternCoroutine`: 랜덤 패턴의 행을 순서대로 `GenerateRow`, 행 사이 `WaitForSeconds(dataSet.patternIntervalTime)` (마지막 행 뒤엔 대기 없음)
  3. 패턴 끝나면 `WaitForSeconds(createInterval)`
- 오브젝트는 `Lane.GenerateObstacle` → 풀에서 레인 위치(카메라 상단 뷰포트)로 꺼냄 → `FixedUpdate`에서 `Rb.MovePosition`으로 `-Z` 방향 `mapSpeed * Time.fixedDeltaTime` 이동.
- 따라서 실제 오브젝트 간 거리 = `간격(초) × mapSpeed`. 현재 값 기준(mapSpeed 9):
  - 패턴 간: 1s → 9 유닛
  - 3D 행 간: 0.2s → 1.8 유닛
  - 2D 행 간: 0.1s → 0.9 유닛

## 문제

- `mapSpeed`는 런타임에 바뀜(`Player.cs:179-180`, 무적 시작 시 `invincibleSpeed`, 종료 시 원래 속도). 시간 기준이면 속도가 바뀔 때 오브젝트 간 **간격이 같이 늘어나/줄어들어** 패턴 모양이 깨짐.
- 거리 기준이면 속도와 무관하게 간격 일정(속도가 빨라지면 생성 빈도가 자동으로 증가).

## 영향 범위

- `createInterval` 사용처: `LaneManager.cs:80` 1곳. 직렬화: `SampleScene.unity` 1곳.
- `patternIntervalTime` 사용처: `LaneManager.cs:98` 1곳, 툴팁 `CreateObjectSO.cs:32`. 직렬화: 3D/2D 에셋 2곳.
- `LaneManager`의 외부 호출은 `RemoveAllLaneObjects`(`Player.cs:287,293`) 뿐 → 스폰 타이밍 변경과 무관.

## 위험 요소

- **인코딩**: `LaneManager.cs`, `CreateObjectSO.cs` 모두 CP949 + CRLF. 편집 시 인코딩/줄바꿈 유지 필요.
- **필드명 변경 시 직렬화 값 유실**: 단위가 초→유닛으로 바뀌므로 `FormerlySerializedAs`로 옛 값을 그대로 가져오면 의미가 틀림(1s → 1유닛). 새 이름으로 바꾸고 씬/에셋 값을 새로 설정해야 함.
- **스폰 타이밍 기준**: 오브젝트는 `FixedUpdate`로 이동하므로, 거리 누적도 `FixedUpdate` 단위(`WaitForFixedUpdate` + `fixedDeltaTime`)로 해야 실제 이동량과 정확히 일치. 대기 종료 시 초과 이동분을 버리면 매 간격마다 최대 1 스텝(≈0.18 유닛 @ speed 9)씩 오차 누적 → 초과분을 다음 대기로 이월해야 함.
- `mapSpeed`가 0이면 스폰도 멈춤(의도된 동작으로 판단).
