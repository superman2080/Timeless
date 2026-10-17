# Boo (부끄부끄) — Plan

> 상태: **구현 완료** (Phase 1~5 전부). 플레이 검증까지 통과, 콘솔 신규 에러/경고 0.

## 구현 현황 요약

완료
- Phase 1 아웃라인 셰이더 `BooOutline.shader` + `M_Boo_Outline.mat` 생성, `Boo_Material` Transparent 전환
- Phase 2 `Boo.cs` (`Boo : Obstacle`)
- Phase 3 `Enemy/Boo.prefab`, `Obstacle3D/Gm_Obstacle_Boo.prefab`, SampleScene의 Boo 인스턴스 삭제
- Phase 4 `ObjectType.BOO`(값 12) + `ObjectData.asset` + Pattern Editor 색 + `Pattern3D_22` + `CreateObjectData3D` 등록
- Phase 5 플레이 검증 (원거리 아웃라인만 / 근거리 몸 드러남 / Lerp 수렴 / 풀 재사용 리셋 / 2D 회수 / 에셋 알파 비오염)

계획과 달라진 점
1. **몸 머티리얼의 깊이 쓰기를 런타임에 강제했다.** 아웃라인(확장된 뒷면)의 내부를 가리려면 몸이 알파 0이어도 깊이를 써야 하는데, URP 머티리얼 임포터가 Transparent 머티리얼의 `_ZWrite`를 0으로 되돌린다. 그래서 `Boo.Awake`에서 런타임 인스턴스에 `_ZWrite = 1`을 건다. (이 처리를 하기 전에는 Boo가 흰 실루엣 덩어리로 보였다.)
2. **아웃라인 셰이더 큐를 `Transparent+1`로 두었다.** 몸보다 뒤에 그려야 몸이 쓴 깊이로 내부가 걸러진다.
3. **`Enemy/Boo.prefab`은 씬 오브젝트가 아니라 FBX에서 새로 만들었다.** 씬의 Boo는 FBX 프리팹 인스턴스가 아니라 언팩된 상태여서, 그대로 프리팹화하면 Whomp처럼 FBX 중첩 구조가 나오지 않는다. 회전(Y 180°)/Animator/머티리얼은 씬 인스턴스와 동일하게 재현했다.
4. **모델 자식을 y = 0.6 띄웠다.** Boo 메시 피벗이 몸 중심이라 y=0이면 절반이 바닥에 박힌다. 콜라이더는 띄운 상태의 메시 바운즈로 다시 맞췄다(center (0, 0.59, 0.38), size (1.08, 0.92, 0.82)).
5. **`CreateObjectData3D`의 확률을 전체 재정규화했다.** Pattern3D_22를 추가하면서 23개 합이 100이 되도록 비례 축소.

참고
- 이번 작업과 무관하게 `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Pipe.prefab`에 `ignoreJumpedPlayer: 0 → 1` diff가 생겨 있다. 에디터에 저장되지 않은 채 남아 있던 변경이 `AssetDatabase.SaveAssets()`에서 함께 기록된 것으로 보인다. 의도한 변경이 아니면 되돌릴 것.

---

> 아래는 원래 계획. 각 항목에 구현 결과를 표시했다.

## 설계 요약

- **적 타입**: `Boo : Obstacle` — 이동(월드 스크롤), 피격 판정, 글리치/셰이크/SFX를 `Obstacle`에서 그대로 재사용. 하단 레인 장애물(점프로 회피 가능)로 둔다.
- **투명 → 등장**: 플레이어와의 z 갭이 `revealDistance` 이하로 들어오면 목표 알파 1, 멀면 0. 매 프레임 `Mathf.Lerp`로 현재 알파를 목표로 수렴(지수 감쇠: `Lerp(cur, target, 1 - exp(-lerpSpeed * dt))` — 프레임레이트에 안정적).
- **아웃라인**: `1 - bodyAlpha`. 즉 몸이 드러나는 만큼 아웃라인이 사라진다. 같은 Lerp 값 하나로 두 알파를 구동하므로 항상 동기화된다.
- **머티리얼 구성** (렌더러 슬롯 2개, 서브메시는 1개 → 2번 슬롯이 같은 메시에 덧그려짐)
  - 슬롯 0 = `Boo_Material` (URP Lit, **Transparent로 전환**) — `_BaseColor.a`를 Lerp
  - 슬롯 1 = `M_Boo_Outline` (신규 `BooOutline.shader`, inverted hull: Cull Front + 노멀 방향 외곽 확장, Unlit, 알파 블렌딩) — `_OutlineColor.a`를 Lerp
  - 아웃라인을 별 머티리얼로 분리하는 이유: URP는 한 머티리얼에서 같은 `LightMode` 패스를 하나만 그린다(Research 8번). 화면 공간 아웃라인(Renderer Feature)은 전역 패스 추가 + `PC_Renderer.asset` 수정이라 영향 범위가 커서 제외.
