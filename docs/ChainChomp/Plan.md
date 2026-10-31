# Chain Chomp (멍멍이) — Plan

> 상태: **구현 완료** (Phase 1~6 전부). 플레이 검증 통과, 콘솔 신규 에러/경고 0.
> 근거: `docs/ChainChomp/Research.md`

## 구현 현황 요약

완료
- Phase 1 `ChainPhysics.cs` — Verlet 로프, 로컬 공간 시뮬레이션, 바닥 충돌, 고리 배치/교차 비틀기
- Phase 2 `ChainChomp.cs` (`ChainChomp : Obstacle`) — 대기 깡충 / 즉시 공격 돌진 / 위협 돌진 / 복귀, 사슬 길이·레인 폭 제한
- Phase 3 `ObjectType.CHAIN_CHOMP`(17) + Pattern Editor 색 + `LaneManager` 2D 생성 금지
- Phase 4 `Enemy/ChainChomp.prefab`, `Obstacle3D/Gm_Obstacle_ChainChomp.prefab`, `ObjectData.asset`, Addressables 등록, 씬 임시 오브젝트 삭제
- Phase 5 `Pattern3D_27/28` + `DebugChainChomp3D` + `DebugChainChompPatternSet` → SampleScene 연결
- Phase 6 플레이 검증 (아래 항목 전부 통과)

미완료: 없음

계획과 달라진 점
1. **콜라이더 SphereCollider → CapsuleCollider** (세로, r 0.4, 높이 3, 중심 y 1.5). 구(r 0.4, 중심 y 0.45)로는 점프한 플레이어(최고 높이 1.5)와 겹치지 않아 **점프로 피해졌다**. `ignoreJumpedPlayer`는 판정 조건만 풀어줄 뿐 트리거 자체가 안 일어남. 캡슐을 위로 길게 세워 점프 높이까지 덮었다. 반경은 그대로라 옆 레인은 여전히 안전.
2. **사슬 고리 10 → 16개**, scale (0.12, 0.2, 0.12). 고리가 구간보다 살짝 길어 끊김 없이 보인다.
3. **머리 방향 보정 2건** (플레이 검증 중 발견)
   - 돌진 중 플레이어를 지나치면 목표가 뒤로 바뀌어 머리가 +Z로 돌아섬 → 공격 목표 z를 휴식 위치 뒤로 못 가게 제한하고, 앞(-Z)으로 나갈 때만 이동 방향을 바라보게 함
   - 플레이어가 머리 바로 옆일 때 바라보는 방향이 옆으로 튐 → 플레이어가 0.5유닛 이상 앞일 때만 바라보고, 각도를 정면 ±`threatAngle`로 제한
4. `reachRatio` 기본값 0.9 (말뚝 기준 최대 5.4유닛)

참고
- 에디터 씬이 플레이 후 dirty로 표시되지만, 디스크의 `SampleScene.unity`에 남은 이번 작업 변경은 `ChainChomp` 임시 오브젝트 삭제와 `patternSet` 참조뿐이다 (임시 카메라 등 잔여물 없음 확인). 저장하지 않았다.
- 정식 패턴 세트로 되돌릴 때: SampleScene `LaneManager.patternSet` → `DebugPatternSet`(또는 `PatternSet`)

## 확정된 결정

1. 사슬 고리 모델 = 기본 Capsule (임시). 머리 = `monster_ball_3d_model`을 자식으로 두고 (270, 0, -25)
2. `ChainPhysics`는 새로 작성 (프로젝트에 없음)
3. 위협 돌진 중에도 대미지 판정을 켠다 (자기 레인 안에서만 움직임)
4. 공격은 예비동작 없이 **바로 돌진**
5. 말뚝은 **보이게** 한다 (임시 Cylinder)
6. 정식 세트 확률 등록 대신 **ChainChomp 전용 디버그 패턴 세트를 새로 만들어** SampleScene에 연결해 보여준다

## 설계 요약

- **적 타입**: `ChainChomp : Obstacle`
  - 피격 연출(글리치, 셰이크, SFX)과 `TakeHit`은 `Obstacle`을 그대로 쓴다.
  - `ignoreJumpedPlayer = true` → 점프로 피할 수 없고, **다른 레인에 있는 것만** 회피 방법이다.
