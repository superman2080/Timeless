# Bullet Bill (킬러) — Research

목표: 상단 레인 3개 전체를 가로막는 폭으로, 월드보다 빠르게 직선으로 날아오는 적. Bullet Bill이 살아 있는 동안에는 하단 레인 오브젝트를 생성하지 않는다. 모델은 씬에 놓여 있는 `creepy_spherical_monster_3d_model`.

아래는 모두 코드/에셋/에디터에서 실제로 확인한 내용이다.

## 1. 모델 — creepy_spherical_monster_3d_model

에셋: `Assets/TripoModels/creepy_spherical_monster_3d_model/`
- `creepy_spherical_monster_3d_model.fbx` (guid `58ac51dd96eeb0542ae2b24a2f1065e7`), `globalScale: 1`, `animationType: 2`(Generic), `importAnimation: 1`
- FBX 내부 서브에셋: GameObject / Transform / **MeshFilter + MeshRenderer** / Mesh `tripo_node_f799e84a` 하나뿐 → **스킨드 메시·본·애니메이션 클립 없음 (정적 메시)**
- 머티리얼: `Materials/creepy_spherical_monster_3d_model.mat` (guid `2410919ab8b6c76409df9fc84d97d12a`), URP Lit, Opaque
- 텍스처: `.fbm/` 아래 metallic / metallic_Smoothness / roughness

씬 인스턴스 (에디터에서 `eval_file`로 확인):
- `SampleScene` 루트의 `creepy_spherical_monster_3d_model`
- **씬이 dirty 상태이고 이 오브젝트는 저장되지 않았다** — 디스크의 `SampleScene.unity`, 어떤 프리팹에도 FBX guid 참조가 없다 (`grep` 결과 0건). 프리팹 연결도 없음(`GetPrefabAssetPathOfNearestInstanceRoot` 빈 문자열).
- Transform: pos `(-10.96, 0, -2.54)`, rot `(270, 0, 0)`, scale `(1,1,1)` — Tripo FBX 특유의 X -90° 보정
- 컴포넌트: Transform, MeshFilter, MeshRenderer (머티리얼 1슬롯). Collider / Rigidbody / Animator 없음
- 렌더러 bounds extents `(0.49, 0.36, 0.49)` → 크기 약 **0.98 × 0.72 × 0.98** (거의 구, 피벗이 바닥, 중심 y = 0.36)
- **정면(입/눈 방향)이 어느 축인지는 아직 확인 못 함** — 프리팹 만들 때 캡처로 확인 필요.

## 2. 레인 구성 (상단 레인)

`Assets/02. Scripts/Map/LaneManager.cs`
- `InitLanes()`: 자식 `Lane`을 `IsUpper`로 나눠 `lanes`(하단) / `upperLanes`(상단)에 담고, 둘 다 `laneInterval` 간격으로 x 배치. 상단은 `basePosition + up * upperLaneHeight`.
- SampleScene 값: `upperLaneHeight: 1.5`, `laneInterval: 1`, `patternDistance: 9` → 상단 레인 x = -1 / 0 / 1, y = 1.5. **상단 3레인 전체 폭 ≈ 3유닛** (레인 중심 간 2 + 양쪽 반 칸).
- `GenerateRow(PatternRow)` → `GenerateRow(row.lower, lanes)` 후 `GenerateRow(row.upper, upperLanes)` (LaneManager.cs:129~133).
- `GenerateRow(ObjectType[], Lane[])`(135~147)에 Lakitu/Whomp용 예외가 하드코딩됨: `ActiveCount > 0 || 2D`면 `continue`. **하단 생성 억제도 같은 자리에서 처리 가능.**
- `GeneratePatternCoroutine`은 패턴 행을 `rowDistance` 간격으로 순차 생성, 패턴 사이는 `patternDistance`. 거리 기반(`WaitDistance`, mapSpeed × fixedDeltaTime 누적).

`Assets/02. Scripts/Map/Lane.cs`
- `GenerateObstacle(type, pos)` → 풀 Get → `currentLane = LaneIndex`, `isUpper = isUpper` → `spawnedObjects`에 등록.
- `RemoveAllObjects()`는 그 레인의 `spawnedObjects`만 회수 → **Lane을 거쳐 생성해야 2D 전환 시 회수된다.**

## 3. 상단 판정

`Assets/02. Scripts/Obstacles/Obstacle.cs:331~344`
- `isUpper`면 `player.IsJump`일 때만 맞음, 하단이면 `ignoreJumpedPlayer || !IsJump`.
- 무적이면 무시. 맞으면 글리치 + 셰이크 + `hitSFX` + `player.TakeHit(DamageAmount)`.
- `ObstacleMovement()`가 `protected virtual` → 속도만 바꾸려면 오버라이드하면 된다.
- `isUpper`는 `Lane.GenerateObstacle`이 세팅하므로 상단 Lane으로 생성하면 자동으로 "점프 중에만 판정"이 된다.

