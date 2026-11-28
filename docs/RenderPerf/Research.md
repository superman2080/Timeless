# RenderPerf — Research

적 생성 시 렌더링 렉(GPU 대기) 원인 조사. 2026-11-28 에디터 플레이 측정 + 에셋 분석 결과. 모두 실제 측정/파일 확인값.

## 1. 측정 결과 요약

- CPU 생성 비용은 무시 가능: `InteractionObjectPool.Get` 재사용 0.02~0.26ms, 풀 부족 시 Instantiate 포함 최대 0.49ms (CHAIN_CHOMP). GC·`CreateGPUProgram` 0건.
- 스파이크 원인은 `GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU` (10~39ms) → GPU 바운드.
- 생성 중지 후 타입별 배치 실험 (1920x1080, PC 품질):

| 상황 | 화면 삼각형 | Main Thread 중앙/최대 |
|---|---|---|
| 적 없음 | 2.56M | 5.3 / 13ms |
| CHAIN_CHOMP x1 | 12.3M | 5.5 / 10.7ms |
| CHAIN_CHOMP x3 | 32.0M | 8.6 / 17.7ms |
| P_SWITCH x3 | 31.6M | 8.6 / 17.6ms |
| CHAIN_CHOMP x6 (이동) | 최대 40.0M | 6.5 / 19.5ms |

- 메시 1개가 메인 패스 + 그림자 캐스케이드 4개 = 최대 5회 그려짐 → 1.95M 메시 하나가 화면 삼각형 ~10M 추가.

## 2. 고폴리 모델 (게임에서 실제 사용 중인 것)

`AssetDatabase.FindAssets("t:Model")` 99개 중 삼각형 순, 프리팹/씬 GUID 참조 확인.

| 모델 | tris | verts | 스킨 | 애니 클립 | 사용처 |
|---|---|---|---|---|---|
| `Assets/TripoModels/monster_ball_3d_model/monster_ball_3d_model.fbx` (58.9MB) | 1,948,134 | 1,428,941 | X | 0 | `07. Prefabs/Enemy/ChainChomp.prefab` (CHAIN_CHOMP) |
| `Assets/TripoModels/rusty_parking_bollard_3d_model/rusty_parking_bollard_3d_model.fbx` (61.6MB) | 1,940,758 | 1,718,097 | X | 0 | `07. Prefabs/Prefab_Item/P_SWITCH.prefab` |
| `Assets/03. Models/Skeletal/Goomba.fbx` | 95,158 | 58,073 | O | 1 | `07. Prefabs/Enemy/Goomba.prefab` (BARREL) |
| `Assets/03. Models/Skeletal/Lakitu.fbx` | 95,004 | 63,750 | O | 2 | `07. Prefabs/Enemy/Lakitu.prefab` (메시명 `ZombieBee`) |
| `Assets/TripoModels/creepy_spherical_monster_3d_model/creepy_spherical_monster_3d_model.fbx` | 93,541 | 62,712 | X | 0 | `07. Prefabs/Enemy/BulletBill.prefab` (프리팹 안에 3개 → 280k) |
| `Assets/03. Models/Static/Super_Mushroom.fbx` | 89,253 | 52,126 | X | 0 | `HP.prefab`, `SUPER_MUSHROOM.prefab` |
| `Assets/03. Models/Skeletal/Whomp.fbx` | 88,780 | 63,324 | O | 1 | `07. Prefabs/Enemy/Whomp.prefab` |
| `Assets/03. Models/Static/SuperStar.fbx` | 87,656 | 55,324 | X | 0 | `SUPER_STAR.prefab` |
| `Assets/03. Models/Static/Mini_Mushroom.fbx` | 84,514 | 56,018 | X | 0 | `MINI_MUSHROOM.prefab` |
| `Assets/03. Models/Skeletal/Boo_Character_Idle.fbx` | 34,968 | 22,328 | O | 1 | `07. Prefabs/Enemy/Boo.prefab` |
| `Assets/03. Models/Skeletal/Octoomba.fbx` | 34,654 | 20,125 | O | 1 | `07. Prefabs/Enemy/Octoomba.prefab` |

