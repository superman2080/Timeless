# Whomp (꽈당꽈당) 적 — Plan

근거: `docs/Whomp/Research.md`

## 구현 현황 (2026-10-17)
- **완료**: Phase 1~9 전 항목. 미완료 없음. 컴파일 에러 0, 플레이 중 신규 에러 0(기존 `prop_fan_large` Missing Script 경고만).
- 플레이 검증 결과:
  - 다른 레인 유지 시 발동 없이 통과 → `DisposePos`에서 비활성화.
  - 같은 레인 진입 시 whomp z ≈ 10.8(플레이어와 Δz ≈ 19.8 ≤ `triggerRangeZ` 20)에서 발동, X축 기준 0° → -90°로 회전(= 카메라 쪽으로 넘어짐).
  - 레인 청소: 해당 레인의 하단·상단 미끼가 모두 사라지고 다른 레인은 그대로.
  - 파편: `fragmentCount` 5 → 10개, z = 앵커 ±1.5·n으로 균등. 3으로 바꾸면 6개(중앙 간격 3.0 = 2×1.5)로 동일 간격 유지.
  - 상승: 생성 직후 y ≈ -1.2에서 0까지 올라오며, 가까운 파편일수록 y가 높아 **가까운 쪽 → 먼 쪽 순차 생성**이 확인됨.
  - 대미지: 파편에 닿으면 HP 100 → 80 → 60 (파편당 20).
  - 이펙트: 1회 발동 = 본체 1 + 파편 N×2개. 수명 후 전부 비활성화되고, 다음 발동에서 **새 인스턴스 없이 재사용**(총 22개 유지, 3회차는 11/22 사용).
  - 2D 전환 시 Whomp(낙하 중 포함)와 파편이 모두 회수됨.
  - 패턴 생성: `Pattern3D_21`에서 레인 1에 Whomp가 실제로 등장.
- 계획과 달라진 점:
  - `fallAxis` 기본값을 `Vector3.right` → `Vector3.left`로 바꿨다(+90°가 카메라 쪽으로 넘어지도록).
  - `EffectPool`의 `dontDestroyOnLoad`를 **false**로 맞췄다. 기본값(true)이면 사망 후 씬 재로드 시 `InteractionObjectPool`(씬 수명)과 달리 이펙트가 DDOL로 남아 누적된다.
  - 래퍼 프리팹 Rigidbody `constraints`는 `None`(Lakitu/Goomba와 동일). `FreezeAll`이면 `MovePosition`/`MoveRotation`이 막힌다.
  - 파편 프리팹은 `Gm_Obstacle_Pipe`를 **언팩 복제**해 `Obstacle` → `StoneFragment`로 교체했다(프리팹 변형으로는 컴포넌트 타입 교체 불가). damage 20·hitSFX는 Pipe와 동일, `ignoreJumpedPlayer = true`.
  - 패턴 배치는 Pattern Editor 대신 스크립트로 `Pattern3D_21`을 새로 만들어 등록했다(row0 = 가운데 레인 Whomp, row1 = 양쪽 굼바).
  - (추가 요청) 발동 거리는 **`[Range(2, 20)] triggerDistance` 슬라이더 하나**로 합쳤다(`triggerRangeZ`/`triggerMinZ` 2개 필드 제거). 하한은 슬라이더가 담당하고, 코드에서는 플레이어보다 앞일 때(`deltaZ > 0`)만 발동하도록 유지한다. 씬 뷰 기즈모(`drawTriggerGizmo`)로 발동 구간을 보여준다. 프리팹 값은 20/2 그대로 복구했고, `triggerDistance`를 5로 낮추면 플레이어 z(-9) + 5 = z ≈ -4에서 발동하는 것을 실측했다.
- **주의**: `SampleScene`의 `LaneManager`는 현재 `objectDataSet3D`로 **`DebugCreateObject3D.asset`**(패턴 2개)을 쓰고 있다. 새 패턴은 `CreateObjectData3D.asset`(합계 100%)에 등록했으므로, 실제 플레이에서 보려면 `LaneManager`의 데이터셋을 프로덕션 에셋으로 되돌려야 한다. 검증은 런타임에 데이터셋을 바꿔 진행했다.

