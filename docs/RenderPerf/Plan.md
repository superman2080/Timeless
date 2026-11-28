# RenderPerf — Plan

근거: `docs/RenderPerf/Research.md`. 목표: 적 생성 시 GPU 렉 제거. CHAIN_CHOMP x3 기준 화면 삼각형 32M → 5M 이하.

## 구현 현황 요약 (2026-11-28)

- **완료**: Phase 1~6 전 항목.
  - FBX 11개 decimate
  - 텍스처 41개를 최대 1024로
  - URP 그림자 cascade 2 / 거리 25
  - 아이템 5종과 ChainChomp 사슬 17개 그림자 끔
- **미완료**: 없음
- **결과**:
  - CHAIN_CHOMP x3 화면 삼각형 32.0M → 2.56M
  - CHAIN_CHOMP x6 이동 시 최대 40.0M → 2.86M
  - 일반 플레이 최대 2.68M
  - 게임 텍스처 메모리 469.3MB → 277.4MB
- **계획과 달라진 점**:
  - ChainChomp 사슬은 `Enemy/ChainChomp.prefab`이 아니라 런타임 프리팹 `Obstacle3D/Gm_Obstacle_ChainChomp.prefab`에 있어서 거기에 적용함.
  - 프리팹을 재저장할 때 Unity가 현재 스크립트 필드로 직렬화를 정리함. 삭제된 `healSFX`/`hitSFX` 필드가 지워지고 `healSfx`, `hitSfx`, `knock*` 기본값이 기록됨. 이전에도 런타임이 같은 기본값을 쓰고 있었으므로 동작은 같음.
  - 오래된 에셋스토어 텍스처 meta 4개는 Unity가 최신 포맷으로 업그레이드하면서 필드가 추가됨.
- **남은 diff 주의**:
  - `Assets/Fonts/NotoSansKR-VF SDF.asset`는 작업 전부터 있던 변경이라 커밋 제외.
  - 측정 중 `runInBackground` 때문에 바뀐 `ProjectSettings.asset`은 원복함.

## 공통 원칙

- 원본 FBX는 git 추적 중 → 문제 생기면 `git checkout -- <fbx>`로 복구.
- Blender 작업은 사용자 열린 씬을 건드리지 않게 **작업용 새 씬**에서 하고 끝나면 삭제.
- 재출력 FBX는 **오브젝트/메시 이름, 머티리얼 슬롯 이름, 계층, (스킨) 본 이름과 클립 이름을 원본과 동일**하게 유지 → `.meta`와 프리팹 참조가 그대로 유지됨. `.meta`는 건드리지 않음.
- FBX 출력 옵션: 텍스처 임베드 끔 (외부 .mat이 `.fbm/` 텍스처 참조), 스케일/축은 원본 import와 같은 설정으로 출력하고 Unity bounds로 비교.
- 모델 하나 교체할 때마다 Unity 검증:
  - 컴파일/콘솔 에러 없음
  - 사용 프리팹의 메시/머티리얼 참조 Missing 없음
  - 메시 bounds가 원본 ±5% 이내
  - 임시 카메라 렌더 전/후 비교 이미지를 scratchpad에 저장해 외형 확인
  - 스킨 모델은 클립 재생까지 확인

## Phase 1 — 초고폴리 정적 메시 (효과 최대)

- [x] 교체 전 기준값 기록: 대상 FBX 전체의 메시 bounds, 삼각형 수, 비교 이미지
  - 기준 이미지와 통계는 세션 scratchpad에 저장(`PreviewRenderUtility` 렌더). 원본 FBX 계층/트랜스폼도 기록.
- [x] `monster_ball_3d_model.fbx` (CHAIN_CHOMP) 1,948,134 → **20,000** (Decimate Collapse, UV 보존)
- [x] `rusty_parking_bollard_3d_model.fbx` (P_SWITCH) 1,940,758 → **10,000**
- [x] Unity 재임포트 후 검증 (ChainChomp.prefab, P_SWITCH.prefab)
  - 확정 출력 설정: `apply_scale_options='FBX_SCALE_ALL'`(기본값이면 루트 스케일 100 + 메시 0.01배로 바뀜), `mesh_smooth_type='FACE'`, 텍스처 임베드 끔, `path_mode='STRIP'`, custom split normal 제거 후 smooth.
  - 결과: 루트 r(270,0,0) s(1), 메시 이름, 외부 머티리얼 리맵, bounds가 원본과 동일. FBX `.meta` 변경 없음.
  - monster_ball은 금 간 부분 디테일이 조금 뭉개짐(노멀맵이 고폴리 기준으로 구워져 있음). 게임 화면 크기에서는 허용 범위.

