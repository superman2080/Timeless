# ZombieBee (김수한무) 적 — Plan

## 구현 현황 (2026-10-16)
- **완료**: Phase 1~8 전 항목. 미완료 없음. (Phase 7 = 정한 레인 위로 이동 후 투척, Phase 8 = Attack 1초 + 사인 유영 + 기울기)
- 컴파일 에러 0. 플레이 중 신규 에러/경고 0이며, 기존 Missing Script 경고(prop_fan_large)만 남아 있다.
- 플레이 검증 결과:
  - 전체 흐름: 진입 후 (x, 2.0, -6)에서 배회하고, 2초 간격으로 투척(약 2.0s씩)한 뒤 10s에 퇴장, 11.5s에 비활성화된다.
  - 피격: 투척한 공이 지상 플레이어에 맞으면 대미지가 들어간다.
  - 굼바 제한: 제한 레인에서는 LASER/HP 생성이 차단되고 굼바는 생성된다. 상단과 다른 레인에는 영향이 없다.
  - 생성 조건: 동시 1마리, 2D에서는 생성하지 않는다. 2D 전환 시 벌과 공이 회수된다.
- 계획과 달라진 점(아래 각 항목 메모 참고):
  - `hoverZ` 기본값을 4에서 -6으로 바꿨다.
  - 공의 비행 중 스크롤을 제거했다.
  - 착지 오프셋을 필드로 분리했다.
  - Phase 6 테스트는 패턴 확률을 바꾸지 않고 런타임 직접 생성으로 진행했다.

근거: `docs/ZombieBee/Research.md`

## 확정된 결정 (피드백 1·2차)
- **위치**: 상단 레인보다 조금 위에 떠 있다. `hoverY = laneManager.upperLaneHeight + hoverOffset`이며 기본 0.5, 현재 씬 기준 2.0이다.
- **굼바 제한**: 스파이크 볼을 떨어뜨린 **그 레인만** 2초(`attackInterval`) 동안 패턴으로 생성되는 하단 오브젝트를 굼바로 제한한다.
- **스파이크 볼 모델**: 임시 프리미티브 구를 쓴다. 나중에 Blender 모델로 교체한다.
- **동시 등장**: 1마리. 2D에서는 생성하지 않는다. Attack 애니메이션은 1.5배속.
- **본체 접촉 대미지**: 없음.

## 설계 요약
- **새 ObjectType**: enum 끝에 `ZOMBIE_BEE`(8), `SPIKE_BALL`(9)을 추가했다.
- **ZombieBee** (`ZombieBee : InteractionObject`)
  - 진입: 상단 레인(z 19.05)에서 `hoverZ`/`hoverY`까지 날아온다.
  - 이동: 공을 던질 레인을 랜덤으로 정하고 그 레인 위로 이동한다. 위아래로 살짝 흔들리며, `lifeTime`은 진입이 끝난 뒤부터 센다.
  - 공격: `attackInterval`마다 공이 손을 떠나도록 `releaseDelay`만큼 앞서 `Attack` 트리거를 건다.
  - 퇴장: 위·앞으로 날아간 뒤 비활성화된다.
- **SpikeBall** (`SpikeBall : Obstacle`)
  - `flightTime` 동안 벌의 손에서 목표 레인 지면으로 포물선을 그리며 날아간다.
  - 착지한 뒤에는 일반 하단 장애물처럼 흘러온다.
- **생성 경로**: 공도 `Lane.GenerateObstacle(type, position)`을 거친다. 그래서 2D/3D 전환 시 함께 회수된다.

## Phase 1 — 타입 등록
- [x] `InteractionObject.cs`: `ObjectType`에 `KEY_CARD` 뒤로 `ZOMBIE_BEE`, `SPIKE_BALL`을 추가했다.
  - 메모: 파일이 CP949라서 바이트 단위로 ASCII만 삽입해 인코딩을 유지했다.
- [x] `PatternEditorWindow.GetColor`: 두 타입의 색을 추가했다.

## Phase 2 — SpikeBall
- [x] `Assets/02. Scripts/Obstacles/SpikeBall.cs`: `Throw(start, landing)`, `ObstacleMovement` 오버라이드(비행 후 `base`), `OnDisable`에서 `landed = true`.
  - 메모(계획 변경): 처음에는 비행 중 시작점과 착지점도 월드와 함께 흘러가게 했다. 하지만 이 경우 착지점이 `mapSpeed × flightTime`(6.3)만큼 밀려 플레이어 뒤에 떨어졌다. 그래서 비행은 벌 기준(화면 고정)으로 하고, 착지 후부터 흘러가게 바꿨다. 애니메이션의 "코앞에 던지기"와도 일치한다.