`Assets/02. Scripts/Player/Player.cs`
- 점프: `JumpCoroutine`에서 `jumpCurve`(기본 0→1.5→0)로 `jumpOffsetY`를 올리고 그 동안 `IsJump = true` (234~244).
- 판정은 `OnTriggerEnter` 1회 진입 기반(`InteractionObject.cs`). Bullet Bill이 빠르게 지나가므로 진입 순간의 `IsJump`만 본다.

## 4. 동시 등장 제한 패턴 (Lakitu / Whomp)

`Whomp.cs:9~66`, `Lakitu.cs:7~72`
- `public static int ActiveCount`, `OnEnable`에서 `++`, `OnDisable`에서 `--`.
- 도메인 리로드가 꺼져 있어 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)] ResetStatics()`로 0 초기화.
- 비활성화 경로(Dispose, 2D 회수, 풀 Return) 모두 `SetActive(false)` → `OnDisable`을 타므로 카운트가 새지 않는다.
- **Bullet Bill도 같은 방식으로 `ActiveCount`를 두면 "살아 있는 동안 하단 생성 금지"를 LaneManager에서 바로 확인할 수 있다.**

## 5. 이동 / 회수

- `Obstacle.FixedUpdate` → `ObstacleMovement` → `Rb.MovePosition(pos + back * mapSpeed * dt)`.
- `InteractionObject.DisposeCoroutine`: `z < DisposePos.z`면 비활성화. 속도가 빨라도 그대로 동작.
- `mapSpeed`는 `Player.ApplyStats`만 바꾼다(CLAUDE.md 규칙) → Bullet Bill의 추가 속도는 **자기 이동량에만** 더해야 한다(mapSpeed 수정 금지).
- 생성 위치 z = 상단 레인 위치(`GetTopViewportPosition(0.05f)`, 화면 상단). 화면 위에서 갑자기 튀어나오는 형태.

## 6. 등록 경로 (Boo 작업에서 확인한 것과 동일)

- `ObjectType` enum (`InteractionObject.cs`) 마지막 값 `BOO = 12` → 새 값은 `BULLET_BILL = 13` append.
- `Assets/11. Datas/ObjectData.asset`: `type: 12` → Gm_Obstacle_Boo까지 등록. 새 항목 `type: 13`.
- 프리팹 2단 구조: 모델 프리팹 `Assets/07. Prefabs/Enemy/<Name>.prefab` + 스폰 프리팹 `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_<Name>.prefab`(Rigidbody 중력X + 트리거 BoxCollider + 스크립트 + 모델 자식).
- `PatternEditorWindow.GetColor` (`Editor/PatternEditorWindow.cs:246`)에 색 추가.
- 3D 패턴: `Assets/11. Datas/Patterns/3D/Pattern3D_00~22`, `CreateObjectData3D.asset`에 23개 등록(확률 합 100).

## 7. UI (경고 연출 배치 위치)

SampleScene `===UI===` 하위 `Canvas` (에디터 `eval_file`로 확인):
- `renderMode: ScreenSpaceOverlay`, `CanvasScaler`: **ConstantPixelSize** (referenceResolution 800×600은 미사용)
- 자식
  - `OctoombaScreenEffect` — 앵커 전체 화면 stretch, 화면 효과용
  - `HPBar` — 앵커 좌상단, pos (300, -100), size 668×208
  - `Score` — 앵커 우상단, pos (-50, -100), TextMeshProUGUI
- → **상단 중앙은 비어 있다.** 경고 아이콘은 상단 중앙 앵커에 두면 기존 UI와 겹치지 않는다.

화면 효과 스크립트 패턴: `Assets/02. Scripts/UI/OctoombaScreenEffect.cs`
- `public static Instance` + `Awake`에서 `Instance = this`, `OnDestroy`에서 해제. 주석: "`Singleton<T>`을 쓰면 Canvas가 DDOL이 되어 씬 재로드 시 중복되므로 씬마다 Awake에서 교체되는 static 참조를 쓴다".
- 호출 측(`Octoomba.cs:31`)은 `if (OctoombaScreenEffect.Instance != null)`로 가드 후 호출.
- 알파 연출은 `Image` + 코루틴. → **경고 UI도 같은 구조로 만들면 된다.**

스프라이트: `Assets/10. Sprites/` (`hpBar.png`, `Timer.png` 등). `hpBar.png.meta` 기준 `textureType: 8`(Sprite), `spriteMode: 2`. 새 경고 이미지는 여기에 Sprite(Single)로 임포트.

## 8. Collider 초기화 시점

- `InteractionObject.Col`/`Rb`는 `Start`에서 할당된다. 풀이 처음 `SetActive(true)` 할 때 `OnEnable`이 `Start`보다 먼저 호출되므로, **OnEnable에서 `Col`을 쓰면 첫 생성 시 null.** 경고 단계에서 콜라이더를 끄려면 자체적으로 `Awake`에서 `GetComponent`해야 한다.

## 9. 구현 직전 repo 갱신 (`ac5a0ba` → `afb3fd5`)

구현 시작 시점에 확인한 변경 (위 1~8의 일부 사실이 바뀜):
- `ObjectType` 끝: `BOO(12), OCTOOMBA(13), SUPER_MUSHROOM(14), MINI_MUSHROOM(15)` → 새 값은 16.
- `LaneManager`: `objectDataSet3D/2D` → `PatternSetSO patternSet`(HP 구간별 `CreateObjectSO` 선택). 레인 z는 `GetTopViewportPosition` 대신 고정 `spawnZ`(씬 값 20). `GenerateRow` 구조는 그대로.
- SampleScene의 `patternSet`은 `DebugPatternSet` → 3D는 `DebugCreateObject3D`(패턴 2개). 정식 세트는 `PatternSet` → `CreateObjectData3D`.
- `Pattern3D_23~25` 사용 중 → 새 패턴은 `Pattern3D_26`.
- Addressables 도입: `Prefabs_Obstacle` 그룹에 `Obstacle3D/*`, `Enemy/*` 프리팹이 파일명 주소 + 라벨 `obstacle`로 등록됨.
- `Obstacle.OnHitPlayer(Player)` 가상 훅 추가(실제 피격 시). Octoomba가 사용.
- SampleScene이 디스크 버전으로 리로드되어, 저장 안 된 `creepy_spherical_monster_3d_model` 인스턴스는 사라짐.
- 모델 방향(렌더 확인): 탄환 모양, 코(얼굴)가 **+Z**, 길이(z) 0.98 / 높이 0.72 / 폭 0.98, 피벗은 바닥 중앙.
- 기존 상단 레인 배치 오브젝트(HP, KEY_CARD, BLOCKADE)는 피벗(바닥)이 상단 레인 y에 놓인다.

## 10. 위험 요소

1. **패턴 셀 3칸 = 오브젝트 3개 문제**: 패턴의 `upper[3]`에 BULLET_BILL을 3칸 다 넣으면 3개가 생성된다. "상단 전체 1개"로 만들려면 LaneManager에서 한 번만, 가운데 상단 레인에 생성하도록 특수 처리해야 한다.
2. **이미 깔린 하단 오브젝트**: 생성 억제는 "새로 생성"만 막는다. Bullet Bill 생성 시점에 이미 트랙 위에 있는 하단 장애물을 점프로 넘다가 Bullet Bill에 맞을 수 있다(점프 강요 + 점프하면 피격). Bullet Bill이 월드보다 빨라 그 장애물을 추월하는 구간에서 발생.
3. **같은 패턴 안 하단 셀**: Bullet Bill이 든 행의 `lower`는 `GenerateRow` 순서상 **상단보다 먼저** 생성된다(LaneManager.cs:131→132). 같은 행 하단까지 막으려면 순서를 바꾸거나 상단을 먼저 검사해야 한다.
4. **2D 모드**: 2D는 레인 2만 쓰고 플레이어는 좌우 이동 불가. 3레인 폭의 적은 의미가 약하고, Lakitu/Whomp도 2D 생성 금지 → 같은 정책이 자연스럽다. 2D 전환 시 `RemoveAllLaneObjects`로 회수되면 `OnDisable`로 `ActiveCount`가 0이 되어 하단 생성이 복구된다.
5. **모델 비율**: 원본은 약 1유닛 구, 레인 간격도 1유닛. 스케일을 키우지 않고 레인마다 모델 1개씩(3개) 나란히 두면 폭이 정확히 3레인을 덮는다. 레인 간격(`laneInterval`)이 바뀌면 모델 배치도 따라가야 하므로 런타임에 간격을 읽어 배치하는 편이 안전.
6. **빠른 이동 + 트리거**: `MovePosition` 이동이 프레임당 크게 튀면 얇은 트리거 통과 누락 가능. 플레이어 캡슐 반지름 0.25, Bullet Bill 콜라이더 z 두께를 이동량보다 충분히 크게 두거나 Rigidbody `collisionDetectionMode`를 Continuous Speculative로.
7. **씬 인스턴스 미저장**: 씬의 원본 오브젝트는 저장 안 된 임시 상태. 프리팹은 FBX에서 직접 만들고 씬 인스턴스는 삭제(씬을 저장할지 여부 주의 — 씬에 다른 미저장 변경이 있을 수 있음).
