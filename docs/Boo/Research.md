# Boo (부끄부끄) — Research

목표: 멀리 있을 때는 투명 + 아웃라인만, 일정 범위 안으로 들어오면 알파 1로 드러나는 적. 전환은 Lerp로 부드럽게. 아웃라인은 알파가 올라가면서 같이 사라진다.

아래는 모두 코드/에셋에서 실제로 확인한 내용이다.

## 1. 씬에 있는 Boo

`Assets/01. Scenes/SampleScene.unity`

- GameObject `Boo` (fileID `207418151980127711`), **프리팹 아님** (`m_PrefabInstance: 0`) — FBX를 씬에 드래그한 상태.
  - Transform: `localPosition (0, 0, -4.58)`, `localRotation (0,1,0,0)` = Y 180° (모델이 카메라를 보도록 뒤집혀 있음)
  - Animator (fileID `696708950158872422`), controller guid `fe7c8ea1de7c7254395e98803b43aa34` = `Assets/04. Animations/Animator/Boo.controller`
  - 자식: `Boo_Mesh`(fileID `7579828381295380014`), `Boo_Rig`(본 계층: Arm.L, Tongue.01/02 등)
- `Boo_Mesh`의 SkinnedMeshRenderer (씬 라인 5108~)
  - 머티리얼 슬롯 **1개**: `Boo_Material` (guid `f4a212c94e9acf345a91f3ad8384abd5`)
  - Mesh: `Boo_Character_Idle.fbx`(guid `eb2573b8e699d6a42b5192c9ad7e9c8a`) 내부 메시
- Collider / Rigidbody **없음** → `InteractionObject`로 쓰려면 추가 필요

관련 신규 에셋 (git untracked):
- `Assets/03. Models/Skeletal/Boo_Character_Idle.fbx`
- `Assets/03. Models/Skeletal/Materials/Boo_Material.mat`
- `Assets/05. Textures/Boo_BaseMap.jpg` (guid `226d761d85d5a2042811da26aa3fe95e`)
- `Assets/04. Animations/Animator/Boo.controller`, `Assets/04. Animations/Clip/Boo_Idle.anim`

## 2. Boo_Material 현재 상태

`Assets/03. Models/Skeletal/Materials/Boo_Material.mat`

- 셰이더: URP Lit (guid `933532a4fcc9baf4fa0491de14d08ed7`)
- `_BaseMap` = Boo_BaseMap.jpg
- `RenderType: Opaque`, `_Surface: 0`(Opaque), `_Blend: 0`, `_ZWrite: 1`, `_QueueOffset: 0`, `m_CustomRenderQueue: -1`
- `_BaseColor: (1,1,1,1)`

즉 지금은 **완전 불투명**. 알파를 Lerp하려면 Transparent 설정(`_Surface: 1`, `_SrcBlend/_DstBlend`, `_ZWrite: 0`, `RenderType: Transparent`, queue 3000, `_SURFACE_TYPE_TRANSPARENT` 키워드)이 필요하다.

## 3. 아웃라인 관련 기존 자산

`Assets/09. Shaders/`: `AnimeFilmScratch.shader`, `AnimeToonPosterize.shader`, `GlitchShader.shadergraph`, `ReflectiveDiffuseSpecTransp.shader`
→ **아웃라인 셰이더는 프로젝트에 없다. 새로 만들어야 한다.**

후처리 쪽은 URP Renderer Feature를 에셋 직접 수정으로 제어 중(`PixelateRendererFeature`, 글리치 FullScreenPass). 아웃라인을 Renderer Feature(화면 공간 외곽선)로 하면 Boo 하나 때문에 전역 패스가 추가되고 `PC_Renderer.asset`을 또 건드리게 된다 → 머티리얼 단위(inverted hull)가 영향 범위가 작다.

## 4. 적 오브젝트 구조 (Whomp 기준, 가장 최신 패턴)

프리팹이 **2단**으로 나뉘어 있다:

1. 모델 프리팹: `Assets/07. Prefabs/Enemy/Whomp.prefab`
   - Whomp.fbx(guid `fab61c363dfc4194ea054ff1c50d0bc4`)의 PrefabInstance + Animator(controller `Whomp.controller`)만 추가. 스크립트/콜라이더 없음.
2. 스폰 프리팹: `Assets/07. Prefabs/Obstacle3D/Gm_Obstacle_Whomp.prefab` (guid `b907f63588caa6f428130051bf2bb648`)
   - 루트: Transform + Rigidbody(`m_UseGravity: 0`, `m_IsKinematic: 0`) + BoxCollider(`m_IsTrigger: 1`) + `Whomp` 스크립트(`objectData.type: 10`)
   - 자식: 1번 모델 프리팹 인스턴스 (`model` 필드로 참조)