- 사용처 없음(대상 제외): `mushroom_creature`, `creepy_bee_character`, `stone_cube_monster`, `StoneMonster.fbx`, `prop_battleBus.fbx`, ParticlePack/Hovl 데모 모델.
- `env_stealth_static.fbx`(581k / 메시 551개)는 모듈 키트라 Track/Background 프리팹이 일부 서브메시만 씀 → 이번 대상 제외.
- ChainChomp 프리팹은 위 메시 외에 사슬 `Capsule` 16개(13k tris), `Cylinder` 1개도 그림자를 드리움.
- BossBowser(`bowser_3d_model_Humanoid.fbx`, 20k)는 적정 수준.

## 3. FBX 참조 구조 (재출력 시 위험 요소)

- 모든 대상 `.fbx.meta`가 `serializedVersion: 24600`, `internalIDToNameTable: []` → 서브에셋 fileID가 **이름 해시**로 결정. 예: ChainChomp 프리팹이 `fileID: -8679921383154817045, guid: e8db2799...`로 메시를 참조.
  - Blender에서 다시 내보낼 때 **오브젝트/메시 이름(`tripo_node_1dacfdb1` 등)과 계층 구조가 바뀌면 프리팹 메시 참조가 깨짐**.
- 머티리얼은 `externalObjects`로 **머티리얼 슬롯 이름 → 외부 .mat** 리맵 (예: `tripo_mat_1dacfdb1 → Materials/monster_ball_3d_model.mat`). 슬롯 이름 유지 필요. 텍스처는 외부 .mat이 `.fbm/` 폴더 텍스처를 참조하므로 FBX에 텍스처를 임베드할 필요 없음.
- 스킨 모델은 `animationType: Generic`, 클립이 FBX 안에 있음 → 재출력 시 본 이름/축/클립 이름 유지 필요. Animator Controller가 FBX 클립 서브에셋을 참조하므로 클립 이름이 바뀌면 깨짐.
- 임포트: `globalScale: 1`, `useFileScale: 1`, `meshCompression: 0`, `isReadable: 0`.
- 모든 FBX는 git으로 추적되고 LFS 아님 → 원본은 `git checkout`으로 복구 가능.

## 4. 텍스처

GameScene + ObjectData.asset 의존성 기준 텍스처 258개, 런타임 메모리 합계 469.3MB (에디터 측정).
- 적/아이템(Tripo `.fbm/`, `05. Textures/Goomba|Lakitu|Boo|Octoomba`)은 거의 전부 2048², 압축 O, mip O. 2048² RGBA/노멀은 개당 10.7MB, RGB는 5.4MB.
  - 메탈릭/스무스니스, 노멀, 베이스컬러, 이미시브 각각 2048².
- 배경 `05. Textures/ModularCorridor_Textures/*` 1024² (max 2048).
- VFX: `UnityTechnologies/ParticlePack/.../Explosion.tif`, `SmokePuff02.png`, `SmokeLoop02.tif`, `ExplosionEmission.tif` 2048². Hovl 텍스처 1024².
- UI: `10. Sprites/hpBar.png` 1280x501 Sprite.
- 크기 축소는 `TextureImporter.maxTextureSize` (meta만 변경, 원본 파일 유지)로 가능.

## 5. 그림자 / URP 설정

- 품질 레벨: Mobile(`Mobile_RPAsset`), PC(`PC_RPAsset`). 에디터는 PC 사용.
- `Assets/Settings/PC_RPAsset.asset`: `m_ShadowCascadeCount: 4`, `m_ShadowDistance: 50`, `m_MainLightShadowmapResolution: 2048`, soft shadow 품질 3.
- `Mobile_RPAsset`: cascade 1, 거리 50, 1024.
- 씬 라이트: Directional 1개(Soft shadow) + `CorridorBakeLights` 포인트 14개(그림자 없음).
- 측정 시 Shadow Casters Count 547 (적 없음) → 709 (CHAIN_CHOMP x3).

## 6. Blender MCP

- Blender 5.3.0, addon 프로토콜 16 (최신). `execute_blender_code`로 bpy 실행 가능.
- 사용자의 열린 Blender 씬을 건드리지 않도록 작업용 새 씬에서 import/decimate/export 후 정리 필요.

## 7. 영향 범위

- 메시 교체: 위 FBX + 해당 프리팹(참조 유지 시 프리팹 수정 없음). 대상 프리팹 12개 모두 `MeshCollider` 0개 확인 → 메시 감소가 충돌 판정에 영향 없음.
- 그림자 설정: 렌더러 에셋 1개 → 전체 씬 그림자 품질에 영향.
- 텍스처 maxSize: 해당 모델 외형 선명도에 영향.
