# Bullet Bill (킬러) — Plan

> 상태: **구현 완료** (Phase 1~6 전부). 플레이 검증 통과, 콘솔 신규 에러/경고 0.

## 구현 현황 요약

완료
- Phase 1 `BulletBill.cs` (`BulletBill : Obstacle`) — 경고 → 발사 2단계, 동시 1개 카운트, 레인 간격 기반 모델 배치/콜라이더 폭
- Phase 2 `BulletBillWarningUI.cs` + `RoundedRect.png` + SampleScene Canvas에 `BulletBillWarning` UI
- Phase 3 `ObjectType.BULLET_BILL`(값 16) + Pattern Editor 색 + `LaneManager` 상단 우선 생성 / 하단 억제
- Phase 4 `Enemy/BulletBill.prefab`, `Obstacle3D/Gm_Obstacle_BulletBill.prefab`, `ObjectData.asset` 등록, Addressables 등록
- Phase 5 `Pattern3D_26` + `CreateObjectData3D` 등록 (25개 합 100)
- Phase 6 플레이 검증 (아래 항목 전부 통과)

계획과 달라진 점
1. **리서치 이후 repo가 갱신되어(`ac5a0ba` → `afb3fd5`) 번호/경로가 바뀌었다.**
   - `ObjectType` 끝이 `MINI_MUSHROOM`(15)이 되어 `BULLET_BILL`은 **16** (계획: 13).
   - `Pattern3D_23~25`가 이미 사용 중이라 새 패턴은 **`Pattern3D_26`** (계획: 23). `CreateObjectData3D`는 24 → 25개, 새 패턴 4%, 기존은 비율 유지하며 96%로 축소.
   - 레인 생성 위치가 카메라 상단 뷰포트 → **고정 `spawnZ = 20`**으로 바뀜. 킬러도 z=20에서 경고 후 발사.
   - SampleScene은 `DebugPatternSet`(→ `DebugCreateObject3D`)을 사용 중. 정식 `CreateObjectData3D`에만 등록했고 디버그 세트는 건드리지 않았다. **에디터에서 바로 보려면 `DebugCreateObject3D`에 `Pattern3D_26`을 넣어야 한다.**
2. **Addressables 등록 추가** — 갱신된 repo에 `Prefabs_Obstacle` 그룹(라벨 `obstacle`)이 생겨, 다른 적과 같은 규칙으로 `Gm_Obstacle_BulletBill` / `BulletBill`(모델) 엔트리를 추가했다.
3. **씬 임시 인스턴스 삭제 불필요** — repo 갱신 때 SampleScene이 디스크 버전으로 리로드되어 저장 안 된 `creepy_spherical_monster_3d_model` 인스턴스는 이미 없었다.
4. **모델 방향/높이** — 렌더 확인 결과 모델은 탄환 모양이고 코(얼굴)가 +Z를 향해, 모델 프리팹 루트에 Y 180°를 더했다(`Euler(0,180,0) × FBX 회전(270,0,0)`). 피벗이 바닥이라 다른 상단 오브젝트처럼 **바닥을 상단 레인 y(1.5)에** 둔다. 콜라이더 size (3, 0.72, 0.98), center (0, 0.36, 0).
5. **`LaneManager.cs`는 CP949 파일** — 인코딩을 유지한 채 수정했다(추가한 한글 주석도 CP949).

참고 (의도하지 않은 diff)
- `Assets/TripoModels/creepy_spherical_monster_3d_model/Materials/creepy_spherical_monster_3d_model.mat`의 `_BaseMap`/`_MainTex`에 같은 폴더의 `Color.jpg`가 연결되는 diff가 생겼다. 에디터에 저장되지 않은 채 남아 있던 변경이 등록 단계의 `AssetDatabase.SaveAssets()`에서 함께 기록된 것으로 보인다. 모델 자체 텍스처라 그대로 두었는데, 원하지 않으면 되돌릴 것.

---

## 설계 요약

- **적 타입**: `BulletBill : Obstacle` — 피격 판정(상단 = 점프 중에만), 글리치/셰이크/SFX는 `Obstacle` 그대로 재사용. `ObstacleMovement()`만 오버라이드.
- **3레인 폭을 한 번에 덮으며 정면(-Z)으로 날아온다.**
  - 오브젝트 1개 = 모델 3개. 모델 스케일은 원본(1) 그대로, **레인마다 모델 1개씩** x = -laneInterval / 0 / +laneInterval에 나란히 배치.
  - 콜라이더는 BoxCollider 1개, x 폭 = `laneInterval × 모델 수`(현재 3유닛).
  - 모델 배치·콜라이더 폭은 `OnEnable`에서 `GameManager.Instance.laneManager.laneInterval`을 읽어 맞춘다.
