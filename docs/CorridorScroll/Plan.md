# CorridorScroll — Plan

> **구현 현황 (2026-11-06)**: 모든 항목 완료.
> - 미완료: 없음
> - 계획 외 추가: `GenerateBackgroundCoroutine`이 다음 타일을 직전 타일 바로 뒤에 붙여 생성하도록 수정(Phase 2 메모 참고)
> - 검증: 컴파일 성공, 콘솔 에러 0. 경고는 기존 Missing Script 경고(`prop_fan_large` 등)만 있다.

근거: `Research.md`

## 결정 사항 (사용자 피드백)
- 맵 전환 호출 주체는 아직 없다. **기능(API)만** 만든다.
- 코리도가 기존 배경(`Background_2`)을 **대체**한다.
- Dungeon은 조명이 없다. 라이팅 "꺼짐"으로 취급한다.
- 라이팅 전환은 **호출 즉시** 페이드한다.
- `BrickFloor` 트랙은 **없앤다**.
- 2D 전환 시 오른쪽 벽(카메라 쪽 +X 벽)을 숨긴다. 숨기는 방법은 `Right` 레이어 부여다. 기존 카메라 컬링을 그대로 쓰므로 **코드를 바꾸지 않는다**.

## 설계 요약
- 코리도를 **배경 타입 하나로 추가**한다. MapManager의 기존 배경 생성/풀링 로직을 그대로 쓴다.
- 라이트는 씬의 `CorridorBakeLights`(Baked)와 `CorridorLightProbes`가 담당한다. 프리팹에는 라이트가 없다.
- "라이팅 끄기"는 **프로브 SH 교체**로 한다.
  - 켜짐 = 베이크값, 꺼짐 = `RenderSettings.ambientProbe`. 전환할 때만 배열을 Lerp한다.
- 맵 전환 진입점은 `MapManager.ChangeBackground(BackgroundType)` 하나로 둔다.

## Phase 1 — 프리팹을 배경 타일로 만들기 (Lava, Dungeon 동일)
- [x] 루트에 `BackgroundManager` 추가(`backgroundLengthZ = 24`, `parallaxSpeedMultiplier = 1`)
- [x] `CorridorMeshes` 로컬 위치를 y -0.7로
- [x] 이어 붙일 때 경계에서 겹치는 `Pillar_L_6`, `Pillar_R_6` 비활성화
- [x] 2D 카메라 쪽 벽 `Wall_L_*`, `Pillar_L_*`, `Ceiling_Arch_L_*`(프리팹당 19개)에 `Right` 레이어 부여

## Phase 2 — MapManager (CP949 인코딩 유지)
- [x] `BackgroundType` enum에 `LavaCorridor`(3), `DungeonCorridor`(4) 추가
- [x] `[SerializeField] bool generateTrack = true` 추가. false면 트랙 초기 채움과 생성 코루틴을 건너뛴다.
- [x] `ChangeBackground(BackgroundType)`와 `event Action<BackgroundType> onBackgroundChanged` 추가
- [x] ContextMenu `Test/Change To LavaCorridor`, `Test/Change To Background2` 추가
- [x] 씬 `MapGenerator` 설정
  - `backgroundPrefabs[3]` = Lava, `[4]` = Dungeon
  - `nowBackground = LavaCorridor`
  - `generateTrack = false`
- [x] 씬의 고정 인스턴스 `ModularCorridor_Lava`, `ModularCorridor_Lava_1` 삭제
- [x] (계획 외) `GenerateBackgroundCoroutine` 생성 위치 수정
  - 문제: 기존에는 매번 `GeneratePos`에 생성했다. 그래서 프레임 단위 대기로 넘어간 거리만큼 타일 사이에 틈이 생겼다(측정 24.036~24.130m).
  - 수정: 직전 타일 z + 길이에 붙여 생성한다. 수정 후 간격은 정확히 24.000m.
  - 이 수정은 기존 Background_1~3에도 똑같이 적용된다.

## Phase 3 — 맵 라이팅 스위치 (`Assets/02. Scripts/Map/MapLighting.cs`, UTF-8)
- [x] `CorridorLightProbes` 오브젝트에 부착하고 `mapManager`를 연결. 기본값은 `litBackgrounds = { LavaCorridor }`, `fadeTime = 1`.
- [x] `Start`에서 `LightmapSettings.lightProbes`의 런타임 사본을 만든다. 플레이 중 `IsPersistent = false`로 확인했다.
  - 베이크값과 앰비언트 배열을 캐시하고, 현재 맵에 맞춰 즉시 적용한 뒤 이벤트를 구독한다.
- [x] 전환하면 즉시 `fadeTime` 동안 SH를 Lerp한다. 중복 호출 시 이전 코루틴을 중단한다.

## Phase 4 — 베이크와 검증
- [x] 고정 인스턴스를 삭제한 상태로 재베이크(프로브 105개, 라이트맵 0장)
- [x] 컴파일 성공, 콘솔 에러 0
- [x] 3D 플레이: 코리도가 24m 간격으로 이어진다. 트랙 0개, 기둥 겹침 없음.
- [x] 2D 전환: cullingMask에서 `Right`가 제외되고(-65), +X 벽이 숨어 플레이어가 보인다.
  - `Ceiling_Vault`는 화면 위쪽 배경으로만 보이고 시야를 가리지 않는다. 그래서 레이어를 추가하지 않았다.
- [x] `ChangeBackground(Background2)`: 프로브(1.5, 0.2, -10)의 DC가 0.761 → 0.592(앰비언트와 같음)로 즉시 바뀌었다.
  - 새 배경 타일은 마지막 코리도 바로 뒤(z 48)에 생성됐다.
  - `LavaCorridor`로 되돌리면 1초 페이드 후 0.761로 복귀한다.

## 한계
- 맵을 전환해도 이미 화면에 있는 타일은 이전 맵으로 남아 흘러 나간다. 라이팅은 호출 즉시 바뀌므로, 잠깐 이전 맵 타일에 새 라이팅이 적용된다.
- `generateTrack`은 씬에서 수동으로 켜고 끈다. Background_1~3으로 전환해도 트랙은 자동으로 다시 생기지 않는다. 필요하면 `ChangeBackground`에서 함께 바꾸도록 확장한다.
- 프로브가 바뀌면(라이트 위치나 개수 변경) Lighting 창에서 `Generate Lighting`을 다시 눌러야 한다.