- [x] 프리팹 `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_SpikeBall.prefab`
  - 루트: SphereCollider(trigger, r 0.22, center y 0.22), Rigidbody(gravity 꺼짐), `SpikeBall`(damage 20, hitSFX는 굼바와 같은 `Attack_Sound`).
  - 자식 `Model`: 구, 지름 0.44, 콜라이더 제거.
  - 임시 머티리얼: `Assets/06. Materials/M_SpikeBall_Temp.mat`(빨간 URP Lit).
- [x] MCP 검증: 컴파일 OK, 콘솔 신규 에러 0.

## Phase 3 — ZombieBee
- [x] `Lane.cs`
  - `GenerateObstacle(type, position)` 오버로드를 추가했다. 기존 메서드는 이 오버로드로 위임하며, 두 메서드 모두 생성된 오브젝트를 반환한다(생성하지 않으면 null).
  - `goombaOnlyUntil`을 추가했다. 하단 레인에서 이 시각 전이면 `BARREL`과 `SPIKE_BALL` 외의 타입은 건너뛴다.
- [x] `Assets/02. Scripts/Obstacles/ZombieBee.cs`
  - 주요 필드:
    - 이동: `hoverZ`, `hoverOffset`, `moveSpeed` 8, `wanderInterval` 1.5, `bobAmplitude`
    - 공격: `lifeTime` 10, `attackInterval` 2, `releaseDelay` 1.39, `exitTime` 1.5, `landingOffsetZ` -1
    - 참조: `ballSocket`, `animator`
  - `ActiveCount`는 static이고 `OnEnable`/`OnDisable`에서 증감한다. Enter Play Mode에서 도메인 리로드가 꺼져 있으므로 `RuntimeInitializeOnLoadMethod`로 초기화한다.
  - 메모(계획 변경): `hoverZ`를 4에서 **-6**으로 바꿨다.
    - 3D 카메라(0,4,-11, pitch 40°, fov 60)에서는 화면 위쪽 끝이 수평선보다 10° 아래다. 그래서 z 4에 두면 벌이 화면 밖으로 나간다.
    - z -6에서는 화면 상단에 벌 전체가 보이는 것을 캡처로 확인했다.
  - 메모(계획 변경): 착지 지점을 고정값 -1에서 `landingOffsetZ` 필드로 분리했다. 튜닝용이다.
  - 메모: 투척 시각은 생성 후 2, 4, 6, 8, 10초로 총 5회다. 10초 시점의 투척 직후 퇴장한다.
- [x] 프리팹 `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_ZombieBee.prefab`
  - 루트: BoxCollider(trigger, 0.8×1×0.8), Rigidbody(gravity 꺼짐), `ZombieBee`.
  - 자식: `ZombieBee` 모델 프리팹을 Y축 180° 회전해 배치했다.
  - 참조: `ballSocket`은 `Ball` 본, `animator`는 자식 Animator에 연결했다.
- [x] `ZombieBee.controller`: Attack 스테이트 speed 1.5.
- [x] MCP 검증: 컴파일 OK, 프리팹 참조 OK.

## Phase 4 — 생성 규칙 (LaneManager)
- [x] `LaneManager.GenerateRow(ObjectType[], Lane[])`: `ZOMBIE_BEE`이면서 `ActiveCount > 0`이거나 2D이면 건너뛴다.
  - 메모: 파일이 CP949라서 바이트 단위로 삽입하고, 주석은 CP949로 인코딩했다.

## Phase 5 — 데이터 등록
- [x] `ObjectData.asset`에 `ZOMBIE_BEE`, `SPIKE_BALL` 프리팹을 등록했다.
- [x] `Pattern3D_20.asset`: 1행 상단 레인 1에 `ZOMBIE_BEE`를 배치했다.
- [x] `CreateObjectData3D.asset`: Pattern3D_20을 5%로 추가했다. 기존 20개 패턴은 5%에서 4.75%로 낮춰 합계 100을 유지했다.
- [x] 2D 데이터셋은 수정하지 않았다.

## Phase 6 — 플레이 검증 (MCP)
- [x] 테스트 방식
  - 메모(계획 변경): 패턴 확률 에셋을 100%로 바꾸지 않았다. 플레이 중 `upperLanes[1].GenerateObstacle(ZOMBIE_BEE)`로 직접 생성해 테스트했고, 에셋 변경은 없었다.
  - 패턴 경로로도 실제로 생성되는 것을 확인했다. 테스트 중 패턴이 벌을 1마리 생성했다.
- [x] 진입부터 배회, 퇴장까지
  - 생성 후 약 2s 만에 (x, 2.0, -6)에 도착해 레인 사이를 이동했다.
  - 11.5s에 `ActiveCount`가 0이 됐다.