- **이동**: 월드 속도 + 자체 비행 속도로 직진. `mapSpeed`는 건드리지 않는다(CLAUDE.md 규칙).
- **경고 → 발사 2단계**
  1. 경고(`warningDuration`초): 생성 위치에 멈춘 채 모델·콜라이더 숨김, UI 상단 중앙에 빨간 ! 블록 깜빡임
  2. 발사: 아이콘 숨김, 모델·콜라이더 켜고 직진
- **하단 생성 억제**: `BulletBill.ActiveCount > 0`이면 `LaneManager`가 하단(`lower`) **새 생성만** 건너뛴다. 경고 단계부터 카운트된다.
  - 같은 행의 하단도 막기 위해 `GenerateRow`에서 **상단을 먼저 생성**한다.
- **동시 1개 제한 + 2D 생성 금지**: Lakitu/Whomp와 같은 정책.
- **경고 UI**: `BulletBillWarningUI` — `OctoombaScreenEffect`와 같은 씬 단위 static `Instance` 패턴. Canvas 상단 중앙에 배치.
  - 아이콘: `Assets/10. Sprites/Warning.png`의 `Warning_0`
  - 배경판: 아이콘과 동일한 크기의 **모서리가 둥근** 반투명 빨간 사각형을 아이콘 뒤에 깔아 **투명 여백만 반투명 빨간색으로** 보이게 한다. 아이콘과 같이 깜빡인다.

## 확정된 결정

1. 3레인 폭을 한 번에 덮으며 정면으로 날아오는 방식.
2. 모델 스케일 유지, 레인마다 모델 1개씩 3개로 3레인을 덮음.
3. 하단은 **새 생성만** 막는다. 기존 하단 오브젝트는 회수하지 않음.
4. 경고 연출: `Assets/10. Sprites/Warning.png`.
5. 아이콘의 투명 여백을 반투명 빨간색으로 덮는다 (아이콘 뒤 배경판).
6. 배경판은 모서리가 둥근 형태 (반경 16px).
7. 깜빡임 주기 0.15초.
8. 기본값: 배경판 `(1,0,0,0.35)`, 아이콘 상단 중앙 1개 128×128, `warningDuration = 1`, `flySpeed = 10`, `damageAmount = 10`.

---

## Phase 1 — BulletBill 스크립트

- [x] `Assets/02. Scripts/Obstacles/BulletBill.cs` 신규 (`BulletBill : Obstacle`, UTF-8)
  - `ActiveCount` + `ResetStatics()` (Whomp/Lakitu 동일)
  - 필드: `flySpeed = 10`, `warningDuration = 1`, `Transform[] models`
  - `Awake`: `BoxCollider` 캐싱 (`InteractionObject.Col`은 `Start`에서 할당되어 첫 `OnEnable` 때 null)
  - `OnEnable`: 카운트 증가 → `LayoutModels()` → 모델·콜라이더 숨김 → `WarningCoroutine` 시작
  - `OnDisable`: 카운트 감소, 경고 중이면 코루틴 정지 + UI `Hide()`
  - `WarningCoroutine`: UI `Show()` → `warningDuration` 대기 → `Hide()` → 모델·콜라이더 켬, 발사
  - `ObstacleMovement()`: 발사 전 정지, 발사 후 `(mapSpeed + flySpeed)`로 `-Z` 이동
  - 구현 메모: 계획의 "씬 뷰 기즈모"는 넣지 않았다(콜라이더 기즈모로 충분).
- [x] 컴파일 확인 — 에러 0

## Phase 2 — 경고 UI

- [x] `Assets/02. Scripts/UI/BulletBillWarningUI.cs` 신규 (UTF-8)
  - static `Instance`(Awake 할당 / OnDestroy 해제), `GameObject root`, `blinkInterval = 0.15`
  - `Show()`: 깜빡임 코루틴(`root` 활성 토글), `Hide()`: 코루틴 정지 + `root` 비활성
- [x] SampleScene `Canvas`에 UI 추가 (씬 저장, diff는 추가분만)
  ```
  Canvas
  └ BulletBillWarning        (BulletBillWarningUI, 앵커 상단 중앙 (0.5,1), pos (0,-100), size 128×128)
    └ Root                   (stretch, 깜빡임 대상)
      ├ Backdrop             (Image, RoundedRect, Sliced, (1,0,0,0.35), stretch)
      └ Icon                 (Image, Warning_0, 흰색, stretch)
  ```
  - 모든 `Image`의 `raycastTarget = false`