## Phase 2 — 정적 ~90k 메시

- [x] `creepy_spherical_monster_3d_model.fbx` (BulletBill, 프리팹 내 3개) 93,541 → 14,999
- [x] `Super_Mushroom.fbx` (HP, SUPER_MUSHROOM) 89,253 → 9,997
- [x] `SuperStar.fbx` 87,656 → 10,000
- [x] `Mini_Mushroom.fbx` 84,514 → 9,999
- [x] Unity 검증: 계층/스케일/bounds/머티리얼 일치, 전/후 렌더 비교 이상 없음
  - 주의: 이 모델들은 Blender에서 쿼드로 열리므로 decimate 비율을 폴리곤 수가 아닌 **삼각형 수** 기준으로 계산해야 함.
  - 주의: Blender에 이전 import의 머티리얼이 남아 있으면 `.001`이 붙어 Unity 외부 머티리얼 리맵이 깨짐. 매 작업 전 고아 데이터를 정리하고, 원본은 git HEAD에서 꺼내 import.

## Phase 3 — 스킨 메시 (위험도 높음)

Decimate는 버텍스 그룹(웨이트)을 유지함. FBX 재출력 시 애니메이션 클립/본 축이 바뀔 위험이 있어 모델마다 클립 재생을 확인하고, 깨지면 해당 모델만 git 복구 후 이 문서에 기록.
- [x] `Goomba.fbx` (BARREL) 95,158 → 15,000
- [x] `Lakitu.fbx` 95,004 → 14,999 (클립 2개)
- [x] `Whomp.fbx` 88,780 → 14,999
- [x] `Boo_Character_Idle.fbx` 34,968 → 12,000
- [x] `Octoomba.fbx` 34,654 → 12,000
- [x] Unity 검증: 계층·본·트랜스폼 diff 동일, take 이름과 구간 동일, 클립 길이/루프 동일, 클립 25%/60% 지점 본 월드 위치가 원본과 일치(소수 셋째 자리), 전/후 렌더 비교 이상 없음
  - 스킨 출력 설정:
    - import는 `anim_offset=0` (기본 1이면 키가 한 프레임 밀려 meta의 firstFrame/lastFrame과 어긋남)
    - Decimate를 Armature보다 앞으로 옮긴 뒤 적용 (웨이트 유지, 웨이트 없는 버텍스 0개)
    - `bake_anim_force_startend_keying`, `simplify 0`
  - take 이름: 단일 take(Goomba/Whomp/Boo/Octoomba = `Scene`)는 전역 take가 씬 이름을 쓰므로 작업 씬 이름을 `Scene`으로 두고 출력. 사용자 Blender 씬은 이름만 잠시 바꿨다가 복구.
  - Lakitu는 `bake_anim_use_all_actions=True`로 출력하고, 액션 이름을 `Idle`/`Attack`으로 바꿔 take가 `ZombieBee_Rig|Idle`, `ZombieBee_Rig|Attack`이 되게 함.
  - 스케일 옵션: Goomba/Lakitu/Whomp는 `FBX_SCALE_ALL`, Boo/Octoomba는 원본처럼 메시/리그 스케일 100이 되도록 `FBX_SCALE_NONE`.
  - 문제 및 해결: Blender import가 Boo의 본 3개를 connected로 만들어 Body 위치 키가 무시되고 bake 시 사라졌음. 출력 전에 모든 본의 `use_connect`를 해제해 해결. Lakitu도 9개 해제(본 위치 동일 확인).
  - [x] 플레이 중 Animator 재생 확인 (Phase 6): BARREL/LAKITU/WHOMP/BOO/OCTOOMBA 5종 모두 새 메시(15k/15k/15k/12k/12k)로 normalizedTime이 증가하며 재생됨.

## Phase 4 — 텍스처 축소 (`TextureImporter.maxTextureSize`, 원본 파일 유지)

대상: GameScene + Title + ObjectData.asset 의존성 중 실효 크기가 1024 초과인 텍스처 41개 (스프라이트/GUI 10개 제외, PC 플랫폼 override 없음).
- [x] 적/아이템/보스 텍스처 (Tripo `.fbm/`, `05. Textures/Goomba|Lakitu|Boo|Octoomba`) 2048 → **1024**
  - 보스(turtle_monster), creepy_mario_figure 포함
