# Chain Chomp (멍멍이) — Research

목표: 하단 레인에 생성되는 적. 생성 위치보다 Z축 뒤쪽(+Z) 바닥 말뚝에 사슬로 묶여 있다.
- 플레이어가 **같은 레인**이고 **일정 거리 안**이면 플레이어 쪽으로 빠르게 달려들어 대미지를 준다. 피하려면 다른 레인에 있어야 한다.
- 범위 밖이면 플레이어를 향한 앞쪽 랜덤 방향으로 돌진했다가 돌아오는 위협 동작을 반복한다.
- 사슬에는 `ChainPhysics`를 적용하고, 사슬 고리 모델은 임시로 Capsule 기본 모델을 쓴다. 머리 모델은 자식 오브젝트로 두고 Z -25° 회전을 준다.

아래는 모두 코드, 에셋, 에디터(`eval_file`)에서 실제로 확인한 내용이다. (2026-10-30)

## 1. 모델 — monster_ball_3d_model

- `Assets/TripoModels/monster_ball_3d_model/monster_ball_3d_model.fbx` (guid `e8db279966a450d4cac845a2ebff872d`), git 미추적
- 머티리얼 리맵: `Materials/monster_ball_3d_model.mat` (guid `79b77352830d6234486b1d2fe58baa91`)
- 텍스처는 `.fbm/` 아래 basecolor / metallic / normal / roughness 등 6개 → **지우면 안 됨**
- SampleScene 루트의 `ChainChomp` 오브젝트
  - 씬 파일에 저장되어 있다 (`SampleScene.unity`에 `ChainChomp` 1건). 프리팹 연결 없음.
  - 컴포넌트: Transform, MeshFilter(`tripo_node_1dacfdb1`), MeshRenderer. Collider/Rigidbody 없음
  - 회전: `Euler(270, 0, -25)`와 정확히 일치 (각도 차 0°). 인스펙터에는 짐벌락 때문에 (270, 335, 0)으로 보일 수 있다.
  - 사용자 확인: 이 회전에서 정면을 바라본다. 정면이 -Z(플레이어 쪽)인지는 **구현 때 캡처로 확인 필요**.
  - 렌더러 bounds: center y 0.49, extents (0.65, 0.49, 0.65) → **지름 약 1유닛 구, 피벗은 바닥**
- **Unity FBX 임포트 설정으로는 임의 각도 회전이 불가능**하다. 그래서 프리팹 안에서 모델을 자식으로 두고 -25°를 준다 (사용자 결정).

## 2. ChainPhysics

- 프로젝트 전체(`Assets/`, `Packages/`, `manifest.json`)에서 `ChainPhysics`를 검색한 결과 **0건**이다. 사슬 관련 스크립트, 패키지, Joint 사용처도 없다.
- 그래서 **새로 작성해야 한다.**
- 물리 모듈(`com.unity.modules.physics`)은 있다. `Physics.gravity` = (0, -9.81, 0), `fixedDeltaTime` = 0.02.

## 3. 월드 이동 / 레인

`Assets/02. Scripts/Map/LaneManager.cs` (**CP949 파일**)
- SampleScene 값: `laneInterval` 1, `spawnZ` 20, `upperLaneHeight` 1.5, `patternDistance` 9, `patternSet` = `DebugPatternSet`
- 하단 레인 x = -1 / 0 / 1, y = 0, z = 20에서 생성
- `GenerateRow(ObjectType[], Lane[])`: Lakitu/Whomp는 `ActiveCount > 0 || 2D`면 건너뛰는 예외가 하드코딩되어 있다. 새 적의 생성 제한도 같은 자리에 넣으면 된다.
- `GenerateRow(PatternRow)`: 상단을 먼저 생성하고, `BulletBill.ActiveCount == 0`일 때만 하단을 생성한다 (킬러 작업 때 추가, 미커밋).