- [x] `Assets/10. Sprites/RoundedRect.png` 생성 — 64×64 흰색, 반경 16px, 1px 안티앨리어싱. Sprite(Single), border 16, Bilinear, Full Rect, 밉맵 없음, 무압축

## Phase 3 — 타입 등록 / 스폰 로직

- [x] `ObjectType`에 `BULLET_BILL` 추가 — **값 16** (위 "계획과 달라진 점" 1)
- [x] `PatternEditorWindow.GetColor`에 `BULLET_BILL` 색 `(0.2, 0.2, 0.25)` 추가
- [x] `LaneManager.GenerateRow(PatternRow)` 수정
  - 상단 먼저(`GenerateUpperRow`) → `ActiveCount == 0`일 때만 하단
  - `GenerateUpperRow`: `upper`에 BULLET_BILL이 있으면 `ActiveCount == 0 && 3D`일 때 가운데 상단 레인에 1개 생성
  - `GenerateRow(types, targets)`: BULLET_BILL 셀은 건너뜀. Lakitu/Whomp 예외 유지
- [x] 컴파일 확인 — 에러 0

## Phase 4 — 프리팹

- [x] `Assets/07. Prefabs/Enemy/BulletBill.prefab` — FBX 인스턴스, 스케일 1, 루트 회전 `Euler(0,180,0) × (270,0,0)` (코가 -Z)
- [x] `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_BulletBill.prefab`
  - 루트: Rigidbody(중력 X, 비키네마틱, Continuous Speculative), BoxCollider(트리거, size (3, 0.72, 0.98), center (0, 0.36, 0)), `BulletBill`
  - 자식: `Model_L/C/R` (모델 프리팹 인스턴스, x = -1/0/1) → `models`
  - `objectData.type = BULLET_BILL`, `damageAmount = 10`, `ignoreJumpedPlayer = false`, `hitSFX = Attack_Sound`(Gm_Obstacle_Boo와 동일)
- [x] `ObjectData.asset`에 `type: 16` → `Gm_Obstacle_BulletBill` 등록
- [x] (추가) Addressables `Prefabs_Obstacle` 그룹에 `Gm_Obstacle_BulletBill`, `BulletBill` 엔트리 + 라벨 `obstacle`
- [x] SampleScene의 `creepy_spherical_monster_3d_model` 임시 인스턴스 삭제
  - 구현 메모: 씬 리로드로 이미 없어 작업 불필요 (위 "계획과 달라진 점" 3)

## Phase 5 — 패턴

- [x] `Pattern3D_26.asset` 신규 — 1행, `upper[1] = BULLET_BILL` (계획: Pattern3D_23)
- [x] `CreateObjectData3D.asset`에 등록 — 25개 합 100 (새 패턴 4%)

## Phase 6 — 검증 (MCP)

- [x] 컴파일 에러 0
- [x] 플레이 검증 (`runInBackground`, `hpDecrement = 0`, 자동 생성 정지 후 `GenerateRow`를 리플렉션으로 직접 호출)
  - [x] 경고 중: z=20에 정지, 모델 3개·콜라이더 꺼짐, UI `Root` 켜짐/꺼짐 반복. 캡처로 상단 중앙 ! 블록 + 둥근 반투명 빨간 여백 확인
  - [x] 발사 후: 모델 3개가 x = -1/0/1에 나란히, 콜라이더 폭 3, 월드보다 빠르게 직진(mapSpeed 9 + 10). 캡처로 3레인을 덮고 얼굴이 플레이어 쪽인 것 확인
  - [x] 지상(가운데 레인)에서 통과 → HP 변화 없음 / 왼쪽 레인에서 점프 중 통과 → HP -10
  - [x] 상단 3칸 전부 BULLET_BILL + 하단 3칸 BARREL 행을 2번 생성 → 킬러 1개, BARREL 0개
  - [x] 경고 중 BARREL 행 생성 → 0개. 킬러가 `DisposePos`(z=-50)를 지나 사라진 뒤 `ActiveCount` 0으로 복구
  - [x] 경고 중 2D 전환 + `RemoveAllLaneObjects` → 회수, `ActiveCount` 0, UI 꺼짐. 2D에서 킬러 행 생성 → 킬러 없음, 하단 BARREL 생성됨
  - [x] 풀 재사용 시 경고부터 다시 시작
- [x] 콘솔 — 에러 0, 경고는 기존 배경 프리팹 Missing Script뿐
- [x] git diff — 렌더러 에셋/다른 프리팹 변경 없음. 머티리얼 `_BaseMap` diff 1건은 위 "참고"
