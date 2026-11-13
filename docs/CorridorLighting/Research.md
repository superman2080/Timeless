# CorridorLighting — Research

목표: `ModularCorridor_Lava`의 실시간 포인트 라이트 14개가 드는 런타임 비용을 없애거나 줄인다. 방법은 라이트 베이크 또는 에미션으로 대체.

## 관련 파일

| 경로 | 내용 |
|---|---|
| `Assets/07. Prefabs/Prefab_Background/ModularCorridor_Lava.prefab` | 루트 → `CorridorMeshes`(FBX 중첩 프리팹) + `CorridorLights`(Point Light 14개) |
| `Assets/03. Models/Static/ModularCorridor_Lava.fbx` | 메시 원본. `.meta`에 `generateSecondaryUV: 0`이라 **라이트맵 UV가 없음** |
| `Assets/06. Materials/ModularCorridor_Materials/M_Corridor_*.mat` | URP/Lit 머티리얼 |
| `Assets/01. Scenes/SampleScene.unity` | 프리팹 인스턴스 1개(pos `0,-0.7,0`). 라이트마다 `UniversalAdditionalLightData`가 씬 오버라이드로 추가돼 있음 |
| `Assets/01. Scenes/SampleScene/LightingData.asset`, `ReflectionProbe-0.exr` | 기존에 베이크한 결과물 |
| `Assets/Settings/PC_Renderer.asset`, `PC_RPAsset.asset` | PC 렌더러: `m_RenderingMode: 2`(**Forward+**) |
| `Assets/Settings/Mobile_Renderer.asset`, `Mobile_RPAsset.asset` | 모바일 렌더러: `m_RenderingMode: 0`(Forward), 오브젝트당 추가 라이트 제한 8개 |

## 라이트 현황 (프리팹 YAML에서 확인)

- `LavaUnderglow_Floor_L_0~5`, `R_0~5`: 12개. Point, Intensity 1.6, Range 4.2, 그림자 없음.
- `VaultColdFill_Ceiling_Vault_1`, `_4`: 2개. Point, Intensity 2, Range 9, 그림자 없음.
- 14개 모두 `m_Lightmapping: 4`(Realtime).
- 어떤 오브젝트에도 Static 플래그가 없음(`m_StaticEditorFlags: 0`, MCP로 확인).

## 메시와 머티리얼 (MCP eval로 확인)

| 메시 | 머티리얼 | 에미션 |
|---|---|---|
| `Floor_L/R_0~5` | `M_Corridor_LavaFloorBase` | **켜짐** (`_EmissionColor` 1.05, 0.85, 0.7) |
| `Rail_Center_0~5` | `M_Corridor_LavaFloorTrack` | **켜짐** (같은 값) |
| `Wall_*`, `Pillar_*`, `Ceiling_Arch_*`, `Ceiling_Vault_*` | WallStone / PillarRound / CeilingArch / CeilingVault | 꺼짐 |

- 용암 바닥은 이미 에미션으로 스스로 빛나 보인다. 포인트 라이트는 그 빛을 **벽·기둥·천장과 지나가는 오브젝트에 비추는 역할**만 한다.

## 움직임 여부

- 이 프리팹 GUID를 참조하는 곳은 `SampleScene.unity`뿐이다. MapManager나 BackgroundManager가 이 프리팹을 스폰하거나 이동시키지 않는다.
- 인스턴스에 이동 스크립트가 없다(MonoBehaviour는 `UniversalAdditionalLightData`만 있음).
- 따라서 **코리도는 정적이라 베이크할 수 있다.**
- 반대로 플레이어, 장애물, 트랙 타일은 움직이는 동적 오브젝트라 라이트맵을 받지 못한다.

## 씬 라이팅 설정

- `m_EnableBakedLightmaps: 1`, `m_EnableRealtimeLightmaps: 0`.
- `m_LightingSettings`가 가리키는 GUID `e94833d3…`는 프로젝트 안에서 찾을 수 없다(**참조가 깨져 있음**). 그래서 지금은 기본 설정으로 동작하며, 베이크하려면 LightingSettings 에셋을 새로 만들어 지정해야 한다.
- 씬에 Light Probe Group이 없다.

## 비용 판단

- **PC(Forward+)**: 그림자 없는 포인트 라이트 14개는 클러스터 컬링으로 처리돼서 부담이 크지 않다. 다만 픽셀마다 라이트 루프 비용은 그대로 든다.
- **모바일(Forward, 오브젝트당 8개 제한)**: 큰 바닥/벽 메시에 라이트가 8개를 넘게 겹치면 일부가 잘리거나, 카메라 위치에 따라 깜빡일 수 있다. 실제 비용 문제는 모바일 쪽이 더 크다.

## 위험 요소

- 베이크하려면 FBX에서 Generate Lightmap UVs를 켜고 재임포트해야 한다. 프리팹 구조는 바뀌지 않는다.
- 라이트를 Baked로 바꾸면 **동적 오브젝트(플레이어/장애물)에는 용암 빛이 비치지 않는다.** 보완하려면 Light Probe Group이 필요하다.
- 베이크하면 `SampleScene/LightingData.asset`과 라이트맵 텍스처가 새로 생기거나 바뀐다. `SampleScene.unity`에는 이미 커밋 안 된 변경이 있다.
- 2D 모드에서는 카메라가 바뀌고 `"Right"` 레이어가 컬링된다. 코리도 레이어는 0(Default)이라 영향이 없다.
