# LaneSpawnZ — Research

## 문제
3D 카메라 각도를 바꾸면 레인(오브젝트 스폰 지점)의 Z 위치가 같이 바뀐다.

## 관련 파일
- `Assets/02. Scripts/Map/LaneManager.cs` — `InitLanes()` / `PlaceLanes()`
- `Assets/02. Scripts/Utils.cs` — `GetTopViewportPosition` 외 뷰포트→y=0 교차 함수 4개

## 현재 동작
1. `LaneManager.InitLanes()`가 `Start`, `Reset`, 에디터 `OnValidate`(delayCall)에서 호출된다.
2. `basePosition = Utils.GetTopViewportPosition(0.05f)`로 계산한다.
   - `Camera.main`에서 뷰포트 (0.5, 1.05) 방향으로 레이를 쏘고, y=0 평면과 만나는 점을 반환한다.
   - 교차점이 없으면 `Ray does not intersect with y=0 plane` 경고를 띄우고 `Vector3.zero`를 반환한다.
3. `PlaceLanes`는 X = `(i - center) * laneInterval`, Y = `basePosition.y`(+`upperLaneHeight`), Z = `basePosition.z`로 레인을 배치한다.

→ 레인 Z는 카메라의 pitch, 높이, Z 위치, FOV에 따라 정해진다. 이것이 원인이다.

## 현재 값 (에디터에서 SampleScene 기준으로 측정)
- Main Camera: pos (0, 4, -11), rot (40, 0, 0)
- `GetTopViewportPosition(0.05f)` = (0, 0, **19.05**), lane0 = (-1, 0, 19.05)
- `GameManager.GeneratePos` = (0, 0, 70) (트랙/배경 생성 지점이며 레인과는 별개)

## 의존 관계
- `Utils`의 함수 중 호출되는 곳은 `LaneManager.cs:56`의 `GetTopViewportPosition` 하나뿐이다.
  `GetBottom/Right/LeftViewportPosition`은 호출하는 곳이 없다.
- 레인 Z를 직접 읽는 코드: `Lane.GenerateObstacle`(레인 위치에서 생성), `Lakitu`/`Whomp`(`LaneX`, 레인 참조).
  모두 레인 Transform만 읽으므로 배치 방식이 바뀌어도 영향이 없다.

## 위험 요소
- 새 필드는 기존 씬 직렬화 데이터에 없으므로 필드 초기값이 적용된다. 초기값을 현재 값(19.05)으로 두면 동작 변화가 없다.
- 카메라를 크게 숙이면 스폰 지점이 화면 안으로 들어와 오브젝트가 갑자기 나타나 보일 수 있다. 이 경우 인스펙터에서 `spawnZ`를 조절한다.
