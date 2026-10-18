# Octoomba (점박문어) — Plan

> 상태: **구현 완료** (Phase 1~6 전부). 플레이 검증 통과, 콘솔 신규 에러 0.

## 구현 현황 요약

완료
- Phase 1 `Obstacle.OnHitPlayer` 훅
- Phase 2 `UI/OctoombaScreenEffect.cs`, `Assets/05. Textures/UI/T_InkBlob.png`
- Phase 3 `Obstacles/Octoomba.cs`
- Phase 4 `Enemy/Octoomba.prefab`, `Obstacle3D/Gm_Obstacle_Octoomba.prefab`(콜라이더 center (0, 0.5, 0.02) size (0.88, 1.01, 0.66)), 씬 `/Octoomba` 삭제, `Canvas/OctoombaScreenEffect`(첫 자식) 생성, 씬 저장
- Phase 5 `ObjectType.OCTOOMBA`(13), `ObjectData.asset`, Pattern Editor 색, `Pattern3D_23`(가운데 레인 1행) + `CreateObjectData3D` 등록(24개 각 ≈4.17%, 합 100)
- Phase 7 먹물 발동을 거리(`splashDistance` = 8) 진입 시 1회로 변경
- Phase 6 플레이 검증: 생성 시 먹물 6개 랜덤 배치 → 커브대로 투명 → 비활성 / 피격 시 데미지 10 + 글리치 + 1초 암전 / HP바는 가려지지 않음

미완료: 없음

계획과 달라진 점
1. **원 텍스처를 한 번 다시 생성했다.** `Mathf.SmoothStep(a, b, t)`는 GLSL smoothstep이 아니라 a~b 보간이라 처음 만든 텍스처의 알파가 거의 0이었다(화면에 안 보임). `InverseLerp` + 3t²−2t³로 다시 생성.
2. 먹물 생성 직후의 단일 실행(Instantiate 직후 가짜 `OnEnable` 무시)은 코드 구조(한 프레임 지연 코루틴이 즉시 비활성화로 취소)로 보장하며, 플레이에서 재현 구분은 하지 않았다.

참고
- 플레이 중 HP가 빠르게 줄어 2D 전환/사망 재로드가 잦았다(이번 작업과 무관한 기존 `Player.cs`/`GameManager.cs` 미커밋 변경 상태에서 테스트). 검증 중에만 리플렉션으로 `hpDecrement`를 0으로 바꿨고 에셋/씬에는 반영되지 않았다.
- 콘솔 에러 1건은 검증 도구(eval) 타임아웃 로그이며 게임 코드 에러가 아니다.

---

## 설계 요약

- **적 타입**: `Octoomba : Obstacle` — 이동/피격 판정/글리치·셰이크·SFX는 `Obstacle` 그대로. 하단 레인, 점프로 회피 가능.
- **생성 효과 (먹물)**: 실제로 활성화되면(풀 Instantiate 직후의 가짜 `OnEnable` 제외, Research 4번) 화면에 반투명 검정 원 여러 개를 랜덤 위치/크기로 뿌린다. 알파 = `inkAlphaCurve.Evaluate(경과시간)` — 커브의 마지막 키 시간이 지속시간이고, 끝나면 원 비활성화.
- **피격 효과 (암전)**: 이 적에게 실제로 맞았을 때 전체 화면 검정 Image의 알파 = `blackoutAlphaCurve.Evaluate(경과시간)`, 1초(커브 길이) 동안.
- **화면 효과 담당**: `Canvas` 아래 `OctoombaScreenEffect` 컴포넌트 하나가 먹물 원들 + 암전 Image를 들고 `SplashInk()` / `Blackout()` 제공. 알파 커브는 이 컴포넌트 인스펙터에서 편집.
  - 접근: `static Instance`를 `Awake`에서 할당(씬 재로드 시 새 Canvas 것으로 교체). `Singleton<T>`은 Canvas를 DDOL로 만들어 재로드 시 중복되므로 쓰지 않는다(Research 8번).
  - 먹물이 겹쳐 뿌려지면(연속 생성) 진행 중인 코루틴을 멈추고 새로 뿌린다. 암전도 동일.
- **원형 스프라이트**: 에디터 스크립트로 부드러운 가장자리의 원 PNG(256px)를 한 번 생성해 에셋으로 저장 → 먹물 Image에 사용.

### 커브 기본값 (인스펙터에서 조정)
- `inkAlphaCurve`: (0s, 0.8) → (0.5s, 0.8) → (4s, 0) — 뿌려진 직후 진하게 유지 후 서서히 투명.
- `blackoutAlphaCurve`: (0s, 1) → (0.6s, 1) → (1s, 0) — 1초 암전 후 풀림.

---

## Phase 1 — Obstacle 피격 훅

- [x] `Obstacle.cs`: `protected virtual void OnHitPlayer(Player player) { }` 추가, `OnCollded`에서 `player.TakeHit(...)` 직후 호출. (기존 동작 변화 없음)

## Phase 2 — 화면 효과 컴포넌트