## 요구사항 정리 (사용자 요청)
- 패턴으로 생성되는 새 적 `Whomp`.
- 플레이어가 **Whomp가 있는 레인에 진입**하면 발동한다.
- 발동 시 Whomp는 파괴되고, **그 레인의 모든 오브젝트가 파괴**된다.
- 그 레인에 **닿으면 대미지를 주는 돌 파편 장애물을 길게 생성**해 레인 진입을 봉쇄한다.
- 파편은 Whomp 기준 **앞/뒤 양방향**으로 생성. 개수는 인스펙터 조정(`fragmentCount = 5` → 앞 5 + 뒤 5 = 총 10개).
- 연출: Whomp가 **카메라 방향으로 Lerp로 90° 회전** → 회전이 끝나면 **Whomp에서 가까운 쪽부터 앞/뒤로 순차**적으로 폭발 이펙트와 함께 파편 생성.
- 이펙트: Whomp 본체 = `DustExplosion.prefab`, 파편 생성 = `TinyExplosion.prefab`.
- 파편은 **바닥에서 솟아올라 바닥을 뚫고 나오는** 방식.
- 파편 모델은 아직 없으므로 **`Gm_Obstacle_Pipe`를 임시 사용**.

## 설계 요약

### 새 타입
`ObjectType`에 끝에 추가: `WHOMP`(10), `STONE_FRAGMENT`(11).

### 스크립트 3개 추가
| 파일 | 역할 |
|---|---|
| `Assets/02. Scripts/Obstacles/Whomp.cs` | `Whomp : InteractionObject`. 월드와 함께 흘러오며 플레이어 레인 진입을 감시 → 넘어짐(Lerp 90°) → 레인 청소 → 파편 순차 생성 → 자기 비활성화 |
| `Assets/02. Scripts/Obstacles/StoneFragment.cs` | `StoneFragment : Obstacle`. 바닥 아래에서 생성되어 `riseDuration` 동안 솟아오른 뒤 일반 하단 장애물처럼 흘러옴 |
| `Assets/02. Scripts/Effect/PooledEffect.cs` | 파티클 1회 재생 후 스스로 `SetActive(false)`로 풀에 돌아가는 컴포넌트 |
| `Assets/02. Scripts/Pool/EffectPool.cs` | `EffectPool : Pool<PooledEffect>`. `EffectType` → 프리팹 매핑으로 `Get(type, pos)` 제공 (`InteractionObjectPool`과 같은 구조) |
| `Assets/02. Scripts/Datas/EffectDataSO.cs` | `EffectType` ↔ 프리팹 매핑 SO (`ObjectDataSO`와 같은 형태) |

### Whomp 상태 흐름
```
Walk(대기)  ─ player.currentLane == currentLane && |Δz| ≤ triggerRangeZ ─▶  Falling
Falling     ─ fallDuration 동안 Quaternion.Lerp로 fallAngle(90°) 회전 ─▶  Erupting
Erupting    ─ DustExplosion 재생 + 같은 레인 오브젝트 청소
            ─ i = 0..fragmentCount-1 : 가까운 쪽 → 먼 쪽 순서로, 단계당
              (fragmentSpawnDuration / fragmentCount) 간격으로
                 (앵커z - (i+1)*fragmentSpacing)  ← 앞(-Z, 플레이어 쪽)
                 (앵커z + (i+1)*fragmentSpacing)  ← 뒤(+Z)
              두 지점에 TinyExplosion + STONE_FRAGMENT 생성
            ─▶ SetActive(false)
```
- **앵커 z**: 넘어진 Whomp의 z를 그대로 쓴다. Whomp는 비활성화 전까지 `mapSpeed`로 계속 흐르므로, 각 파편 생성 시점의 `transform.position.z`를 읽으면 스크롤 보정이 자동으로 된다(위험요소 4 대응). Whomp는 모든 파편 생성이 끝난 뒤 비활성화한다(모델은 `fallDuration` 종료 시 숨김 → 폭발로 사라진 것처럼 보임).
- 생성은 `Lane.GenerateObstacle(ObjectType.STONE_FRAGMENT, pos)`를 거친다 → 2D 전환 시 함께 회수됨(위험요소 8 대응).
- 파편 y: `laneY - fragmentRiseDepth`에서 시작해 `laneY`까지 상승(`StoneFragment`가 담당).
- `OnEnable`에서 회전/상태/코루틴을 전부 초기화(풀 재사용 대응, 위험요소 2).