`Assets/02. Scripts/Map/Lane.cs`
- `GenerateObstacle(type, pos)`: 풀에서 `Quaternion.identity`로 꺼내고 `currentLane`/`isUpper`를 세팅한 뒤 `spawnedObjects`에 등록한다.
- `RemoveAllObjects()`는 이 목록만 회수한다 → 2D 전환 시 함께 회수된다.

`GameManager` 런타임 값: `mapSpeed` 9, `GeneratePos` (0,0,70), `DisposePos` (0,0,-50)

`InteractionObject.DisposeCoroutine`: 루트 `z < DisposePos.z`면 `SetActive(false)`

## 4. 플레이어

`Assets/02. Scripts/Player/Player.cs` (**CP949 파일**, 수정 대상 아님)
- 위치 z = **-9 고정**, scale 0.6
- CapsuleCollider: 트리거, r 0.25, h 1.8, center y 0.9 (스케일 적용 시 r 0.15, h 1.08)
- Rigidbody: 비키네마틱, 중력 X
- `currentLane`은 `ChangeLaneCoroutine`(0.1초)이 **끝난 뒤에** 갱신된다. 이동 중에는 이전 레인 값이 남아 있다.
- `IsJump`: 점프 중 true. `IsInvincible`: 무적 플래그
- 2D 모드: 마지막 레인(2)에 고정, 좌우 이동 불가

## 5. 피격 판정 — Obstacle

`Assets/02. Scripts/Obstacles/Obstacle.cs`
- `OnCollded`: 상단이면 점프 중에만 맞고, 하단이면 `ignoreJumpedPlayer || !player.IsJump`일 때 맞는다. 무적이면 무시.
- 맞으면 글리치 + 카메라 셰이크 + `hitSFX` + `player.TakeHit(DamageAmount)` + `OnHitPlayer(player)` 훅
- `FixedUpdate` → `protected virtual ObstacleMovement()` (기본: `mapSpeed`로 -Z 이동)
- **`ignoreJumpedPlayer = true`면 점프로 피할 수 없다** → "다른 레인으로만 회피" 요구에 맞는다.

`Assets/02. Scripts/Obstacles/InteractionObject.cs`
- `[RequireComponent(Collider, Rigidbody)]`: 루트에 Collider와 Rigidbody가 있어야 한다.
- `OnTriggerEnter`가 루트에서 `onTargetHitEvent`를 발생시킨다. `collisionables` 목록이 있어 같은 상대에 대해 `OnTriggerExit` 전까지 1회만 판정한다.
- `Col`/`Rb`는 `Start`에서 할당된다 → **첫 `OnEnable` 시점엔 null** (킬러 Research 8과 같음)
- `ObjectType` 마지막 값 `BULLET_BILL` = 16 → 새 값은 **17**

## 6. 비슷한 적 구현 참고

- `Whomp.cs`: 같은 레인 + `0 < deltaZ <= triggerDistance`면 발동, 씬 뷰 기즈모로 구간 표시, `ActiveCount` + `ResetStatics()`, 2D 생성 금지, `OnEnable`에서 풀 재사용 상태 초기화
- `Lakitu.cs`: `FixedUpdate`에서 목표 위치로 `MovePosition`/`SmoothDamp` 이동
- `BulletBill.cs`: `Awake`에서 콜라이더를 직접 캐싱 (`Col`이 null인 문제 회피), `ObstacleMovement()` 오버라이드
- 프리팹 2단 구조 (`Gm_Obstacle_Whomp` 확인)
  - 루트: Rigidbody(중력 X, 비키네마틱) + 트리거 BoxCollider + 스크립트
  - 자식: 모델 프리팹 인스턴스(`Enemy/Whomp.prefab`) → 그 안에 FBX 회전(270, 0, 0)

## 7. 등록 경로