- **구조**: 루트는 생성 위치(레인, z = spawnZ)에 있고 월드와 같이 흘러온다.
  - 그 뒤쪽(+Z) 바닥에 말뚝(`Anchor`)이 있다.
  - 말뚝에서 머리(`Head`)까지 사슬(`Chain`)이 이어진다.
  - 머리만 루트 로컬 공간에서 움직인다.
  ```
  Gm_Obstacle_ChainChomp   (Rigidbody, CapsuleCollider(트리거, 세로), ChainChomp)
  ├ Anchor                 (말뚝 위치, 로컬 z = +anchorDistance, 바닥)
  ├ Head                   (돌진하는 머리. 회전은 바라보는 방향)
  │ └ ChainChomp           (Enemy/ChainChomp.prefab → 내부 FBX 자식 회전 (270, 0, -25))
  └ Chain                  (ChainPhysics)
    └ Link_00 ~ Link_NN    (Capsule, 콜라이더 제거)
  ```
- **콜라이더**: 루트의 콜라이더 1개. 매 `FixedUpdate`마다 `center`를 머리 로컬 위치로 옮긴다 (Research 8-1 방법 A).
  - 반경 0.4: 옆 레인을 침범하지 않는 크기 (Research 8-4)
  - 구현 메모: 점프 회피 방지를 위해 세로 CapsuleCollider(높이 3)로 변경 (상단 "계획과 달라진 점" 1)
- **상태 (코루틴 1개로 순환)**
  1. **Idle**: 머리가 휴식 위치에서 제자리 깡충. 플레이어 쪽(-Z)을 바라본다.
  2. **Attack (발동 조건)**: `player.currentLane == currentLane` 이고 `0 < 머리 z - 플레이어 z <= attackRange`, 3D 모드
     - 예비동작 없이 즉시 플레이어 z를 향해 같은 레인을 따라 `lungeSpeed`로 돌진
     - 목표 z는 매 프레임 `플레이어 z - 루트 z`로 갱신한다 (월드가 흘러도 정확히 플레이어 위치까지)
     - 사슬 길이(`chainLength`)를 넘지 않도록 말뚝 기준 거리를 제한
     - 맞추든 못 맞추든 `lungeHold`초 뒤 `returnSpeed`로 복귀 → `attackCooldown` 후 다시 판정
  3. **Threat (범위 밖 위협)**: 발동 조건이 아니면 `threatInterval`마다 실행
     - 앞쪽(-Z 기준 좌우 ±`threatAngle`) 랜덤 방향으로 `threatDistance`만큼 빠르게 돌진했다가 복귀
     - x는 자기 레인 안(±`laneInterval × 0.3`)으로 제한해 옆 레인을 침범하지 않는다
     - 돌진 중 같은 레인에 플레이어가 들어오면 판정은 살아 있다 (자기 레인 안이므로 규칙 유지)
  - 머리는 항상 이동 방향, 대기 중에는 플레이어를 바라본다.
- **ChainPhysics (신규)**: Verlet 로프. 루트 로컬 공간에서 시뮬레이션해 월드 스크롤 영향이 없다.
  - 양 끝 고정: 시작 = `Anchor`, 끝 = 머리 뒤쪽 연결점
  - 중력 처짐, 바닥(y ≥ 고리 반경) 충돌, 거리 제약 반복 (`iterations`)
  - 고리 Transform은 각 구간 중점에 배치. 캡슐 축(Y)을 구간 방향으로 맞추고, 홀/짝 고리를 90° 번갈아 비틀어 사슬처럼 보이게 한다.
  - `OnEnable`에서 직선으로 초기화한다 (풀 재사용)
- **생성 제한**: 2D에서는 생성하지 않는다 (Whomp와 같은 정책). 동시 등장 수는 제한하지 않는다.

## 기본값 (인스펙터에서 조정 가능)

| 항목 | 값 | 비고 |
|---|---|---|
| `damageAmount` | 10 | 다른 적과 동일 |
| `anchorDistance` | 2 | 루트 → 말뚝 (+Z) |
| `chainLength` | 6 | 말뚝 → 머리 최대 거리. 말뚝 기준 최대 4유닛 앞까지 돌진 |
| `attackRange` | 4 | 머리와 플레이어의 z 거리 |
| `lungeSpeed` / `returnSpeed` | 25 / 6 (유닛/s, 루트 기준) | |
| `lungeHold` / `attackCooldown` | 0.15초 / 0.6초 | |
| `threatInterval` / `threatAngle` / `threatDistance` | 1.2초 / 35° / 2.5 | |
| 콜라이더 | 캡슐 r 0.4, 높이 3 | 구현 중 구 → 캡슐로 변경 |
| 사슬 고리 수 | 16 | Capsule 스케일 (0.12, 0.2, 0.12) (구현 중 10 → 16) |

---

## Phase 1 — ChainPhysics

