# Whomp (꽈당꽈당) 적 — Research

작업 목표: 패턴으로 생성되는 새 적 Whomp. 플레이어가 Whomp가 있는 레인에 진입하면 Whomp가 카메라 방향으로 90° 넘어지며(Lerp) 자폭한다. 그 순간 해당 레인의 모든 오브젝트가 파괴되고, Whomp 위치를 기준으로 앞/뒤로 각각 N개의 돌 파편 장애물이 가까운 쪽부터 순차적으로(폭발 이펙트와 함께, 바닥을 뚫고 올라오며) 생성되어 그 레인 진입을 봉쇄한다.

아래는 모두 코드/에셋에서 직접 확인한 사실이다.

## 1. 관련 에셋

| 종류 | 경로 | 확인 내용 |
|---|---|---|
| Whomp 모델 | `Assets/03. Models/Skeletal/Whomp.fbx` | guid `fab61c363dfc4194ea054ff1c50d0bc4`. `animationType: 2`(Generic), `globalScale: 1.5`, `useFileScale: 1`. 외부 머티리얼 `tripo_mat_6c26c1e9`(guid `b6c43623c82bd57488783931421dcecc`) 연결. 클립 `Walk`(frame 1~25, `loopTime: 1`, internalID `-623603649343465320`) 1개 |
| Whomp 컨트롤러 | `Assets/04. Animations/Whomp.controller` | 스테이트 `Walk` 1개(위 클립), 파라미터 없음, Base Layer만 존재 |
| Whomp 프리팹 | **없음** | `Assets/07. Prefabs/Enemy/`에는 `Gm_Chaser_1`, `Goomba`, `Lakitu`만 있음. `Obstacle3D/`에도 Whomp 래퍼 없음 |
| 파편 임시 프리팹 | `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Pipe.prefab` | guid `0549e2b9d1c498b4c83118cf5e69c218`, root fileID `3720953440862370034`. 루트에 `Obstacle`(damage 20, `ignoreJumpedPlayer: 0`, hitSFX 연결), Rigidbody(`m_UseGravity: 0`), Collider, 자식 모델 1개. `objectData.type: 1`(= LASER)로 저장되어 있음 |
| 본체 폭발 이펙트 | `Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/DustExplosion.prefab` | 루트 + 자식 `Embers`/`SmallExplosion`/`Shockwave`/`SandParticle`. **모든 ParticleSystem이 `looping: 0`, 스크립트(MonoBehaviour) 없음 → 자동 파괴 안 됨** |
| 파편 생성 이펙트 | 같은 폴더 `TinyExplosion.prefab` | 동일 구조(비루프, 스크립트 없음) |
| 오브젝트 매핑 | `Assets/11. Datas/ObjectData.asset` | `type: 0`(NONE, prefab 없음) ~ `type: 9`까지 등록됨. 9 = SPIKE_BALL. **새 타입은 10, 11이 된다** |
| 패턴 | `Assets/11. Datas/Patterns/3D/`, `2D/` | `PatternDataSO` 에셋. Pattern Editor로 편집 |

참고: 프로젝트 전체 스크립트(`Assets/02. Scripts/`)에 **`ParticleSystem`을 쓰는 코드가 하나도 없다** (grep 결과 0건). 즉 이펙트 재생/해제용 공통 헬퍼가 없으므로 새로 만들어야 한다.

## 2. 현재 동작 흐름 (확인 사실)

### 생성 경로
1. `LaneManager.GenerateObjectCoroutine`(`Map/LaneManager.cs:73`)이 뷰 모드별 `CreateObjectSO`에서 `PickRandom()`으로 패턴을 고르고, 행마다 `GenerateRow`를 호출한 뒤 `WaitDistance(rowDistance)` / `WaitDistance(patternDistance)`로 **거리 기준** 대기한다.
2. `LaneManager.GenerateRow(ObjectType[], Lane[])`(`:135`)에서 타입별 예외 처리를 한다. 현재 예외는 하나뿐:
   ```csharp
   if (types[i] == ObjectType.LAKITU && (Lakitu.ActiveCount > 0 || GameManager.Instance.currentViewMode == ViewMode.View2D))
       continue;
   ```
   → **동시 수 제한/2D 차단을 넣을 자리가 여기다.**