- `ObjectType` enum에 값 추가 (`InteractionObject.cs`)
- `Assets/11. Datas/ObjectData.asset`에 타입 → 프리팹 등록
- Addressables `Prefabs_Obstacle` 그룹에 `Obstacle3D/*`, `Enemy/*` 프리팹을 파일명 주소 + 라벨 `obstacle`로 등록 (킬러 작업과 같은 규칙)
- `PatternEditorWindow.GetColor` (`Editor/PatternEditorWindow.cs:246~265`)에 색 추가
- 3D 패턴은 `Pattern3D_00~26` 사용 중 → 새 패턴은 **`Pattern3D_27`**. `CreateObjectData3D.asset`에 25개 등록 (합 100)
- SampleScene은 `DebugPatternSet`(→ `DebugCreateObject3D`)을 사용 중이다. 정식 세트에만 등록하면 에디터 플레이에서 안 나온다.

## 8. 위험 요소

1. **콜라이더 위치**: `InteractionObject`는 루트 콜라이더의 트리거만 받는다. 머리가 루트에서 떨어져 돌진하므로 콜라이더가 머리를 따라가야 한다.
   - 방법 A: 루트에 SphereCollider를 두고 매 프레임 `center`를 머리 위치로 갱신
   - 방법 B: 머리 자식에 콜라이더를 두는 복합 콜라이더. 트리거 메시지가 Rigidbody 오브젝트(루트)로도 전달된다.
   - A는 콜라이더가 1개라 `RequireComponent`와 그대로 맞는다.
2. **사슬 고리의 기본 Capsule에는 CapsuleCollider가 붙어 있다.** 지우지 않으면 루트 Rigidbody의 복합 콜라이더가 된다(트리거 아님). 그러면 플레이어나 다른 오브젝트와 물리 충돌하거나 판정이 생긴다 → **반드시 제거**
3. **월드 스크롤과 사슬 물리**: 사슬을 월드 좌표로 시뮬레이션하면 루트가 -Z로 계속 움직여 관성 때문에 사슬이 뒤로 끌린다. **루트 로컬 공간에서 시뮬레이션**해야 스크롤 영향이 없다.
4. **레인 폭**: 머리 지름(약 1)이 `laneInterval`(1)과 같다. 옆 레인 플레이어 캡슐 가장자리는 x 0.75 지점이다 (스케일 적용 시 0.85). **머리 콜라이더 반경이 0.75를 넘으면 옆 레인에서도 맞는다** → 반경 0.4 정도로 작게
5. **랜덤 돌진이 옆 레인을 침범하면** "다른 레인이면 안전" 규칙이 깨진다. 랜덤 돌진의 x 범위를 자기 레인 안으로 제한하거나, 랜덤 돌진 중에는 판정을 끄는 방법이 있다.
6. **레인 변경 중 판정**: `player.currentLane`은 이동이 끝나야 바뀐다. 발동 조건은 `currentLane`으로 보더라도, 실제 피격은 트리거(위치)로 처리해야 막 빠져나간 플레이어가 억울하게 맞지 않는다.
7. **2D 모드**: 플레이어가 레인 2에 고정되어 피할 수 없다 → Whomp처럼 2D 생성 금지가 자연스럽다.
8. **풀 재사용**: 머리 위치, 사슬 점, 상태 코루틴을 `OnEnable`에서 초기화해야 한다.
9. **인코딩**: `LaneManager.cs`, `InteractionObject.cs`는 CP949. 수정 시 인코딩을 유지한다. 새 스크립트는 UTF-8로 작성한다.
10. **씬 인스턴스**: SampleScene의 `ChainChomp`는 씬에 저장된 상태. 프리팹으로 옮긴 뒤 씬에서 지워야 한다. 지우지 않으면 플레이 중에도 원점에 떠 있다.
11. **(구현 중 확인) 점프 높이**: 점프 중 플레이어는 `jumpCurve` 최고 1.5까지 올라간다. 머리 높이(약 1)의 구 콜라이더로는 겹치지 않는다. 그래서 `ignoreJumpedPlayer = true`여도 트리거가 일어나지 않아 점프로 피해진다. → 세로로 긴 캡슐 콜라이더가 필요하다.