- [x] `Assets/02. Scripts/Obstacles/ChainPhysics.cs` 신규 (UTF-8)
  - 필드: `Transform start`, `Transform end`, `Transform[] links`, `float gravity = 9.81f`, `float damping = 0.98f`, `int iterations = 8`, `float groundY = 0f`, `float linkRadius = 0.06f`, `float segmentLength`
  - 점 배열(`links.Length + 1`개)을 **부모(루트) 로컬 좌표**로 보관
  - `LateUpdate`: 적분 → 양 끝 고정 → 거리 제약 반복 → 바닥 보정 → 고리 Transform 배치/회전
  - `ResetChain()`: start ~ end 직선으로 초기화 (`OnEnable`에서 호출, 외부에서도 호출 가능)
- [x] 컴파일 확인 — 에러 0

## Phase 2 — ChainChomp 스크립트

- [x] `Assets/02. Scripts/Obstacles/ChainChomp.cs` 신규 (`ChainChomp : Obstacle`, UTF-8)
  - `Awake`: `SphereCollider` 캐싱 (Research 5: `Col`이 첫 `OnEnable`에서 null)
  - `OnEnable`: 머리 휴식 위치 복귀, 콜라이더 center 초기화, 사슬 `ResetChain()`, 행동 코루틴 시작
  - `OnDisable`: 코루틴 정지
  - `ObstacleMovement()`: 루트 이동(기본) 유지 + 머리 로컬 위치/회전 갱신 + 콜라이더 center 동기화
  - 행동 코루틴: Idle ↔ Attack / Threat 순환 (설계 요약 참조)
  - `CanAttack()`: 3D, 같은 레인, `0 < 머리 z - 플레이어 z <= attackRange`
  - `OnDrawGizmosSelected`: 공격 범위, 사슬 최대 반경 표시 (Whomp 방식)
- [x] 컴파일 확인 — 에러 0

## Phase 3 — 타입 등록 / 생성 제한

- [x] `ObjectType`에 `CHAIN_CHOMP` 추가 — 값 17 (`InteractionObject.cs`, ASCII 한 줄만 추가해 인코딩 영향 없음)
- [x] `PatternEditorWindow.GetColor`에 `CHAIN_CHOMP` 색 `(0.15f, 0.15f, 0.18f)` 추가
- [x] `LaneManager.GenerateRow(types, targets)`: `CHAIN_CHOMP`는 2D면 건너뜀 (CP949 유지, 변환 왕복 무손실 확인 후 수정)
- [x] 컴파일 확인 — 에러 0

## Phase 4 — 프리팹 / 등록

- [x] `Assets/07. Prefabs/Enemy/ChainChomp.prefab`
  - 루트 (0,0,0) → 자식 FBX 인스턴스, 회전 `(270, 0, -25)`, 스케일 1
- [x] `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_ChainChomp.prefab` (설계 요약의 계층)
  - 루트: Rigidbody(중력 X, 비키네마틱, Continuous Speculative), SphereCollider(트리거, r 0.4), `ChainChomp`
  - 메모: Phase 6에서 CapsuleCollider(트리거, Y축, r 0.4, 높이 3, 중심 y 1.5)로 교체, `colliderHeight` 1.5. `InteractionObject`의 `RequireComponent(Collider)` 때문에 캡슐을 먼저 추가한 뒤 구를 제거해야 지워진다
  - `objectData.type = CHAIN_CHOMP`, `damageAmount = 10`, `ignoreJumpedPlayer = true`, `hitSFX` = 다른 적과 같은 `Attack_Sound`
  - `Anchor`: 임시 Cylinder 말뚝 (scale (0.15, 0.3, 0.15), 바닥에 꽂힌 위치, **CapsuleCollider 제거**)
  - `Chain/Link_00~15`: 기본 Capsule, **CapsuleCollider 제거**
  - 메모: 고리 수 10 → **16**으로 변경. 사슬 길이 6 ÷ 16 = 구간 0.375, 고리 scale (0.12, 0.2, 0.12) → 길이 0.4로 살짝 겹쳐 끊김 없이 보이게 함. `Anchor`(빈 오브젝트, 로컬 z 2) 아래 `Post`(Cylinder) + `ChainStart`(y 0.35), `Head` 아래 `ChainSocket`(머리 뒤 (0, 0.45, -0.4))에 사슬 양 끝 연결
- [x] `ObjectData.asset`에 `type: 17` → `Gm_Obstacle_ChainChomp` 등록
- [x] Addressables `Prefabs_Obstacle` 그룹에 `Gm_Obstacle_ChainChomp`, `ChainChomp` 엔트리 + 라벨 `obstacle`
- [x] SampleScene의 `ChainChomp` 씬 오브젝트 삭제 후 씬 저장 (Research 8-10)
  - 메모: 다른 미저장 변경 없이 저장됨 (씬 diff는 오브젝트 삭제분 + 아래 patternSet 참조)
