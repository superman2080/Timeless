# MapLavaLighting — Research

목표
- 기본 배경 타일을 `ModularCorridor_Dungeon`으로 바꾼다.
- `ModularCorridor_Lava`를 N번(기본 20)마다 1타일 끼워 넣는다.
- 용암 타일이 프로브 구역을 지나가는 동안에만, 그리고 타일이 덮고 있는 프로브에만 용암 조명이 켜지게 한다.

## 관련 파일

| 경로 | 역할 |
|---|---|
| `Assets/02. Scripts/Map/MapManager.cs` | 트랙/배경 타일 생성·풀링. `BackgroundType` enum, `ChangeBackground`, `onBackgroundChanged` 정의 |
| `Assets/02. Scripts/Map/BackgroundManager.cs` | 배경 타일 1개. `Update`에서 `-Z`로 `mapSpeed × parallaxSpeedMultiplier`만큼 이동, `DisposePos.z` 이하가 되면 `SetActive(false)` |
| `Assets/02. Scripts/Map/MapLighting.cs` | 라이트 프로브 SH를 lit ↔ off로 교체. 커밋 a87e762에서 추가됨 |
| `Assets/07. Prefabs/Prefab_Background/ModularCorridor_Dungeon.prefab` | guid `a2420d79...`, `backgroundLengthZ: 24`, `parallaxSpeedMultiplier: 1` |
| `Assets/07. Prefabs/Prefab_Background/ModularCorridor_Lava.prefab` | guid `d0ea1095...`, `backgroundLengthZ: 24`, `parallaxSpeedMultiplier: 1` |
| `Assets/01. Scenes/SampleScene.unity` | MapManager, MapLighting, `CorridorBakeLights`, `CorridorLightProbes`가 있는 씬 |
| `Assets/01. Scenes/SampleScene/LightingData.asset` | 베이크된 프로브 데이터 |
| `docs/CorridorLighting/Plan.md` | 지금의 라이팅 구조(에미션 + 프로브)를 만든 이전 작업 |

## 현재 동작

### MapManager (SampleScene 설정값)
- `nowBackground: 3`(LavaCorridor), `generateTrack: 0`(트랙은 생성 안 함)
- `backgroundPrefabs` 인덱스는 enum과 1:1이다. `[3]` = Lava(`d0ea...`), `[4]` = Dungeon(`a242...`)
- `GameManager`: `generatePos.z = 70`, `disposePos.z = -50`
- `Start`
  - `InitializeBackgroundFill()`: z=70부터 24씩 빼 가며 70, 46, 22, -2, -26까지 `nowBackground` 타일 5개를 채운다.
  - 그다음 `GenerateBackgroundCoroutine()`을 시작한다.
- `GenerateBackgroundCoroutine`
  - 첫 회차는 `lastBg == null`이라 `GeneratePos`(z=70)에 생성한다. 이후로는 `lastBg.z + BackgroundSize.z` 위치에 붙여 생성한다.
  - `lastBg.z <= 70 - 24`가 될 때까지 대기한다.
  - **기존 문제**: 첫 회차 타일이 Initialize의 z=70 타일과 같은 위치에 겹쳐 생성된다. 지금은 같은 타입이라 티가 안 난다. 하지만 Lava/Dungeon이 섞이면 서로 다른 메시가 겹쳐 z-fighting이 생길 수 있다.
- `GenerateBackground(type, ...)`: 자식 중 비활성 상태이면서 `backgroundType`이 같은 타일을 재사용한다. 없으면 `Instantiate`하고 `bg.backgroundType = type`을 넣는다. 프리팹에 저장된 `backgroundType`은 0이지만 런타임에 덮어쓰므로 문제없다.
- 타일 span: 다음 타일을 `z + 24`에 붙이므로 타일 하나는 `[z, z+24]`를 덮는 것으로 추정된다. **메시 pivot 위치는 구현 때 MCP로 bounds를 확인해야 한다.**

### MapLighting (SampleScene 설정값: `litBackgrounds: [3]`, `fadeTime: 1`)
- `Start`
  - `LightmapSettings.lightProbes`를 `Instantiate`한 사본으로 바꾼다. 원본 에셋은 보호된다.
  - `lit` = 베이크된 SH 105개, `off` = 전부 `RenderSettings.ambientProbe`로 채운다.
  - 처음 상태는 `nowBackground`가 lit 목록에 있으면 lit, 아니면 off다.
- `onBackgroundChanged`를 받으면 **프로브 105개 전체**를 `fadeTime`초 동안 보간한다.
- 문제: `ChangeBackground`는 다음 스폰 타입만 바꾸고, 타일은 z=70에서 생성된다. 그래서 조명이 실제 타일보다 먼저(약 (70-6)/mapSpeed초) 바뀐다. 또 화면에 Dungeon과 Lava가 같이 보이면 둘이 같은 조명을 받는다.
- `ChangeBackground` / `onBackgroundChanged` / `nowBackground`를 쓰는 곳은 MapManager와 MapLighting뿐이다(grep으로 확인). 나머지는 ContextMenu 테스트용이다.

### 라이팅 구조 (`docs/CorridorLighting` 결과)
- 프리팹 안에는 라이트가 없다(Lava, Dungeon 둘 다 Light 컴포넌트 0개).
- 코리도 메시는 `ReceiveGI = LightProbes`, `BlendProbes`다. 스크롤되므로 static이 아니다.
- 용암 빛은 Lava Floor/Rail 에미션(`BakedEmissive`)과 씬의 `CorridorBakeLights`(Baked Point, Lava 라이트 14개를 복제한 것)가 프로브에 베이크된 값이다.
  - 즉 **베이크된 프로브 SH 자체가 "용암 조명"**이다. Baked 라이트는 런타임에 끌 수 없다.
- `CorridorLightProbes`: x {-3,-1.5,0,1.5,3} × y {0.2,1.8,4} × z {-18,-14,...,6}, 총 105개. 위치는 런타임에 `LightmapSettings.lightProbes.positions`로 얻을 수 있고, 인덱스는 `bakedProbes`와 같다.
- off 상태는 `RenderSettings.ambientProbe`(Skybox 모드, `m_AmbientMode: 0`)이다. Dungeon은 이 하늘 앰비언트만 받는다. 지금 MapLighting의 off 상태와 같다.
- 실시간 라이트는 Directional Light 1개뿐이다.

## 의존 관계
- `MapLighting` → `MapManager.nowBackground`, `onBackgroundChanged` 구독
- `MapManager` → `GameManager.Instance.GeneratePos/DisposePos`
- `BackgroundManager` → `GameManager.Instance.mapSpeed/DisposePos`
- 플레이어, 장애물 등 움직이는 모든 렌더러가 같은 프로브를 샘플링한다. 따라서 용암 조명은 그 위치를 지나는 플레이어/장애물에도 적용된다(의도한 동작).

## 위험 요소
- 위에 적은 첫 회차 중복 생성: 타입이 섞이면 겹침이 눈에 보일 수 있다.
- 타일 span 방향/pivot을 잘못 가정하면 조명이 24유닛 어긋난다.
- `bakedProbes` 대입은 배열 전체를 복사한다. 프로브가 105개라 매 프레임 대입해도 싸지만, 바뀐 프레임에만 대입하는 게 좋다.
- off SH(하늘 앰비언트)가 Dungeon에 충분히 밝은지는 기존 상태와 같으므로 이번 작업 범위 밖이다.
- 씬 YAML의 MapLighting 직렬화 필드(`litBackgrounds`, `fadeTime`)를 바꾸면 기존 값이 사라진다. 인스펙터에서 다시 설정해야 한다.
