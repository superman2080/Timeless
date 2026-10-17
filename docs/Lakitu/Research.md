# Lakitu (김수한무, 구름 탄 투척 적) 적 — Research

작업 목표: 패턴으로 생성되는 새 적 Lakitu. 생성 후 약 10초 동안 자유롭게 날아다니고, 2초마다 랜덤 레인에 스파이크 볼을 떨어뜨린 뒤 사라진다. 또 하단 레인에는 굼바만 생성되도록 제한한다.

## 1. 관련 에셋 (확인한 사실)

| 종류 | 경로 | 비고 |
|---|---|---|
| 모델 FBX | `Assets/03. Models/Skeletal/Lakitu.fbx` | Generic 리그, scale 1. 구름 포함 높이 약 1m. **+Z를 바라봄** |
| 모델 프리팹 | `Assets/07. Prefabs/Enemy/Lakitu.prefab` | FBX의 Variant. Animator(`Lakitu.controller`)는 모델 루트에 있고 applyRootMotion은 꺼짐 |
| 컨트롤러 | `Assets/04. Animations/Lakitu.controller` | Idle(기본, 2s 루프) → Attack(Trigger `Attack`, 2.958s) → exit time 1에서 Idle로 |
| 공 데이터 | 메모리 `zombie-bee-attack-ball` | 공 최종 반지름 0.22m. Blender 51f(임포트 기준 50f, 24fps)에 손을 떠남 → 클립 기준 **약 2.08s**. 리그에 `Ball` 본(Root 자식)이 남아 있어 소켓으로 쓸 수 있음 |
| 장애물 래퍼 예시 | `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Goomba.prefab` | 루트에 BoxCollider(trigger), `Obstacle`(damage 20), Rigidbody(gravity 꺼짐, non-kinematic), scale 1.34. 자식 Goomba 모델을 Y축 180° 회전 |
| 오브젝트 매핑 | `Assets/11. Datas/ObjectData.asset` | **굼바 = `ObjectType.BARREL`(2)** → `Gm_Obstacle_Goomba`. 나머지는 LASER, BLOCKADE, FAN, ROBOT_ARM, HP, KEY_CARD |
| 패턴 | `Assets/11. Datas/Patterns/3D/*` (20개), `2D/*` (5개) | 상단 레인은 `Pattern3D_19`, `Pattern2D_04`에서만 사용. 굼바(BARREL)는 3D_02/04/11/16, 2D_01/03 하단에 있음 |
| 스파이크 볼 모델 | 없음 | 후보: `Assets/TripoModels/creepy_spherical_monster_3d_model/`(미확인, git 미추적) 또는 프리미티브 구 |

## 2. 현재 동작 흐름

### 생성
1. `LaneManager.GenerateObjectCoroutine`이 뷰 모드에 맞는 `CreateObjectSO`에서 `PickRandom()`으로 패턴을 고른다.
2. 행마다 `GenerateRow(row)`가 `GenerateRow(row.lower, lanes)`와 `GenerateRow(row.upper, upperLanes)`를 호출한다.
3. `Lane.GenerateObstacle(type)`는 다음 순서로 처리한다.
   - `NONE`이면 건너뛴다.
   - 그 외에는 `InteractionObjectPool.Get(type, lane.position, identity)`로 꺼낸다.
   - `currentLane`과 `isUpper`를 세팅하고 `spawnedObjects`에 추가한다.
4. 거리 간격: 패턴 사이 `patternDistance` 9, 행 사이 `rowDistance`. `mapSpeed`가 9이므로 패턴 하나 생성 후 약 1초 대기한다.

### 이동과 소멸
- `Obstacle`, `HealObject`, `PowerUpItem`은 각자 `FixedUpdate`에서 `Rb.MovePosition(pos + back * mapSpeed * dt)`로 움직인다. `Obstacle.ObstacleMovement()`는 `protected virtual`이다.
- `InteractionObject.OnEnable`이 `DisposeCoroutine`을 시작한다. `z < DisposePos.z`(-50)이 되면 `SetActive(false)`로 처리한다. **월드와 같이 흘러가지 않는 오브젝트는 이 코루틴만으로는 사라지지 않는다.**
- `LaneManager.RemoveAllLaneObjects()`는 2D/3D 전환 시(`Player.CheckChangeView`) 각 Lane의 `spawnedObjects`를 `Pool.Return`한다. **Lane을 거치지 않고 생성한 오브젝트는 회수되지 않는다.**