- **머티리얼 인스턴스**: 런타임에 `SkinnedMeshRenderer.materials`로 인스턴스를 받아 캐싱. 셰어드 에셋은 절대 쓰지 않는다(git diff / 전체 Boo 동기화 문제).
- **풀 재사용**: `OnEnable`에서 알파를 "완전 투명 + 아웃라인 1"로 리셋.

## 확정된 결정

1. **씬 Boo 인스턴스** — 프리팹으로 뽑은 뒤 SampleScene에서 **삭제**. 스폰은 풀/패턴으로만.
2. **데미지** — `damageAmount = 10`, `ignoreJumpedPlayer = false` (점프로 회피 가능).
3. **동시 등장 제한 없음** — 일반 장애물 취급. `LaneManager.GenerateRow`에 예외 추가하지 않는다.
4. **생성/회수는 풀 경유** — `Lane.GenerateObstacle` → `InteractionObjectPool`. Boo 자체 `Instantiate`/`Destroy` 없음.
5. **아웃라인 두께는 화면 고정 굵기** — 클립 공간에서 확장하고 `w`로 보정하므로 거리와 무관하게 픽셀 두께가 일정하다.
6. **등장 사거리는 `[Min(0f)]`** — 인스펙터에서 자유 입력, 음수만 차단 (슬라이더 상한 없음).

---

## Phase 1 — 아웃라인 셰이더 / 머티리얼

- [x] `Assets/09. Shaders/BooOutline.shader` 신규 작성
  - Tags: `RenderType=Transparent`, `Queue=Transparent`
  - 구현 메모: 실제로는 `Queue=Transparent+1`로 작성했다. 몸보다 나중에 그려야 몸이 쓴 깊이로 확장된 뒷면의 내부가 걸러진다.
  - 1 패스: `LightMode=UniversalForward`, `Cull Front`, `ZWrite Off`, `ZTest LEqual`, `Blend SrcAlpha OneMinusSrcAlpha`
  - 정점(**화면 고정 굵기**): 포지션을 클립 공간으로 변환하고, 노멀을 뷰 공간 → 클립 공간 방향으로 변환한 뒤 `positionCS.xy += normalCS.xy * _OutlineWidth * positionCS.w * (_ScreenParams.y / _ScreenParams.x 보정)` 으로 확장. `w` 곱으로 투영 나눗셈이 상쇄되어 거리와 무관하게 픽셀 두께가 일정해지고, 종횡비 보정으로 가로/세로 두께가 같아진다. (스킨드 메시는 스키닝된 포지션/노멀이 들어오므로 그대로 동작)
  - 프로퍼티: `_OutlineColor (Color, 기본 흰색)`, `_OutlineWidth (Range 0~0.05, 기본 0.005 — 클립 공간 단위 ≒ 화면 높이 비율)`
  - 프래그먼트: `_OutlineColor` 그대로 출력(Unlit). URP Core/Common.hlsl include, SRP Batcher 호환 CBUFFER(`UnityPerMaterial`) 작성
- [x] `Assets/06. Materials/M_Boo_Outline.mat` 생성 — 위 셰이더 사용, 색/두께 기본값 세팅
- [x] `Boo_Material`을 Transparent로 전환
  - `_Surface: 1`, `_Blend: 0`(Alpha), `_SrcBlend: 5 / _DstBlend: 10`, `_ZWrite: 0`, `RenderType: Transparent`, `m_CustomRenderQueue: 3000`, 키워드 `_SURFACE_TYPE_TRANSPARENT`
  - 에디터에서 Surface Type을 바꿔 Unity가 키워드/큐를 직접 세팅하도록 한다(손으로 YAML 수정 시 키워드 누락 위험)
  - `_BaseColor.a`를 0으로 저장해 기본 상태가 투명이 되게 함
  - 구현 메모: `BaseShaderGUI.SetupMaterialBlendMode` + `SetMaterialKeywords`로 전환했다(키워드 `_SURFACE_TYPE_TRANSPARENT`, 큐 3000). `_ZWrite`는 에셋에 1로 저장해도 임포터가 0으로 되돌리므로 런타임에서 강제한다(위 "계획과 달라진 점" 1번).

## Phase 2 — Boo 스크립트