- [x] 캡처로 정면 방향 확인 → 플레이어 쪽을 안 보면 `Head` 기준 회전을 보정
  - 메모: 정면 렌더 결과 얼굴(눈·입)이 -Z(플레이어 쪽)를 정확히 바라봄 → 보정 불필요. `Head`의 +Z가 바라보는 방향이고 기본 회전은 `LookRotation(back)`

## Phase 5 — 패턴 / 디버그 세트

- [x] `Assets/11. Datas/Patterns/3D/Pattern3D_27.asset` 신규 — 1행, `lower[1] = CHAIN_CHOMP`
  - 레인마다 동작을 보기 위해 하나 더: `Pattern3D_28.asset` — 1행, `lower[0] = CHAIN_CHOMP`, `lower[2] = CHAIN_CHOMP`
- [x] `Assets/11. Datas/DebugChainChomp3D.asset` 신규 (`CreateObjectSO`) — `Pattern3D_27` 50%, `Pattern3D_28` 50%, `rowDistance` 1.8 (기존 디버그 세트와 동일)
- [x] `Assets/11. Datas/DebugChainChompPatternSet.asset` 신규 (`PatternSetSO`)
  - `threshold2DView` 0.5 / `threshold3DView` 0.7 (기존 `DebugPatternSet`과 동일)
  - `tiers3D`: maxRatio 1 → `DebugChainChomp3D`
  - `tiers2D`: maxRatio 1 → 기존 `DebugCreateObject2D` (2D에서는 생성 금지라 그대로 재사용)
- [x] SampleScene `LaneManager.patternSet`을 `DebugPatternSet` → `DebugChainChompPatternSet`으로 교체 후 씬 저장
  - 되돌릴 때는 이 참조만 다시 `DebugPatternSet`(guid `3021758a…`)으로 바꾸면 된다
- 정식 세트(`CreateObjectData3D`)에는 등록하지 않는다 (아래 "확인이 필요한 점" 1)

## Phase 6 — 검증 (MCP)

- [x] 컴파일 에러 0, 콘솔 신규 경고 0 (경고는 기존 배경 프리팹 Missing Script뿐)
- [x] 플레이 검증 (`runInBackground`, `hpDecrement = 0`, `ChangeLane(1, true)`, `GenerateRow` 직접 호출)
  - [x] 같은 레인 + 범위 안: 즉시 돌진 → HP 99.9 → 89.9
  - [x] 점프 중 피격 — 처음엔 **실패**(구 콜라이더가 점프한 플레이어에 닿지 않음) → 캡슐로 바꾼 뒤 `IsJump = true` 상태에서 HP 99.9 → 89.9 확인
  - [x] 다른 레인(멍멍이 레인 0/2, 플레이어 1 / 멍멍이 1, 플레이어 2): 공격 돌진 없음, 위협 동작만, HP 변화 없음
  - [x] 위협 돌진 머리 x가 ±0.30(= 레인 간격 × 0.3) 안에 머묾, 돌진 거리 사슬 제한 안
  - [x] 사슬: 고리 최저 y 0.060(= 고리 반경)으로 바닥 아래로 안 꺼짐. 측면 렌더로 말뚝~머리 연결, 바닥에 처짐, 스크롤 중 뒤로 끌리지 않음 확인
  - [x] 플레이어를 지나친 뒤에도 머리가 정면(-Z) 유지 (보정 후)
  - [x] 2D 전환 + `RemoveAllLaneObjects` → 2마리 회수, 2D에서 3레인 모두 CHAIN_CHOMP 행 생성 → 0마리
  - [x] 풀 재사용: 회수된 2개 중 하나가 z=20, 머리 휴식 위치, 정면, 콜라이더 중심 (0, 1.5, 0)으로 재등장
- [x] git diff — 렌더러 에셋 변경 없음. 머티리얼 `creepy_spherical_monster_3d_model.mat` diff는 이전 킬러 작업 때부터 있던 것

---

## 확인이 필요한 점

1. **정식 세트 등록**: 이번에는 전용 디버그 세트로만 보여주고 `CreateObjectData3D`에는 넣지 않는다. 정식 세트에도 넣으려면 확률을 알려주세요.
2. **예비동작 없는 즉시 돌진**: 범위 4 안에서 같은 레인이면 피할 시간이 거의 없다(돌진 25유닛/s). 사용자 결정: 실제 플레이 후 `attackRange`/`lungeSpeed` 조정 여부 논의.
