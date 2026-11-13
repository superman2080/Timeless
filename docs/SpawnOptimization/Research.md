# SpawnOptimization — Research

증상: 적(장애물) 생성 시 프레임 드랍. 생성 경로를 코드에서 추적한 결과.

## 생성 흐름

```
LaneManager.GenerateObjectCoroutine (거리 기준 루프)
 └ GeneratePatternCoroutine → GenerateRow → Lane.GenerateObstacle(type, pos)
     └ InteractionObjectPool.Get(type, pos, rot)
         └ Take(type)
             ├ GetChildList(true)  ← Pool<T>.GetChildList
             │    = transform.GetComponentsInChildren<T>(true).ToList()
             └ 비활성 같은 타입 없으면 Instantiate(prefab) 후 SetActive(false)
     └ Lane.spawnedObjects.Add(obj)
```

## 확인된 문제 (코드 근거)

### 1. 생성마다 풀 전체 스캔 + GC 할당 — `Pool/Pool.cs:26`, `Pool/InteractionObjectPool.cs:85`
- `GetChildList`가 매번 `GetComponentsInChildren<T>(true)`(배열 할당) + `.ToList()`(리스트 할당).
- 풀 자식 수 N(전 타입 합계, 런 동안 계속 증가)에 비례해 O(N) 계층 탐색 + 할당이 **오브젝트 1개당 1번**. 한 행에서 최대 6개(하단 3 + 상단 3) 생성 → 행마다 6번 전체 스캔.
- `GetComponentsInChildren`은 프리팹 내부 자식 계층까지 전부 순회하므로 N보다 실제 비용이 큼(Lakitu/ChainChomp처럼 본/체인 계층이 깊은 프리팹 포함).
- 같은 함수를 쓰는 곳:
  - `Pool.Get()` (`Pool.cs:12`), `EffectPool.Take` (`EffectPool.cs:81`) — 이펙트 생성마다 동일 비용
  - `Lakitu.GetLandingZ` (`Lakitu.cs:138`) — 공 던질 때마다 (`GetChildList(false)`)
  - `Whomp.ClearLane` (`Whomp.cs:143`) — 자폭 시

### 2. 타입별 첫 생성 시 런타임 `Instantiate` — `InteractionObjectPool.cs:92`, `EffectPool.cs:157`
- 프리워밍 없음. 각 타입이 처음 등장하는 순간 게임 도중 `Instantiate` 발생 → 프리팹 로드/Awake/Animator·Rigidbody·Collider 초기화 비용이 해당 프레임에 몰림.
- 동시에 필요한 개수가 풀 보유 수를 넘을 때마다(예: Whomp 파편 10개 + 이펙트 11개가 0.1초 안에) 다시 `Instantiate`.
- 처음 화면에 그려질 때의 셰이더 variant 컴파일/업로드도 첫 등장 프레임에 겹칠 수 있음(비활성 상태로 프리워밍하면 이것까지는 해결 안 됨).
- `ObjectData.asset`에 등록된 타입: 1~19 (`NONE` 제외 19종).

### 3. `Lane.spawnedObjects` 무한 증가 — `Map/Lane.cs:15,27,31`
- `GenerateObstacle`마다 `Add`만 하고 어디서도 제거/`Clear`하지 않음. `RemoveAllObjects`도 순회만 하고 비우지 않음.
- 풀 재사용 때문에 같은 오브젝트가 여러 번 중복으로 쌓임. 런 시간에 비례해 메모리 증가 + 2D/3D 전환(`Player.cs:295` → `RemoveAllLaneObjects`) 때 순회 비용 증가. 직접적인 생성 렉 원인은 아니지만 누수.

### 4. 코루틴 yield 객체 매번 할당 (경미)
- `LaneManager.WaitDistance`(`LaneManager.cs:112`): 거리 대기 중 **매 FixedUpdate마다** `new WaitForFixedUpdate()`. 상시 GC 유발 → 누적되면 GC 스파이크가 생성 타이밍과 겹쳐 보일 수 있음.
- `ChainChomp`, `Lakitu`, `Whomp`, `Player`의 루프 안 `new WaitForFixedUpdate()`도 같은 패턴(빈도 낮음).

## 영향 범위 / 위험
- `Pool<T>`는 `InteractionObjectPool`, `EffectPool` 둘이 상속. `GetChildList`의 외부 호출자는 `Lakitu`, `Whomp`.
- 풀은 "자식 중 비활성 오브젝트 = 반환된 것" 규약. 반환은 여러 곳에서 `gameObject.SetActive(false)`를 직접 호출(`InteractionObject.DisposeCoroutine`, `Obstacle.KnockMovement`, `Whomp`, `Lakitu`, `PooledEffect` 등) → **반환 경로에 훅을 거는 방식은 불가**, 생성된 인스턴스 목록을 들고 `activeSelf`로 판별하는 방식이어야 기존 규약 유지.
- 풀 GameObject는 `Singleton` DDOL. 씬(`SampleScene.unity`)에 `Pool`, `EffectPool` 오브젝트가 배치되어 있음.
- 인코딩: `InteractionObjectPool.cs`, `LaneManager.cs`의 한글 주석은 CP949. `Pool.cs`, `EffectPool.cs`, `Lane.cs`, `Lakitu.cs`, `Whomp.cs`는 UTF-8(또는 ASCII).
