# SpawnOptimization — Plan

## 구현 현황 요약
- 완료: Phase 1~4 전부, Phase 5 중 컴파일·콘솔 확인, 플레이 중 프리워밍·런타임 할당 측정
- 미완료: 2D 전환 회수 / Whomp 자폭 / Lakitu 투척 장면을 직접 눈으로 확인하는 테스트, 실제 프레임 드랍 전후 비교(프로파일러) → 사용자 플레이 확인 필요
- 범위 밖 발견: `MapManager.GenerateBackground`(배경 타일)도 같은 패턴(`GetComponentsInChildren` 매번 + 런타임 Instantiate)이 남아 있음. 배경 생성 타이밍 렉이 보이면 같은 방식으로 후속 작업.

근거: [Research.md](Research.md). 목표: 적 생성 프레임의 할당·계층 탐색·런타임 Instantiate 제거. 동작(스폰 규칙, 반환 규약 `SetActive(false)`)은 그대로.

## Phase 1 — 풀 조회를 캐시 리스트로 (핵심, 문제 1)
- [x] `Pool/Pool.cs`
  - `private readonly List<T> items` + `public IReadOnlyList<T> Items`. `Awake`에서 씬에 미리 있던 자식만 한 번 수집.
  - `protected T Create(GameObject prefab)` 헬퍼(Instantiate + 목록 등록).
  - `GetChildList` 삭제, `Get()`도 `items` 순회. `System.Linq` 제거.
- [x] `Pool/InteractionObjectPool.cs` — `Take`가 타입별 `Dictionary<ObjectType, List<InteractionObject>>`만 순회. 생성은 `CreateInactive` 경유. (CP949 유지)
- [x] `Pool/EffectPool.cs` — 같은 방식(`Dictionary<EffectType, List<PooledEffect>>`).
- [x] `Obstacles/Lakitu.cs`, `Obstacles/Whomp.cs` — `Pool<InteractionObject>.Instance.Items` 순회 + 비활성/null skip.

## Phase 2 — 프리워밍 (문제 2)
- [x] `InteractionObjectPool`: `List<PrewarmData> prewarm`(type + count) 인스펙터 목록, 미지정 타입은 `DefaultPrewarmCount = 3`.
  - 계획과 다른 점: 생성 시점을 `Awake` → **`Start`**로 변경. 인스턴스가 생성 직후 한 번 활성화되면서 OnEnable이 돌기 때문에, `GameManager`(실행 순서 -100) 등 다른 매니저 초기화 뒤에 해야 안전함.
  - 이미 있는 개수만큼은 건너뛰고 부족분만 생성.
- [x] `EffectPool`: 같은 방식 (`EffectType` + count, 기본 3).
- [x] SampleScene 인스펙터 값 저장: `Pool` = `STONE_FRAGMENT` 10, `LAKITU` 1, `WHOMP` 1 / `EffectPool` = `TINY_EXPLOSION` 10.
  - 메모: 씬 저장 시 Cinemachine `m_NoiseOffsets` 값도 diff에 같이 잡힘(Cinemachine이 자동으로 바꾸는 값, 동작 영향 없음).

## Phase 3 — Lane 목록 누수 수정 (문제 3)
- [x] `Map/Lane.cs` — `GenerateObstacle`에서 `Add` 전에 비활성/null 항목 `RemoveAll`(캡처 없는 람다라 캐싱됨), `RemoveAllObjects` 끝에 `Clear()`. 풀이 null을 주면 NRE 대신 null 반환.

## Phase 4 — yield 객체 캐싱 (문제 4, 경미)
- [x] `Map/LaneManager.cs` — `static readonly WaitForFixedUpdate WaitFixed` 재사용. (CP949 유지)

## Phase 5 — 검증 (MCP)
- [x] 컴파일: 에러 0, 경고 0.
- [x] SampleScene 플레이: 시작 시 풀 71개 / 이펙트 13개 미리 생성 확인(타입별 3개, 지정 타입은 지정 개수). t=12s → t=21s 사이 추가 생성 없음. 게임 코드 에러 없음(경고는 기존 Missing Script, 기존 cubemap 버전 경고뿐).
  - `HP` 아이템은 13개까지 늘어남 → 동시에 많이 깔리는 타입. 런타임 생성이 거슬리면 `Pool` 인스펙터에서 `HP` 개수를 13 정도로 올리면 됨.
- [x] 할당 측정: 플레이 중 `InteractionObjectPool.Get` 100회 → **GC 할당 0B**, 100회에 9.7ms(1회 약 0.1ms, 대부분 활성화/OnEnable 비용).
- [ ] 프로파일러로 실제 생성 프레임의 드랍 전후 비교 — 사용자 플레이로 확인 필요.

## 결정 사항
- 프리워밍 개수: 풀 인스펙터에서 타입별 지정, 미지정 타입은 3개. (사용자 피드백)
- 셰이더 워밍업(`ShaderVariantCollection`): 이번 범위에서 제외. 타입별 **첫 등장 때만** 드랍이 남으면 그때 추가.