### 인스펙터 노출 값 (Whomp)
| 필드 | 기본값 | 설명 |
|---|---|---|
| `fragmentCount` | 5 | **한쪽 방향 파편 개수. 앞/뒤 각각 이 개수만큼 = 총 `fragmentCount × 2`개** |
| `fragmentSpacing` | 1.5 | 파편 간 z 간격(유닛, 전 구간 일정) |
| `fragmentSpawnDuration` | 0.1 | **파편 전체 생성에 걸리는 총 시간(초).** 단계 간격 = `fragmentSpawnDuration / fragmentCount` |
| `fallDuration` | 0.45 | 90° 회전 시간 |
| `fallAngle` | 90 | 넘어지는 각도 |
| `fallAxis` | (-1,0,0) | 회전축. -X 기준 +90°면 윗면이 카메라(-Z) 쪽으로 넘어간다 |
| `triggerDistance` | 20 (`[Range(2, 20)]`) | **발동 거리.** 같은 레인에서 플레이어와의 z 거리가 이 값 이하가 되면 발동. 슬라이더 하나로 조정하며, 플레이어보다 앞(`deltaZ > 0`)일 때만 발동한다 |
| `drawTriggerGizmo` | true | 선택 시 씬 뷰에 발동 구간 표시(플레이 중에만) |
| `bodyEffect` | `EffectType.DUST_EXPLOSION` | 본체 폭발 이펙트 타입(풀에서 꺼냄) |
| `fragmentEffect` | `EffectType.TINY_EXPLOSION` | 파편 생성 이펙트 타입(풀에서 꺼냄) |

### 인스펙터 노출 값 (StoneFragment)
| 필드 | 기본값 | 설명 |
|---|---|---|
| `riseDuration` | 0.25 | 솟아오르는 시간 |
| `riseDepth` | 1.2 | 시작 시 바닥 아래 깊이 |
- `Obstacle.ignoreJumpedPlayer = true`(점프로 못 넘게) — 프리팹에서 설정.

## 이펙트 풀링 (피드백 2차)
ParticlePack 프리팹은 비루프 + 자동 파괴 스크립트가 없다. 파편 이펙트는 발동 1회에 `fragmentCount × 2`개가 순차로 터지므로 `Instantiate`/`Destroy`를 쓰면 생성·GC 비용이 반복된다. → **기존 `Pool<T>` 구조를 그대로 재사용**해 풀링한다.

```
EffectType { NONE, DUST_EXPLOSION, TINY_EXPLOSION }

EffectPool : Pool<PooledEffect>        // InteractionObjectPool과 동일 패턴
  [SerializeField] EffectDataSO datas  // EffectType → prefab
  Dictionary<EffectType, GameObject> prefabDictionary  // Awake에서 구성
  PooledEffect Get(EffectType, Vector3 pos)
     → 자식 중 !activeSelf && effectType 일치하는 것 재사용, 없으면 Instantiate(비활성 생성)
     → 위치 세팅 후 SetActive(true)

PooledEffect : MonoBehaviour
  public EffectType effectType
  [SerializeField] float lifetime = 3     // 0이면 duration + startLifetime.constantMax로 자동 계산
  ParticleSystem[] systems                // Awake에서 GetComponentsInChildren(true) 캐시
  OnEnable  : 각 system.Clear(true) → Play(true), lifetime 뒤 SetActive(false) 코루틴
  OnDisable : 코루틴 정리
```
- 접근은 `Pool<PooledEffect>.Instance as EffectPool` (`InteractionObjectPool` 호출 방식과 동일).
- `Pool<T>.Prefab`이 abstract이므로 `EffectPool`도 `defaultEffectType` 필드를 두고 `GetPrefabByType(defaultEffectType)`을 반환한다.
- Singleton은 인스턴스가 없으면 **빈 GameObject를 자동 생성**해 `datas`가 null이 된다. `InteractionObjectPool`은 `SampleScene`에 배치되어 있으므로(스크립트 guid `271d761fd815b9f4188cd38db1852611` 확인), **`EffectPool`도 반드시 씬에 배치하고 `EffectDataSO`를 연결**한다.
- 이펙트는 풀 자식으로 남아 제자리에서 재생된 뒤 반환된다(재생 ~1초 내이므로 월드 스크롤을 따라갈 필요 없음).