- [x] `Assets/02. Scripts/Obstacles/Boo.cs` 신규 (`Boo : Obstacle`, UTF-8)
  - 필드
    - `[Min(0f)] [SerializeField] float revealDistance = 12f` — 이 z 갭 이하에서 드러남 (상한 없음, 음수만 차단)
    - `[Min(0f)] [SerializeField] float fadeDuration = 0.1f` — 알파가 목표에 거의 도달하는 시간(초). 내부적으로 감쇠율 `3 / fadeDuration`로 환산해 지수 Lerp. 0이면 즉시 전환
    - `[Range(0f, 1f)] [SerializeField] float hiddenAlpha = 0f` — 멀리 있을 때 몸 알파 (알파는 0~1 고정이라 Range 유지)
    - `[SerializeField] Renderer targetRenderer` — Boo_Mesh의 SkinnedMeshRenderer
    - `[SerializeField] int bodyMaterialIndex = 0`, `outlineMaterialIndex = 1`
  - `Awake`: `targetRenderer.materials`로 인스턴스 배열 받아 `bodyMat` / `outlineMat` 캐싱 (`Shader.PropertyToID("_BaseColor")`, `"_OutlineColor"` 캐싱)
  - `OnEnable`: `base.OnEnable()` + `currentAlpha = hiddenAlpha` 후 즉시 적용(리셋)
  - `Update`: 목표 알파 계산(`InRange() ? 1 : hiddenAlpha`) → `currentAlpha = Mathf.Lerp(currentAlpha, target, 1f - Mathf.Exp(-(3f / fadeDuration) * Time.deltaTime))` → `ApplyAlpha()`
  - `InRange()`: `GameManager.Instance.player`의 z와 자기 z 차이. 플레이어 뒤로 지나간 경우(갭 < 0)도 드러난 상태 유지
  - `ApplyAlpha()`: body `_BaseColor.a = currentAlpha`, outline `_OutlineColor.a = 1 - currentAlpha`
  - `OnDestroy`: 생성한 머티리얼 인스턴스 `Destroy`(에디터 누수 방지)
  - 씬 뷰 기즈모: `revealDistance` 구간 표시(Whomp의 `OnDrawGizmosSelected` 방식 재사용, 토글 필드 포함)
- [x] 컴파일 확인: `unity command console_status --project-path .`

## Phase 3 — 프리팹

- [x] 아웃라인 머티리얼 슬롯 추가 (`Boo_Mesh` 렌더러 슬롯 1 = `M_Boo_Outline`)
  - 구현 메모: 씬 오브젝트가 아니라 새로 만든 `Enemy/Boo.prefab`의 렌더러에 `[Boo_Material, M_Boo_Outline]`로 설정했다.
- [x] `Assets/07. Prefabs/Enemy/Boo.prefab` 생성 — FBX 인스턴스 + Animator(Boo.controller) + 머티리얼 2슬롯 (Whomp.prefab과 동일 구조)
  - 구현 메모: 씬의 Boo는 언팩 상태라 그대로 프리팹화하면 FBX 중첩 구조가 안 나와, FBX에서 새로 구성했다(위 "계획과 달라진 점" 3번).
- [x] `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Boo.prefab` 생성
  - 루트: `Rigidbody`(useGravity 0, isKinematic 0), `BoxCollider`(isTrigger 1, 메시 bounds에 맞춰 크기/센터), `Boo` 스크립트
  - 자식: `Enemy/Boo.prefab` 인스턴스 (localPosition y = 0.6, Y 180°) — 피벗이 몸 중심이라 바닥에 박히지 않도록 띄움
  - `Boo.targetRenderer` = 자식 `Boo_Mesh`의 SkinnedMeshRenderer 연결
  - `objectData.type = BOO`, `damageAmount`/`ignoreJumpedPlayer`/`hitSFX` 세팅
- [x] SampleScene에서 Boo 임시 인스턴스 삭제

## Phase 4 — 타입 등록 / 스폰 연결

- [x] `InteractionObject.cs`의 `ObjectType`에 `BOO,` 추가 (값 12, 마지막에 append — 기존 값 변경 금지)
- [x] `Assets/11. Datas/ObjectData.asset`에 `type: 12` → `Gm_Obstacle_Boo.prefab` 등록 (에디터/MCP로 등록해 fileID+guid가 맞게 들어가도록)
- [x] `PatternEditorWindow.GetColor`에 `ObjectType.BOO` 색 추가 (예: `new Color(0.85f, 0.85f, 0.95f)` — 유령 느낌의 흰빛)
- [x] 테스트용 3D 패턴에 Boo 배치
  - 구현 메모: 신규 `Pattern3D_22.asset`(1행, 가운데 하단 레인에 BOO) 생성 후 `CreateObjectData3D.asset`에 등록. 23개 패턴 확률을 합 100으로 재정규화(각 ≈ 4.35%).

## Phase 5 — 검증 (MCP)

- [x] `console_status`로 컴파일 에러 0 확인
- [x] 플레이 실행(`runInBackground` 켠 상태) — 멀리서 아웃라인만 보이는지, 접근 시 부드럽게 몸이 드러나는지, 아웃라인이 같이 사라지는지 게임 뷰 캡처로 확인 (캡처 파일은 확인 후 `.meta`까지 삭제)
- [x] 풀 재사용 확인: 한 Boo가 뒤로 사라진 뒤 다음 Boo가 **투명 상태로** 다시 등장하는지
- [x] 2D 전환 시 레인 회수에 Boo가 포함되는지(`Lane.RemoveAllObjects`) 확인
- [x] `Boo_Material` / `M_Boo_Outline` 에셋에 런타임 알파가 저장되지 않았는지 git diff로 확인
- [x] 콘솔 경고 확인 — 기존 경고(배경 프리팹 Missing Script, `Ray does not intersect with y=0 plane`) 외 신규 경고 없는지
