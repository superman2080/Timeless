# Addressables — Research

코드/에디터에서 확인한 사실만 정리.

## 1. 현재 상태

- `com.unity.addressables` **미설치** (`Packages/manifest.json`에 없음). 레지스트리 최신 버전 `4.1.1`.
- `Assets/AddressableAssetsData` 없음. 코드에 `Addressables` / `AssetReference` / `Resources.Load` 사용 없음.
- `Resources` 폴더는 `Assets/UnityTechnologies/ParticlePack/TextMesh Pro/Resources/`(샘플 팩)뿐 — 게임 코드에서 사용 안 함.

## 2. 빌드 씬 / 씬 로딩

- Build Settings: `Assets/01. Scenes/Title.unity` → `Assets/01. Scenes/SampleScene.unity` (둘 다 enabled).
- `StartButton.LoadSceneAsyncCoroutine` → `SceneManager.LoadSceneAsync(sceneToLoad)` (Title에서 SampleScene으로, 이름 문자열).
- `Player.Restart` → `SceneManager.LoadScene("SampleScene")` (사망 시 재로드).

## 3. "현재 게임에서 사용되는 파일" = 빌드 씬 2개의 재귀 의존성

`AssetDatabase.GetDependencies([Title, SampleScene], recursive)` 결과 **508개, 약 196MB**.

| 폴더 | 개수 | | 폴더 | 개수 |
|---|---|---|---|---|
| 01. Scenes | 2 | | 07. Prefabs | 27 |
| 02. Scripts | 34 (.cs, 번들 대상 아님) | | 08. Audio | 8 |
| 03. Models | 108 | | 09. Shaders | 8 |
| 04. Animations | 10 | | 10. Sprites | 4 |
| 05. Textures | 114 | | 11. Datas | 8 |
| 06. Materials | 56 | | Settings | 2 |
| TripoModels | 12 | | UnityTechnologies | 15 (FX용 파티클 프리팹/텍스처) |
| DamageNoise.asset | 1 | | Packages/* | 99 (URP/Cinemachine 등 패키지 내부) |

확장자: `.mat 158, .tif 96, .cs 66, .png 61, .prefab 29, .shader 25, .fbx 24, .asset 15, .jpg 8, .mp3 6, .controller 6, .anim 3, .ogg 2 …`

사용 중인 프리팹 29개:
- `07. Prefabs/Obstacle3D/` Gm_Obstacle_ Boo, Blockade, SpikeBall, Fan, Whomp, Robot_Arm, Octoomba, Lakitu, Goomba, SpikeRock, Pipe
- `07. Prefabs/Enemy/` Lakitu, Octoomba, Goomba, Boo, Whomp
- `07. Prefabs/Prefab_Track/` BrickFloor, Track_1, Track_3
- `07. Prefabs/Prefab_Background/` Background_1~3
- `07. Prefabs/Prefab_Item/` HP, KEY_CARD
- `07. Prefabs/Effect/` FX_TinyExplosion, FX_DustExplosion
- `07. Prefabs/Sound/` BgmChangeSystem
- `UnityTechnologies/.../Prefabs/` TinyExplosion, DustExplosion (FX_ 프리팹의 원본)

## 4. 프리팹 참조 방식 (전부 직접 참조)

- `InteractionObjectPool.datas` → `11. Datas/ObjectData.asset` (`ObjectData[].prefab`, GameObject 직접 참조)
- `EffectPool.datas` → `11. Datas/EffectData.asset` (`EffectDataSO.effectDatas[].prefab`)
- `MapManager.trackPrefabs` / `backgroundPrefabs` (`GameObject[]`, 씬 인스펙터 직렬화)
- 패턴: `CreateObjectData3D/2D.asset` → `Patterns/*` (`ObjectType` enum만, 프리팹 참조 없음)
- 모두 씬 → SO/컴포넌트 → 프리팹의 **하드 참조**라, 씬이 로드되면 의존성이 같이 메모리에 올라온다.

## 5. 위험 요소

- **중복 포함**: 씬이 Build Settings에 남아 있는 상태에서 그 씬이 직접 참조하는 에셋을 Addressable 그룹에 넣으면, 같은 에셋이 플레이어 빌드(씬 의존성)와 번들 양쪽에 들어간다(Addressables Analyze의 "Check Scene to Addressable Duplicate Dependencies" 규칙 대상). 런타임 로딩을 Addressables로 바꾸지 않으면 그룹에 담기만 해서는 용량만 늘고 이득이 없다.
- 첫 씬(Title)은 Build Settings에 반드시 남아야 한다(부트스트랩). Title의 의존성은 플레이어 빌드에 포함된다.
- `.cs` 스크립트, `Packages/` 내부 에셋은 엔트리로 넣지 않는다(스크립트는 번들 불가, 패키지 에셋은 암시적 의존성으로 처리).
- `Settings/PC_Renderer.asset`, URP 에셋은 Graphics/Quality 설정에서 참조 → 플레이어 빌드 쪽. 글리치 머티리얼처럼 렌더러 피처와 씬 양쪽에서 참조되는 에셋은 중복 가능.
- 씬 로딩을 `Addressables.LoadSceneAsync`로 바꾸면 Play Mode Script가 "Use Existing Build"일 때 번들 빌드가 선행돼야 한다(기본값 "Use Asset Database"는 빌드 불필요).
- `Player.Restart`는 `CameraManager`/`GameManager` 등 DDOL 싱글톤이 살아있는 상태에서 씬을 재로드한다. Addressables 씬 로드로 바꿔도 Single 모드면 동작 동일.