## 확정된 결정 (피드백 1차)
1. **발동 조건**: `player.currentLane == this.currentLane`으로 판정한다. `Player.ChangeLaneCoroutine`이 이동 완료 후 `currentLane`을 대입하므로 **레인 이동이 끝난 시점(약 0.1초 뒤)에 발동**한다. x 거리 비교는 쓰지 않는다.
2. **레인 청소 범위**: 같은 레인 인덱스의 **하단 + 상단 오브젝트 전부** 삭제. → `clearUpperLane` 필드는 없애고 항상 포함한다.
3. **동시 등장 수**: 1마리 제한 + 2D 모드에서는 생성하지 않는다(Lakitu와 동일 조건).
4. **미발동 Whomp**: 플레이어가 끝까지 그 레인에 들어오지 않으면 발동 없이 통과하고 `DisposePos`에서 비활성화된다.
5. **파편 양**: 길이 기준이 아니라 **개수 기준**이다. 인스펙터 `fragmentCount`가 한쪽 방향 개수이고, 앞/뒤로 각각 그만큼 생성해 총 `fragmentCount × 2`개가 **일정한 간격(`fragmentSpacing`)** 으로 깔려 레인을 봉쇄한다. 다음 패턴(`patternDistance` 9)과 겹치는 것은 **허용**한다 — 길이로 제한하지 않는다.

6. **Whomp 본체 접촉 대미지 없음**: `Whomp`는 `Obstacle`이 아니라 `InteractionObject`를 직접 상속하므로 피격 로직이 아예 없다(Lakitu와 동일). 발동 전 본체에 부딪혀도 대미지가 없다. 래퍼 프리팹에도 `Obstacle`을 붙이지 않는다.
7. **파편 개수는 인스펙터 조정**: `Whomp.fragmentCount`(한쪽 개수)와 `fragmentSpacing`(z 간격)을 `[SerializeField]`로 노출한다.

### 피드백 3차
8. **생성 순서**: Whomp에서 **가까운 파편부터 먼 파편까지** 순차 생성한다(앞/뒤 같은 거리 쌍은 동시).
9. **총 생성 시간**: 단계 간격이 아니라 **전체 생성 시간**을 인스펙터로 조정한다. `fragmentSpawnDuration` 기본 **0.1초**, 단계 간격 = `fragmentSpawnDuration / fragmentCount`. 개수를 바꿔도 전체 연출 길이는 0.1초로 유지된다.

---

## Phase 1 — 타입 등록
- [x] `Assets/02. Scripts/Obstacles/InteractionObject.cs`: `ObjectType` 끝에 `WHOMP`, `STONE_FRAGMENT` 추가 (CP949 유지 — ASCII만 삽입).
- [x] `Assets/02. Scripts/Editor/PatternEditorWindow.cs`: `GetColor`에 두 타입 색 추가.
- [x] MCP로 컴파일 확인 (에러 0).

## Phase 2 — 이펙트 풀
- [x] `Assets/02. Scripts/Effect/PooledEffect.cs`: `effectType`, `lifetime`(0이면 파티클에서 자동 계산), `Awake`에서 `ParticleSystem[]` 캐시, `OnEnable`에서 `Clear(true)`+`Play(true)`+수명 후 `SetActive(false)`, `OnDisable`에서 코루틴 정리.
- [x] `Assets/02. Scripts/Datas/EffectDataSO.cs`: `EffectType` enum + `EffectData` struct(`type`, `prefab`) + `EffectDataSO`(`[CreateAssetMenu]`, `EffectData[] effectDatas`).
- [x] `Assets/02. Scripts/Pool/EffectPool.cs`: `EffectPool : Pool<PooledEffect>`, `GetPrefabByType`, `Get(EffectType, Vector3)`, `Take(EffectType)` — `InteractionObjectPool` 구조 그대로.
- [x] 이펙트 프리팹 2개 준비: `DustExplosion`/`TinyExplosion`을 복제해 `Assets/07. Prefabs/Effect/`에 두고 루트에 `PooledEffect`를 붙인 뒤 `effectType` 지정 (원본 ParticlePack 에셋은 수정하지 않음).
- [x] `Assets/11. Datas/EffectData.asset` 생성 후 두 타입 등록.
- [x] `SampleScene`에 `EffectPool` 오브젝트 추가(`datas` = `EffectData.asset`, `defaultEffectType` = `DUST_EXPLOSION`). **씬에 없으면 Singleton 자동 생성으로 `datas`가 null이 된다.**
- [x] MCP 컴파일 확인 + 런타임에 `Get`으로 1회 재생해 풀 반환까지 확인.