- [x] 배경 `05. Textures/ModularCorridor_Textures/*` 등 1024는 **유지**, 2048 이상인 배경 텍스처(`prop_wallPanel_A01/A02_dff/nrm`)만 1024
- [x] VFX `UnityTechnologies/ParticlePack` 2048 텍스처 4개 → 1024
- [x] 제외: UI 스프라이트(`10. Sprites/*`), 폰트 아틀라스, Hovl(이미 1024)
- [x] 적용 후 텍스처 메모리 재측정: 469.3MB → **277.4MB**. 1024 초과 대상 0개.

## Phase 5 — 그림자

- [x] `Assets/Settings/PC_RPAsset.asset`: `m_ShadowCascadeCount` 4 → **2**, `m_ShadowDistance` 50 → **25** (cascade2Split 0.25)
- [x] ChainChomp 사슬 `Capsule` 16개(`Link_00~15`) + `Cylinder`(`Post`): `shadowCastingMode` Off
  - 런타임 프리팹 `Obstacle3D/Gm_Obstacle_ChainChomp.prefab`에 적용. `Enemy/ChainChomp.prefab`에는 사슬이 없어 변경 없음.
- [x] 작은 아이템 프리팹 렌더러 `shadowCastingMode` Off: HP(Sphere + 버섯), SUPER_MUSHROOM, MINI_MUSHROOM, SUPER_STAR, P_SWITCH
- [x] 플레이 화면 캡처로 확인: 렌더 정상 (적/아이템/배경 표시, 깨짐 없음). 캡처 파일은 확인 후 삭제.
  - 씬이 어두워 캡처만으로는 그림자 품질 차이를 판단하기 어려움 → 근거리 그림자 품질은 **직접 플레이로 확인 필요**.

## Phase 6 — 최종 측정

- [x] 통제 실험 재실행 (생성 중지 후 해당 타입만 배치, 1920x1080):

| 상황 | 화면 삼각형 전 → 후 | Shadow Casters 전 → 후 |
|---|---|---|
| 적 없음 | 2.56M → 2.47M | 547 → 451 |
| HP x3 | 3.7M → 2.30M | 540 → 421 |
| CHAIN_CHOMP x1 | 12.3M → 2.50M | 599 → 451 |
| CHAIN_CHOMP x3 | 32.0M → **2.56M** | 709 → 427 |
| P_SWITCH x3 | 31.6M → **2.53M** | 556 → 451 |
| CHAIN_CHOMP x6 (이동, 최대) | 40.0M → **2.86M** | 726 → 475 |

  - 에디터 Main Thread는 전후 모두 중앙값 5.5~6.5ms. 이 PC는 GPU가 강해 전에도 CPU 기준 차이는 작았고, 줄어든 것은 GPU 작업량(삼각형/그림자)임.
- [x] 30초 일반 플레이: Main Thread 중앙값 6.5ms, p95 11.7, p99 13.7, 최대 19.0ms (16.7ms 초과 1프레임). 화면 삼각형 최대 2.68M (이전 측정 최대 40M).
  - Unity 창 포커스를 유지하지 못해 에디터가 스로틀됨 → 30초에 589프레임. 프레임당 비용 수치는 유효.
- [x] 콘솔: 새 에러 0. 기존 에러 6개는 이전 측정 세션의 프로파일 로드/eval 타임아웃 기록. 경고는 기존 ParticlePack/Missing Script.
- [x] git diff 점검:
  - `PC_Renderer.asset`와 글리치 머티리얼 diff 없음
  - 폰트 에셋은 작업 전부터 있던 변경
  - 측정용 `runInBackground`가 `ProjectSettings.asset`에 저장되어 원복함
- [x] 이 문서 상단에 구현 현황 요약

## 결정 사항 (사용자 피드백 2026-11-28)

1. 목표 삼각형 수: 제안대로 (CHAIN_CHOMP 20k, P_SWITCH 10k, 적 15k, 아이템 10k, Boo/Octoomba 12k).
2. Phase 3 스킨 모델: 진행. 깨지면 해당 모델만 git 복구 후 기록.
3. 텍스처: 최대 1024로 통일 (배경 1024 유지, 512 축소 안 함).
4. 그림자: cascade 2, 거리 25.