`ObjectData.asset`에 등록되는 것은 **스폰 프리팹(Gm_Obstacle_*)** 쪽이다.

## 5. 스폰 / 등록 경로

- `ObjectType` enum: `Assets/02. Scripts/Obstacles/InteractionObject.cs:7` — 현재 마지막 값 `STONE_FRAGMENT = 11`
- `Assets/11. Datas/ObjectData.asset`: `type: 0`~`11`까지 prefab 매핑. **enum 값(정수) 순서대로** 들어 있음 → 새 타입은 `type: 12`로 append.
- `InteractionObjectPool.Take()`: `prefabDictionary`에 타입 없으면 경고 후 `null`. 풀은 자식 중 `activeSelf == false` + 같은 `objectData.type`을 재사용. 즉 **OnEnable에서 상태 초기화 필수**(Whomp가 rotation/model 복구하는 이유).
- `Lane.GenerateObstacle(type, pos)` → 풀에서 Get → `currentLane`, `isUpper` 세팅 → `spawnedObjects`에 등록(2D 전환 시 `RemoveAllObjects`로 회수).
- `LaneManager.GenerateRow()`에 Lakitu/Whomp처럼 "동시 N마리 제한 / 2D에서는 생성 금지" 예외가 하드코딩되어 있다(`LaneManager.cs:135~147`).
- 패턴 편집 색: `Assets/02. Scripts/Editor/PatternEditorWindow.cs:246` `GetColor` switch — 새 타입은 여기에 색을 추가해야 흰색 기본값으로 안 빠진다.

## 6. 상호작용 / 데미지

- `InteractionObject`(추상, `[RequireComponent(typeof(Collider), typeof(Rigidbody))]`)
  - `OnEnable`에서 `DisposeCoroutine` 시작 → `z < GameManager.Instance.DisposePos.z`면 `SetActive(false)`
  - `OnTriggerEnter`에서 `ICollisionable` 상대에게 `onTargetHitEvent` 발생
- `Obstacle : InteractionObject` (`Obstacle.cs`)
  - `damageAmount`, `ignoreJumpedPlayer`, `hitSFX` 필드
  - `FixedUpdate` → `ObstacleMovement()` (protected virtual): `Rb.MovePosition(pos + back * mapSpeed * dt)`
  - `OnCollded`: `isUpper`면 점프 중에만, 하단이면 지상(또는 `ignoreJumpedPlayer`)일 때만 판정. 무적이면 무시. 글리치 + 셰이크 + SFX + `player.TakeHit`
- Whomp/Lakitu는 자체 피격 판정이 없어 `InteractionObject`를 직접 상속. **플레이어에게 데미지를 주는 적이면 `Obstacle`을 상속하는 쪽이 이동/판정/연출을 그대로 재사용한다.**

## 7. 플레이어 위치 참조 방법

- `GameManager.Instance.player` (`Whomp.cs:88`에서 사용), `player.currentLane`, `player.transform.position.z`, `player.IsJump`, `player.IsInvincible`
- 플레이어는 z 고정, 월드가 `-Z`로 흐른다 → **거리는 `transform.position.z - player.transform.position.z`(z 갭)로 재는 것이 자연스럽다.** Whomp의 `triggerDistance`도 같은 방식.

## 8. 위험 요소

- **머티리얼 공유**: `renderer.sharedMaterial`의 알파를 바꾸면 에셋이 수정되어 git diff가 남고 모든 Boo가 같이 바뀐다(CLAUDE.md의 렌더러 에셋 주의와 같은 종류의 함정). 런타임에는 `renderer.materials`(인스턴스)를 받아 캐싱해야 한다.
- **풀 재사용**: 투명도/아웃라인 값을 `OnEnable`에서 초기 상태(투명 + 아웃라인 1)로 되돌리지 않으면 재사용 시 이미 드러난 상태로 등장한다.
- **반투명 스킨드 메시 자기겹침**: 알파 블렌딩이면 몸 내부/뒷면이 겹쳐 보인다. 유령 캐릭터라 의도에 맞는 편이지만 혀/팔이 겹치는 구간은 눈에 띌 수 있다.
- **inverted hull 아웃라인 한계**: 하드 엣지/노멀이 갈라진 지점에서 외곽선이 끊길 수 있다. 메시가 부드럽다면 문제없음.
- **URP 멀티패스**: 한 머티리얼에 `LightMode=UniversalForward` 패스를 두 개 두면 하나만 그려진다. 아웃라인을 별 머티리얼 슬롯으로 분리하면 이 제약을 피한다(SkinnedMeshRenderer에서 서브메시보다 많은 머티리얼은 마지막 서브메시에 덧그려짐).
- SampleScene은 이미 수정 상태(`M`)이고, Boo가 씬에 맨몸으로 놓여 있다 → 프리팹화하면서 씬 인스턴스를 어떻게 할지 결정 필요.