- [x] 투척: 2초 간격으로 랜덤 레인에 투척했다. `goombaOnlyUntil` 시각이 2초씩 갱신되는 것을 확인했다.
- [x] 피격: 지상 플레이어에게 공이 맞으면 `TakeHit`가 호출된다.
- [x] 굼바 제한
  - 제한 레인: LASER와 HP는 null(차단), BARREL은 생성됐다.
  - 상단 HP는 생성됐다.
  - 제한을 해제하면 LASER가 생성됐다.
- [x] 동시 1마리: `GenerateRow`를 두 번 호출해도 `ActiveCount`는 1이었다.
- [x] 2D: 전환 후 `RemoveAllLaneObjects`를 실행하면 벌과 공이 회수됐다(`ActiveCount` 0). 2D에서 `GenerateRow`를 호출해도 생성되지 않았다.
- [x] 콘솔: 신규 에러/경고 0. 에러 1건은 MCP eval 타임아웃 로그로, 게임과 무관하다.
  - 메모: 플레이 중 뷰 전환 테스트 때문에 `GlitchMetetrial.mat`의 `_Alpha`가 바뀌었다. 기존에 알려진 부작용이며 git으로 원복했다.

## Phase 7 — 피드백 3차: 투척 레인 위로 이동 후 던지기
"랜덤한 레인에 던진다"를 처음에는 "현재 위치에서 랜덤 레인으로 던진다"로 잘못 이해했다. 바른 동작은 다음과 같다: 레인을 랜덤으로 정하고, **그 레인 위로 이동한 뒤** 그 레인에 던진다.
- [x] `ZombieBee.cs`
  - 랜덤 배회(`wanderInterval`/`wanderTimer`)를 없애고 `attackLane` 필드를 추가했다.
  - `FixedUpdate`의 목표 x는 `attackLane.LaneX`다.
  - 흐름:
    1. 생성 시 첫 레인을 정한다.
    2. 투척 직후 다음 레인을 정한다(`PickAttackLane`).
    3. 다음 Attack 트리거까지 약 0.6초 동안 이동한다. 최대 2유닛을 이동해도 0.25초면 도착한다.
  - `ThrowBall`은 `attackLane`에 던지고, 같은 레인에 굼바 제한을 건다.
  - 메모(변경): 수명 10초는 **진입 완료(z가 `hoverZ`에 도착) 후부터** 센다. 진입에 약 3초가 걸리는데, 기존처럼 생성 시점부터 세면 첫 투척(2초)이 진입 도중에 일어났다.
    - 생성부터 비활성화까지 전체는 약 3 + 10 + 1.5초다.
- [x] MCP 검증
  - 컴파일 OK, 신규 에러 0.
  - 플레이 결과:
    - 벌이 x=1에 도착한 뒤 레인 2(x=1)에 투척했다.
    - 다음 투척은 레인 1이었고, 벌이 x=0으로 이동한 것을 확인했다.
    - 2초 간격으로 5회 투척한 뒤 퇴장해 비활성화됐다.
  - 플레이 중 바뀐 `GlitchMetetrial.mat`은 다시 원복했다.

## Phase 8 — 피드백 4차: Attack 1초 + 김수한무식 좌우 유영

### 근거 (코드/에셋에서 확인)
- `ZombieBee.controller`의 Attack 스테이트는 현재 speed 1.5다.
  - 클립 길이: 71프레임 / 24fps ≈ 2.958s.
  - 릴리즈: 50프레임 = 2.083s, 정규화하면 0.704.
- `ZombieBee.cs`의 현재 x 이동은 `MoveTowards`(moveSpeed 8)로 목표 레인까지 등속으로 움직인다. 유영 동작은 없다.
- 레인 x는 -1/0/1이다(`laneInterval` 1).

### 설계
- **Attack 1초**
  - speed = 2.958 / 1 = **2.958**.
  - `releaseDelay` = 0.704 × 1s = **0.70s**.
- **한 사이클 타임라인** (2초, 투척 직후를 0으로 둠)

  | 구간 | 시간 | 동작 |
  |---|---|---|
  | 유영 | 0 ~ 약 1.0s | 레인 좌우를 부드럽게 왕복 |
  | 레인 선택·이동 | 약 1.0 ~ 1.3s | 랜덤 레인을 고르고 그 위로 부드럽게 감속하며 도착 |
  | 공격 | 1.3 ~ 2.0s | Attack 트리거 후 0.7s 뒤 릴리즈, 투척 |

  - 첫 사이클(진입 완료 직후)도 같은 순서다.
