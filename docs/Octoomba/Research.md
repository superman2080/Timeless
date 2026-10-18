# Octoomba (점박문어) — Research

코드/씬에서 확인한 사실만 정리.

## 1. 모델 (씬 `SampleScene`의 `/Octoomba`)

- 루트 `Octoomba` (pos (-0.98, 0, -7.53)) : `Transform`, `Animator`(`Assets/04. Animations/Animator/Octoomba.controller`)
  - `Alien_Mesh` : `SkinnedMeshRenderer`, 머티리얼 `Assets/06. Materials/Enemy/M_Octoomba.mat`, rot (270,0,0) scl 100
  - `Alien_Rig` / `Root` / `Body`, `Leg.L`, `Leg.R`
- **FBX 프리팹 인스턴스가 아님**(언팩 상태, `GetCorrespondingObjectFromSource` = null). FBX 원본은 `Assets/03. Models/Skeletal/Octoomba.fbx`(미커밋).
- 메시 월드 바운즈: center y 0.50, extents (0.44, 0.50, 0.33) → 피벗이 바닥, y=0에 두면 바닥 위에 선다(Boo처럼 띄울 필요 없음).
- 애니메이터: 파라미터 없음, 상태 1개(`Octoomba`, 기본). 스크립트에서 제어할 것 없음.
- SampleScene은 현재 **dirty**(저장 안 된 사용자 변경 존재) 상태.

## 2. 기존 적 구조 (Boo 기준)

- 모델 프리팹 `Assets/07. Prefabs/Enemy/Boo.prefab` + 래퍼 프리팹 `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Boo.prefab`
  - 래퍼 루트: `Rigidbody`(useGravity 0), `BoxCollider`(isTrigger), `Boo` 스크립트(`objectData.type = 12`, `damageAmount`, `hitSFX` 등), 자식으로 모델 프리팹 중첩.
- `Boo : Obstacle` — 이동/피격은 `Obstacle` 재사용.

## 3. Obstacle / 피격 흐름 (`Assets/02. Scripts/Obstacles/Obstacle.cs`)

- `Start`에서 `onTargetHitEvent += OnCollded` (private).
- `OnCollded`: 상대가 `Player`이고, 하단이면 `ignoreJumpedPlayer || !player.IsJump`, 상단이면 `player.IsJump`일 때만 판정. 무적이면 무시.
  → `CameraManager.FadeGlitch` + `CameraShake` + `PlayClipAtPoint(hitSFX)` + `player.TakeHit(damage)`.
- **서브클래스가 "실제로 맞았을 때"를 알 수 있는 훅이 없다.** `OnCollded`가 private이고 판정 조건이 그 안에 있다.

## 4. 풀/생성 시점 (`Pool/InteractionObjectPool.cs`, `Map/Lane.cs`)

- `Lane.GenerateObstacle(type, pos)` → `InteractionObjectPool.Get(type, pos, rot)` → `Take()` 후 위치 세팅 → `SetActive(true)`.
- `Take()`에서 재사용할 비활성 오브젝트가 없으면 `Instantiate`(프리팹이 활성 상태라 **이 순간 `OnEnable`이 (0,0,0)에서 한 번 실행됨**) 후 즉시 `SetActive(false)`.
  → **`OnEnable`에서 바로 "생성 효과"를 띄우면 첫 생성 시 두 번 실행된다.** `OnEnable`에서 코루틴으로 한 프레임 미루면 즉시 비활성화될 때 코루틴이 같이 중단되어 실제 활성화 때만 실행된다.
- 반환 = `SetActive(false)`. 2D/3D 전환 시 레인 오브젝트 전부 회수(`Lane.RemoveAllObjects`).

## 5. UI (`/Canvas`)

- `Canvas` : Screen Space - Overlay, sortingOrder 0, `CanvasScaler`, `GraphicRaycaster`. 루트 pos (960,540) → 기준 해상도 1920×1080.
- 자식: `HPBar`(Slider + `HPBar` 스크립트)만 있음.
- 화면 효과(글리치/픽셀화)는 `CameraManager`가 Renderer Feature 에셋을 직접 수정하는 방식 — 이번 효과는 UI로 하므로 무관.

## 6. 원형 스프라이트

- 프로젝트에 범용 원형 UI 스프라이트 없음(`*circle*` 검색 결과 없음).
- Unity 내장 `UI/Skin/Knob.psd`가 원형이지만 해상도가 작아 화면 크기로 키우면 가장자리가 흐리다.

## 7. 데이터 등록 지점 (새 장애물 추가 절차)

- `ObjectType` enum (`InteractionObject.cs`) — 마지막 값 `BOO`(12). 새 값은 13.
- `Assets/11. Datas/ObjectData.asset`에 type → 래퍼 프리팹 등록.
- `PatternEditorWindow.GetColor`에 색 추가.
- 패턴 에셋 `Assets/11. Datas/Patterns/3D/Pattern3D_NN.asset`(현재 마지막 22) + `CreateObjectData3D.asset` 확률(합 100) 등록.

## 8. 위험 요소

- `Singleton<T>`는 기본 `dontDestroyOnLoad = true` — Canvas 자식 컴포넌트에 쓰면 Canvas 루트 전체가 DDOL 되고, 사망 시 씬 재로드로 Canvas가 중복된다. 화면 효과 컴포넌트는 Singleton을 쓰지 않거나 `dontDestroyOnLoad = false`로 둬야 한다.
- Overlay Canvas에 전체 화면 Image를 올리면 `raycastTarget` 기본 true → 이 씬엔 클릭 UI가 없어 실해는 없지만 꺼두는 게 맞다.
- 씬이 dirty 상태라 씬 저장 시 사용자의 미저장 변경도 함께 저장된다.