3. `Lane.GenerateObstacle(type)` / `GenerateObstacle(type, position)`(`Map/Lane.cs:17,19`): `NONE`은 skip, 풀에서 `Get(type, position, identity)`, `currentLane`/`isUpper` 세팅, `spawnedObjects`에 추가 후 반환.
   - **`Lane`을 거치지 않고 만든 오브젝트는 `RemoveAllObjects()` 회수 대상에서 빠진다** → 파편도 `Lane.GenerateObstacle(type, pos)`를 거쳐야 한다.

### 이동/소멸
- `Obstacle.FixedUpdate` → `ObstacleMovement()`(`protected virtual`, `Obstacles/Obstacle.cs:24`): `Rb.MovePosition(pos + Vector3.back * GameManager.Instance.mapSpeed * Time.fixedDeltaTime)`.
  - `SpikeBall`이 이 메서드를 오버라이드해 비행 중에만 다른 이동을 하는 선례가 있다(`Obstacles/SpikeBall.cs:42`).
- `InteractionObject.OnEnable` → `DisposeCoroutine`: `z < GameManager.Instance.DisposePos.z`면 `SetActive(false)`.
- `Pool<T>.Return`은 위치/회전/스케일을 리셋하고 `SetActive(false)`. **회전을 Lerp로 바꾸는 오브젝트는 `OnEnable`에서 회전을 직접 초기화해야 재사용 시 넘어진 상태로 나오지 않는다.**
- `InteractionObjectPool.Take`는 자식 중 `!activeSelf && objectData.type == type`인 것을 재사용, 없으면 `Instantiate`. `Get`은 위치 세팅 **후** `SetActive(true)`.

### 충돌/판정
- `InteractionObject.OnTriggerEnter`(`:84`)가 `ICollisionable`을 가진 상대에 대해 `onTargetHitEvent`/`onTakeHitEvent`를 발생. 같은 상대 중복 방지 리스트(`collisionables`) 보유, `OnDisable`에서 clear.
- `Obstacle.OnCollded`(`:31`): `isUpper`면 `player.IsJump`일 때만, 하단이면 `ignoreJumpedPlayer || !player.IsJump`일 때만 피격. 무적이면 무시. 피격 시 글리치 + 셰이크 + SFX + `player.TakeHit(damage)`.
  - **파편을 `Obstacle` 상속으로 만들면 "닿으면 대미지"는 공짜로 얻는다.** 점프로 넘지 못하게 하려면 `ignoreJumpedPlayer = true`.

### 플레이어 레인 상태
- `Player`는 `InteractionObject`를 상속하므로 `public int currentLane` 필드를 그대로 쓴다(`InteractionObject.cs:33`).
- `Player.ChangeLane(index)` → `ChangeLaneCoroutine`(`Player.cs:218`): `laneX`를 0.1초간 Lerp하고 **끝난 뒤에야** `currentLane = laneIndex`를 대입한다. → `currentLane`은 이동 완료 시점에 바뀐다(진입 판정이 0.1초 늦음). 더 즉각적으로 판정하려면 `player.transform.position.x`와 `lane.LaneX` 거리 비교가 정확하다.
- 2D 모드에서는 `Update`가 매 프레임 마지막 레인으로 고정하고 `ChangeLane`이 2D에서 즉시 return → **2D에서는 레인 진입 개념이 없다.**
- `GameManager.Instance.player`(`GameManager.cs:13`)로 플레이어 참조 가능(`Start`에서 `FindAnyObjectByType`).

### 씬 수치 (기존 Research 기록 + 코드 기준)
- 하단 레인 `Lane_0~2`: x = -1/0/1, y = 0, z ≈ 19.05. 상단 레인 y = 1.5(`upperLaneHeight`).
- `mapSpeed` 9, `GeneratePos.z` 70, `DisposePos.z` -50, 플레이어 z = -9, `laneInterval` 1, `patternDistance` 9.
- 즉 레인 생성 지점(z≈19)에서 플레이어(z=-9)까지 거리 약 28유닛 ≈ **3.1초**.