### 충돌
- `InteractionObject.OnTriggerEnter`가 `onTargetHitEvent`를 발생시킨다.
- `Obstacle.OnCollded`의 판정 규칙: `isUpper`면 `player.IsJump`일 때만 맞고, 하단이면 `ignoreJumpedPlayer || !IsJump`일 때 맞는다. 무적이면 무시한다.
- 맞으면 글리치, 셰이크, SFX가 나오고 `player.TakeHit(damage)`가 호출된다.

### 풀
- `InteractionObjectPool.Get(type)`은 다음 순서로 동작한다.
  - 자식 중 비활성이면서 `objectData.type`이 같은 오브젝트를 재사용한다.
  - 없으면 `Instantiate`한다.
- `Return`은 transform을 리셋하고 `SetActive(false)`로 만든다.

## 3. 씬 수치 (SampleScene, 에디터에서 확인)
- Player: (0, 0, -9). 콜라이더 높이 약 1.08. 카메라: (0, 4, -11), pitch 40°.
- 레인 위치:
  - 하단 레인 `Lane_0~2`: x = -1/0/1, y 0, z 19.05.
  - 상단 레인 `Lane_Upper_0~2`: y 1.5.
- `mapSpeed` 9, `GeneratePos` z 70, `DisposePos` z -50.
- `LaneManager.upperLaneHeight` 1.5, `laneInterval` 1.

## 4. 의존 관계와 영향 범위
- **`ObjectType` enum(`InteractionObject.cs`)**: 에셋에 int로 직렬화된다. 값을 **끝에 추가**해야 기존 패턴과 `ObjectData.asset`이 유지된다.
- **`PatternEditorWindow`**: enum 전체를 메뉴에 자동으로 표시한다. 새 타입 색은 `GetColor`에 추가하면 되고, 추가하지 않으면 기본색이 쓰인다.
- **`LaneManager.GenerateRow`**: 하단 굼바 제한을 넣을 자리. 모든 생성이 이 함수를 거친다.
- **`Lane.GenerateObstacle`**: 스파이크 볼을 레인 회수 목록에 넣으려면 위치를 지정하는 오버로드가 필요하다.

## 5. 위험 요소
- **Attack 길이와 투척 간격이 맞지 않음**: Attack 클립은 2.96s인데 투척 간격은 2s다. 그대로 재생하면 다음 투척이 이전 동작을 끊는다. 스테이트 속도를 1.5배(1.97s, 릴리즈 약 1.39s)로 하거나 간격을 바꿔야 한다.
- **Animation Event 사용 불가**: Animator가 자식 모델에 있어서, Animation Event 수신 스크립트도 자식에 있어야 한다. → 코루틴에서 `릴리즈 시간 / 속도` 뒤에 공을 생성하는 쪽이 간단하다.
- **Lakitu 소멸 처리**: 월드와 같이 흘러가지 않으므로 `DisposeCoroutine`으로는 사라지지 않는다. 수명 코루틴에서 직접 `SetActive(false)`해야 한다.
- **기존 `Lane.spawnedObjects` 문제**: 항목을 지우지 않아 플레이 중 같은 풀 오브젝트가 계속 쌓인다. 스파이크 볼도 Lane 경유로 만들면 더 빨리 늘어난다. 기능에는 문제가 없고, 이번 범위 밖이다.
- **Lakitu 중복 생성**: 수명이 10초인데 패턴은 약 1초마다 뽑힌다. 같은 패턴이 다시 나오면 여러 마리가 동시에 존재할 수 있다.
- **씬 dirty 상태**: SampleScene에는 원래 사용자 변경분이 있다(git M). 씬은 수정하지 않는 설계가 안전하다.