- **유영 동작**
  - 목표 x = `swayAmplitude × sin(2π × t / swayPeriod)`. 기본값은 진폭 1(양끝 레인까지), 주기 2s다.
  - 위상은 생성 시점부터 계속 흐른다. 그래서 사이클이 바뀌어도 끊기지 않고 이어진다.
- **부드러운 이동**
  - x 이동을 `MoveTowards`에서 `Mathf.SmoothDamp`(smoothTime 약 0.15s)로 바꾼다.
  - 유영에서 레인으로, 레인에서 유영으로 넘어갈 때 가감속이 붙는다.
  - y의 흔들림과 z 진입·퇴장은 기존 방식을 유지한다.
- **기울기 연출(추가 제안)**
  - 루트를 x 속도에 비례해 Z축으로 최대 10° 기울인다. 김수한무처럼 이동 방향으로 기울어지는 느낌을 낸다.
  - 필요 없으면 `tiltAngle = 0`으로 끈다.

### 작업
- [x] `ZombieBee.controller`: Attack 스테이트 speed를 1.5에서 2.958로 변경(클립 길이 / 1s).
- [x] `ZombieBee.cs` 필드
  - `releaseDelay`를 1.39에서 0.70으로 변경(스크립트 기본값과 프리팹 직렬화 값 모두 수정).
  - `swayAmplitude = 1`, `swayPeriod = 2`, `laneMoveTime = 0.3`, `xSmoothTime = 0.12`, `tiltSpeed = 3`, `tiltAngle = 10` 추가.
  - 메모: `xSmoothTime`은 레인 이동 0.3s 안에 도착하도록 계획의 0.15에서 0.12로 줄였다.
  - 메모: 기울기는 `xVelocity / tiltSpeed`(clamp ±1) × `tiltAngle`로 계산한다. 정규화 기준을 `moveSpeed`(8) 대신 `tiltSpeed`(3)로 둬서, 유영 최고 속도(약 3.1 u/s)에서 최대 기울기가 나오도록 했다.
- [x] `ZombieBee.cs` 로직
  - `attackLane`이 null이면 유영 상태이고, 목표 x는 사인 곡선을 따른다.
  - 사이클 순서: `release - releaseDelay - laneMoveTime`에 `PickAttackLane()` → `release - releaseDelay`에 Attack 트리거 → `release`에 투척 → `ResumeSway()`.
  - `FixedUpdate`: x는 `SmoothDamp`, y·z는 `MoveTowards`로 계산해 `Rb.MovePosition`. 기울기는 `Rb.MoveRotation`.
  - 메모(계획 외 추가): 처음에는 투척 후 `attackLane = null`만 했는데, 그 순간 사인 목표값이 엉뚱한 위치에 있어 레인 반대편으로 급히 튀는 문제가 플레이 로그에서 확인됐다. 그래서 `ResumeSway()`가 현재 x에서 사인 곡선이 이어지도록 위상을 맞추게 했다. 위상은 `asin(x / 진폭)`으로 구한다.
- [x] MCP 검증
  - 컴파일 OK, 신규 에러 0.
  - 플레이 로그(0.1s 간격)로 확인한 항목:
    - 투척 후 유영이 현재 위치에서 이어진다(예: x 1.00 → 0.96 → 0.86 → 0.68 …).
    - 유영 중 x가 -0.9~1.0 범위를 오간다.
    - 레인을 고른 뒤 Attack 시작 전에 해당 레인 x(±0.03)에 도착한다.
    - Attack이 정규화 시간 0→1까지 약 0.95~1.0s 걸린다.
    - 이동 방향으로 기울어진다(왼쪽 이동 시 +z, 최대 10°).
    - 2초 간격으로 5회 투척한 뒤 퇴장하고 비활성화된다.
  - 플레이 중 바뀐 `GlitchMetetrial.mat`은 원복했다.

### 결정 (피드백)
- 사인 유영과 기울기를 넣는다. 유영 범위·속도는 기본안(진폭 1, 주기 2s)으로 한다.
- 시간 배분(유영 약 1.0s / 레인 이동 0.3s / 공격 0.7s)은 현재 설계대로 두고, 어색하면 나중에 조정한다.

## 알려진 한계 / 후속 후보
- 공이 착지(z ≈ -7)한 뒤 플레이어(z -9)에 닿기까지 약 0.2s밖에 없다. 실제 반응 시간은 공이 손을 떠나는 순간부터 약 0.9s다. 체감상 어려우면 `landingOffsetZ`를 양수로 바꾸거나 `flightTime`을 늘려 조절한다.
- 공은 임시 구다. Blender 모델이 나오면 프리팹의 `Model` 자식만 교체하면 된다.
- `Lane.spawnedObjects`가 비워지지 않는 문제는 이번 작업 전부터 있던 것이다(Research 5절).