## 3. 의존 관계 / 영향 범위

| 대상 | 영향 |
|---|---|
| `ObjectType` enum (`Obstacles/InteractionObject.cs:7`) | 에셋에 int로 직렬화됨. **반드시 끝에 추가**(10, 11). 파일이 CP949이므로 ASCII만 삽입해 인코딩 유지 필요 |
| `PatternEditorWindow.GetColor`(`Editor/PatternEditorWindow.cs:246`) | 새 타입 색 추가(없으면 기본색) |
| `LaneManager.GenerateRow`(`:135`) | Whomp 동시 수 제한 + 2D 차단 조건 추가 지점 |
| `Lane` / `Pool` | 파편을 `Lane.GenerateObstacle(type, pos)`로 생성해야 2D 전환 시 함께 회수됨 |
| `InteractionObjectPool.GetChildList(false)` | 같은 레인 오브젝트 전수 조회에 쓰임(선례: `Lakitu.GetLandingZ`, `:138`) |
| `ObjectData.asset` | 새 타입 2개 프리팹 등록 |
| 패턴 에셋 | Whomp를 배치해야 실제로 등장 |

## 4. 위험 요소

1. **Whomp 모델의 정면 방향 미확인.** Lakitu는 +Z를 바라봤고 `Gm_Obstacle_Goomba`는 자식 모델을 Y 180° 회전시켜 쓴다. Whomp도 래퍼 프리팹에서 Y 회전으로 맞춰야 할 가능성이 높고, "카메라 방향(=-Z)으로 90° 넘어짐"의 회전축 부호도 실제 재생으로 확인해야 한다 → **회전 축/각도를 인스펙터 값으로 노출**해 조정 가능하게 한다.
2. **풀 재사용 시 회전 잔존.** 넘어진 회전이 남은 채로 재활성화될 수 있다 → `OnEnable`에서 회전/상태 초기화 필수.
3. **이펙트 누수.** ParticlePack 프리팹은 자동 파괴가 없다. 매 발동마다 `Instantiate`만 하면 비활성 오브젝트가 무한 누적된다 → 수명 후 `Destroy` 하는 헬퍼 필요.
4. **파편 생성 중 스크롤.** 순차 생성 중에도 월드는 `mapSpeed`로 흐른다. 생성 시점의 절대 z를 미리 계산해 두면 간격이 틀어진다 → 각 파편 생성 시 **그 시점의 기준 z**(사망한 Whomp 위치를 따라 같은 속도로 흐르는 앵커)를 쓰거나, 스크롤량(`mapSpeed × 경과시간`)을 보정해야 한다.
5. **파편이 플레이어 뒤에 생길 수 있음.** 발동 시 Whomp가 플레이어보다 앞(+Z)에 있으면 "뒤쪽(+Z)" 파편은 정상이지만, Whomp가 플레이어를 거의 지나친 상태라면 "앞쪽(-Z)" 파편이 플레이어 뒤에 생겨 무의미하다 → 발동 가능 z 범위(최소 거리) 제한 필요.
6. **파편이 플레이어 위에 즉시 생성될 위험.** 파편은 `Obstacle` 상속이므로 생성 즉시 플레이어와 겹치면 바로 대미지다. "바닥에서 솟아오름"을 쓰면 올라오는 동안 회피 여지가 생기지만, 플레이어 바로 아래 생성은 피해야 한다(최소 거리 제한으로 완화).
7. **레인 전체 오브젝트 파괴 범위.** 같은 레인 전체인지, Whomp 주변 일정 z 범위인지, 상단 레인 오브젝트까지 포함인지 결정 필요(아래 Plan의 열린 질문).
8. **2D 전환.** 발동 도중 2D로 전환되면 `RemoveAllLaneObjects()`가 Whomp/파편을 회수한다. 진행 중 코루틴이 비활성 오브젝트에서 돌지 않도록 `OnDisable` 정리 필요.
9. `Pool<InteractionObject>`는 타입별 리스트가 없어 파편을 많이 쓰면 `Instantiate`가 늘어난다(개수 N×2 수준이면 문제 없음).