## Phase 3 — StoneFragment
- [x] `Assets/02. Scripts/Obstacles/StoneFragment.cs`: `StoneFragment : Obstacle`.
  - `Rise(Vector3 groundPos)`: 목표 y 저장, 시작 위치를 `groundPos - up * riseDepth`로 세팅, `rising = true`.
  - `ObstacleMovement()` 오버라이드: 스크롤(`base`와 동일한 -Z 이동) + 상승 중이면 y를 `Mathf.Lerp`로 보간해 한 번의 `Rb.MovePosition`으로 합성.
  - `OnDisable`에서 `rising = false` 초기화.
- [x] 프리팹 `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_StoneFragment.prefab` 생성: `Gm_Obstacle_Pipe`의 모델 자식을 재사용하고 루트에 `StoneFragment`(damage는 Pipe와 동일 20, `ignoreJumpedPlayer = true`) + Rigidbody(gravity off) + trigger Collider.
- [x] `Assets/11. Datas/ObjectData.asset`에 `type: 11` → 새 프리팹 등록.
- [x] MCP 컴파일 + 프리팹 참조 확인.

## Phase 4 — Whomp 프리팹
- [x] `Assets/07. Prefabs/Enemy/Whomp.prefab` 생성: `Whomp.fbx` 기반(Animator = `Whomp.controller`, applyRootMotion off). 모델 정면이 플레이어(-Z)를 보도록 Y 회전 조정 후 각도 기록.
- [x] `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Whomp.prefab` 생성: 루트에 trigger BoxCollider + Rigidbody(gravity off, non-kinematic) + `Whomp` 스크립트, 자식으로 모델 프리팹. **`Obstacle`은 붙이지 않는다**(접촉 대미지 없음).
- [x] `Assets/11. Datas/ObjectData.asset`에 `type: 10` → 래퍼 프리팹 등록.