- [x] `Assets/02. Scripts/UI/OctoombaScreenEffect.cs` 신규
  - 필드: `Image[] inkBlobs`, `Vector2 inkSizeRange`(px, 기본 300~600), `AnimationCurve inkAlphaCurve`, `Image blackoutImage`, `AnimationCurve blackoutAlphaCurve`
  - `SplashInk()`: 각 blob을 화면 안 랜덤 anchoredPosition/크기/회전으로 배치 후 코루틴으로 알파 커브 재생 → 끝나면 비활성
  - `Blackout()`: 코루틴으로 알파 커브 재생 → 끝나면 비활성
  - 공통 `PlayCurve(Image[] images, AnimationCurve curve)` 코루틴 하나로 둘 다 처리
- [x] 원 텍스처 생성 (eval_file로 1회 실행, 프로젝트에 스크립트는 남기지 않음) → `Assets/05. Textures/UI/T_InkBlob.png` (Sprite 임포트)

## Phase 3 — Octoomba 스크립트

- [x] `Assets/02. Scripts/Obstacles/Octoomba.cs` 신규 (`Octoomba : Obstacle`)
  - `OnEnable`: `base.OnEnable()` + 한 프레임 뒤 `OctoombaScreenEffect.Instance?.SplashInk()` (코루틴 — Instantiate 직후 즉시 비활성화되면 자동 취소)
  - `OnHitPlayer`: `OctoombaScreenEffect.Instance?.Blackout()`

## Phase 4 — 프리팹 / 씬

- [x] 씬 `/Octoomba` → `Assets/07. Prefabs/Enemy/Octoomba.prefab` (모델 프리팹)
- [x] 래퍼 `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Octoomba.prefab`: `Rigidbody`(gravity off) + `BoxCollider`(trigger, 메시 바운즈 기준) + `Octoomba`(damage 10, hitSFX는 Boo와 동일) + 자식 `Enemy/Octoomba.prefab`
- [x] SampleScene의 `/Octoomba` 인스턴스 삭제 (스폰은 풀/패턴으로만, Boo와 동일)
- [x] SampleScene `Canvas` 아래 `OctoombaScreenEffect` 오브젝트 생성
  - 자식 `Ink_0..5` (Image, T_InkBlob, 검정, raycastTarget off, 비활성)
  - 자식 `Blackout` (Image, 전체 화면 stretch, 검정, raycastTarget off, 비활성)
  - `OctoombaScreenEffect` 오브젝트를 Canvas의 **첫 번째 자식**(HPBar보다 앞)으로 둬서 먹물/암전 모두 HP바 **아래**에 그려지게 함 (HP바는 항상 보임)
- [x] 씬 저장 (⚠ 씬이 이미 dirty — 사용자의 미저장 변경도 같이 저장됨)

## Phase 5 — 데이터 등록

- [x] `ObjectType.OCTOOMBA` (= 13) 추가
- [x] `ObjectData.asset`에 OCTOOMBA → `Gm_Obstacle_Octoomba` 등록
- [x] `PatternEditorWindow.GetColor`에 색 추가 (보라 계열)
- [x] 테스트용 패턴 `Pattern3D_23.asset` (가운데 레인 1행에 OCTOOMBA) 생성 + `CreateObjectData3D` 등록, 확률 재정규화 (Boo 때와 동일 방식)

## Phase 6 — 검증

- [x] 컴파일/콘솔 에러 0 확인
- [x] 플레이: 생성 시 먹물 1회만 뿌려짐(첫 Instantiate 중복 없음), 커브대로 사라짐 / 피격 시 1초 암전 / 점프 회피 시 암전 없음 / 씬 재로드 후에도 동작

---

## Phase 7 — 먹물 발동을 거리 기준으로 변경 (구현 완료)

생성 직후가 아니라, 플레이어와의 z 거리가 `splashDistance` 이하가 되는 순간 **활성화당 1회** 먹물을 뿌린다. 거리 판정은 Boo와 같은 방식(플레이어 z 고정, `transform.position.z - player.z`).

- [x] `Octoomba.cs`
  - `[Min(0f)] float splashDistance` 필드 추가 (기본 8, 인스펙터에서 조정)
  - `bool splashed`를 `OnEnable`에서 false로 리셋
  - `Update`: `!splashed`이고 거리 이하면 `SplashInk()` 후 `splashed = true`
  - 한 프레임 지연 코루틴(`SplashNextFrame`) 제거 — 풀 Instantiate 직후 같은 프레임에 비활성화되므로 `Update`가 돌지 않아 중복 발동 문제가 없다
  - (선택) Boo처럼 선택 시 발동 거리를 씬 뷰 기즈모로 표시
    - 구현 메모: 지시가 없어 넣지 않음
- [x] 플레이 검증: 생성 시점엔 먹물 없음 → 거리 진입 시 1회 → 점프로 피해도 먹물은 이미 발동
  - 결과: z 갭 25 / 12.8에서 먹물 없음, 2.5에서 발동, 지나간 뒤에도 재발동 없음. 콘솔 에러 0

## 확정된 결정

1. **HP바는 가리지 않는다** — 먹물/암전 모두 HP바 아래 레이어.
2. **테스트 패턴 생성** — `Pattern3D_23` 생성 + `CreateObjectData3D` 등록.
3. **피격 연출 유지** — `Obstacle` 기본 글리치/셰이크/SFX 유지 + 암전 추가.
4. **먹물 원 개수/크기** — 별도 지시 없어 기본값(6개, 지름 300~600px) 사용. 인스펙터에서 조정 가능.