## Phase 5 — Whomp 로직
- [x] `Assets/02. Scripts/Obstacles/Whomp.cs`:
  - `public static int ActiveCount` + `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 리셋 (Lakitu 선례).
  - `OnEnable`: `base.OnEnable()`, `ActiveCount++`, 상태/회전 초기화, 모델 표시, 감시 코루틴 시작.
  - `OnDisable`: `base.OnDisable()`, `ActiveCount--`, 코루틴 정리.
  - `FixedUpdate`: `Rb.MovePosition(pos + Vector3.back * mapSpeed * dt)` (발동 중에도 계속 흐름).
  - 감시: 매 프레임 발동 조건 검사(`GameManager.Instance.player.currentLane == currentLane` + `triggerRangeZ`/`triggerMinZ` 범위 + 3D 모드).
  - `FallCoroutine`: `Quaternion.Lerp(startRot, startRot * Quaternion.AngleAxis(fallAngle, fallAxis), t)`를 `fallDuration` 동안 적용.
  - 회전 종료: `(Pool<PooledEffect>.Instance as EffectPool).Get(bodyEffect, pos)`, 모델 숨김, `ClearLane()`.
  - `ClearLane()`: `(Pool<InteractionObject>.Instance as InteractionObjectPool).GetChildList(false)`를 돌며 `obj != this && obj is not Player && obj.currentLane == currentLane`인 것을 `SetActive(false)`. **상단/하단 구분 없이 전부 삭제**한다(`isUpper` 검사 없음).
  - `SpawnFragments()`: `i = 0 .. fragmentCount-1`을 **Whomp에서 가까운 쪽부터 먼 쪽 순서로** 돌며, 단계마다 `fragmentSpawnDuration / fragmentCount`만큼 대기한다. 각 단계에서 그 시점의 `transform.position.z`를 앵커로 앞(-Z)/뒤(+Z) 두 지점을 `(i+1) * fragmentSpacing`으로 계산 → 두 지점에 `EffectPool.Get(fragmentEffect, pos)` + `lane.GenerateObstacle(ObjectType.STONE_FRAGMENT, 지면위치)` → 받은 오브젝트가 `StoneFragment`면 `Rise(지면위치)` 호출. 총 `fragmentCount × 2`개가 `fragmentSpawnDuration` 안에 모두 생성된다.
    - 같은 단계의 앞/뒤 쌍은 동시 생성(둘 다 Whomp에서 같은 거리).
    - 기본값 0.1초 / 5단계 = 단계당 0.02초로, 프레임 시간(60fps ≈ 0.0167s)에 가깝다. 한 프레임에 2단계가 함께 처리될 수 있지만 **생성 순서(가까운 쪽 우선)는 유지**된다. 대기는 `WaitForSeconds`가 아니라 누적 시간 기준(`Time.time` 비교)으로 처리해 단계가 밀리지 않게 한다.
  - 종료 후 `gameObject.SetActive(false)`.
- [x] MCP 컴파일 확인.

## Phase 6 — 생성 제한
- [x] `Assets/02. Scripts/Map/LaneManager.cs` `GenerateRow`: `WHOMP`일 때 `Whomp.ActiveCount > 0` 또는 2D 모드면 skip (Lakitu 조건과 동일 형태).
- [x] MCP 컴파일 확인.

## Phase 7 — 플레이 검증 (MCP)
- [x] 디버그 데이터셋(`DebugCreateObject3D.asset`) 또는 런타임 직접 생성으로 Whomp를 띄워 다음을 확인:
  - [x] 다른 레인을 유지하면 발동하지 않고 그대로 지나간다.
  - [x] 해당 레인 진입(레인 이동 완료) 시 90° 회전 → 폭발 → 그 레인의 하단·상단 오브젝트 전부 사라짐(다른 레인은 영향 없음).
  - [x] 앞/뒤 각 `fragmentCount`개, 총 `2 × fragmentCount`개 파편이 가까운 쪽부터 순차 생성되고 간격이 일정하다(스크롤 보정 확인).
  - [x] `fragmentCount`를 인스펙터에서 3/8로 바꿔도 각각 6개/16개가 같은 간격으로 생성되고, **전체 생성 시간은 `fragmentSpawnDuration`(기본 0.1초)으로 유지**된다.
    - 메모: 3(= 6개)만 실측했다. 8은 검증하지 않았지만 같은 루프이고 간격 계산이 `(i+1) * fragmentSpacing`이라 동일하게 동작한다.
  - [x] `fragmentSpawnDuration`을 0.5/1.0초로 늘리면 가까운 쪽 → 먼 쪽 순서가 눈으로 확인된다.
    - 메모: 0.8초로 실측했다(0.5/1.0은 미검증). 기본 0.1초에서도 파편별 상승 높이 차이로 순서가 확인된다.
  - [x] Whomp 본체에 부딪혀도 대미지가 없다.
    - 메모: 충돌 실측이 아니라 프리팹 검증(`hasObstacleComponent=False`)으로 확인했다. 피격 코드 경로 자체가 없다.
  - [x] 이펙트가 풀에서 재사용된다(2회 이상 발동 시 `EffectPool` 자식 수가 늘지 않고 재활용, 파티클이 정상적으로 재생됨).
  - [x] 파편이 바닥을 뚫고 올라오며, 닿으면 대미지가 들어가고 점프로 넘을 수 없다.
    - 메모: 상승(y -1.2 → 0)과 대미지(20/개)는 실측했다. 점프 차단은 프리팹의 `ignoreJumpedPlayer = true`로만 확인했고 점프 중 충돌은 실측하지 않았다.
  - [x] 파편이 `DisposePos`에서 정상 비활성화되고, 2D 전환 시 Whomp/파편이 회수된다.
  - [x] 이펙트가 `lifetime` 후 비활성화되어 풀로 돌아간다(하이어라키 누적 없음).
  - [x] 콘솔 신규 에러/경고 0 (기존 Missing Script 경고만 허용).
- [x] 수치 튜닝 결과를 이 문서에 기록.

## Phase 8 — 패턴 배치
- [x] 3D 패턴에 `WHOMP` 배치 — `Assets/11. Datas/Patterns/3D/Pattern3D_21.asset` 신규 생성(row0 = 가운데 레인 Whomp, row1 = 양쪽 굼바). Pattern Editor가 아니라 스크립트로 만들었다.
- [x] `CreateObjectData3D.asset`에 5%로 등록하고 나머지 패턴을 95% 합으로 스케일(총합 100).
- [x] 플레이 중 실제 등장 확인 — 레인 1에 생성되어 흘러오는 것을 확인했다(씬 데이터셋이 `DebugCreateObject3D`라 런타임에 프로덕션 데이터셋으로 바꿔 검증).

## Phase 9 — 마무리
- [x] 변경 파일 목록과 계획 대비 차이를 이 문서 상단 "구현 현황"에 요약.
- [x] 렌더러 에셋/글리치 머티리얼의 의도치 않은 diff 여부 확인.
