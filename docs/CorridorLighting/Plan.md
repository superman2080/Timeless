# CorridorLighting — Plan

> **구현 현황 (2026-11-06)**: 모든 항목 완료.
> - 미완료: 없음
> - 계획과 달라진 점: Contribute GI를 에미션 메시에만 적용(Phase 1), 프로브 x 범위를 ±3으로 축소(Phase 2)
> - 결과: SampleScene의 실시간 라이트는 Directional Light 1개만 남음. 프로브 105개, 라이트맵 0장.

근거: `Research.md`

## 결정 사항 (사용자 피드백)
- 방식: **에미션 + Light Probe**. 라이트맵 베이크는 하지 않는다.
- 코리도는 앞으로 **스크롤된다**(MapManager 등으로 스폰/이동). 그래서 라이트맵은 쓸 수 없다.
- `ModularCorridor_Dungeon`도 같이 처리한다.

## 설계
- 프리팹 안의 라이트는 **완전히 삭제**한다.
  - 런타임에 스폰한 프리팹 인스턴스는 베이크 결과(`bakingOutput`)가 없다. 라이트를 Baked 모드로 바꿔 둬도 실시간으로 켜져 버리기 때문에 삭제해야 한다.
- 용암 빛은 바닥/레일 머티리얼의 에미션이 낸다. 에미션 GI 플래그를 `Baked`로 바꿔 프로브 베이크에 반영되게 한다.
- Light Probe Group은 **씬에 고정**한다. 월드가 플레이어 쪽으로 흘러오므로, 카메라 주변 고정 구역에 프로브를 두면 지나가는 모든 오브젝트(코리도 메시, 플레이어, 장애물)가 그 위치의 프로브를 샘플링한다.
- 코리도 메시는 `Receive GI = Light Probes`로 설정한다. Batching Static은 **켜지 않는다**(켜면 스크롤이 안 됨).
- 차가운 청록 보조광(VaultColdFill)은 에미션 소스가 없다. 그래서 **씬 전용 Baked 라이트**(`CorridorBakeLights`, 프로브 베이크에만 쓰이고 런타임 비용 0)로 SampleScene에 남긴다.

## Phase 1 — 프리팹
- [x] Lava, Dungeon 프리팹에서 `CorridorLights`와 그 자식 라이트 삭제(Lava 14개, Dungeon 5개)
- [x] 두 프리팹의 MeshRenderer(각 62개)에 `ReceiveGI = LightProbes`, `LightProbeUsage = BlendProbes` 설정
  - 변경: `ContributeGI` 플래그는 **에미션 메시에만**(Lava의 Floor/Rail 18개, Dungeon 0개) 줬다.
  - 이유: 처음에 벽·천장 전부에 줬더니 벽이 하늘 앰비언트를 가려 프로브가 어두워졌다(DC 0.14, 원래 앰비언트는 0.6~1.0). 그 결과 화면 전체가 거의 검게 나왔다. 기존 실시간 라이트도 그림자가 꺼져 있어 아무것도 가리지 않았으므로, 가리는 물체를 빼는 쪽이 원래 룩에 가깝다.
- [x] `M_Corridor_LavaFloorBase`, `M_Corridor_LavaFloorTrack`의 `globalIlluminationFlags`를 `BakedEmissive`로

## Phase 2 — 씬 (SampleScene)
- [x] 씬 인스턴스의 라이트 오버라이드(`UniversalAdditionalLightData` 추가분) 정리
  - 원본 라이트가 삭제되면서 자동으로 사라졌다. 남은 오버라이드는 없다.
- [x] `CorridorBakeLights` 생성: 기존 Lava 라이트 14개와 같은 위치(+씬 오프셋 y -0.7)·색·세기의 **Baked** Point Light
- [x] `CorridorLightProbes` 생성: x {-3,-1.5,0,1.5,3} × y {0.2,1.8,4} × z -18~6(4m 간격) = 105개
  - 변경: 처음에 둔 x=±4 프로브는 벽 메시(중심 x 4.15) 안쪽에 들어가 검게 나와서 ±3으로 줄였다.
- [x] `Assets/01. Scenes/SampleScene/SampleSceneLighting.lighting`을 만들어 씬에 지정. Baked GI 켬, Realtime GI 끔, Progressive GPU, Auto Generate 끔
- [x] 베이크 완료(라이트맵 0장, 프로브 105개)
- [x] 베이크 전후 게임 뷰 비교: 벽·바닥의 전체 톤은 거의 같다. 바닥 가장자리에 맺히던 주황 빛 웅덩이만 사라졌다.

## Phase 3 — 검증
- [x] 실시간 라이트: Directional Light 1개만 남음(나머지는 모두 `bakingOutput.isBaked`)
- [x] 컴파일 실패 없음, 작업 후 새로 생긴 콘솔 에러/경고 없음

## 한계 / 후속
- 프로브는 오브젝트마다 하나의 SH 값만 받는다. 그래서 바닥에 맺히던 국소적인 주황 빛 웅덩이는 재현되지 않는다. 필요하면 용암 에미션 강도를 올려 보완한다.
- Dungeon은 에미션 소스가 없고 SampleScene에도 없다. Dungeon을 쓰는 씬에서는 그 씬에 Baked 라이트와 프로브를 배치하고 베이크해야 한다. 원래 라이트 값은 아래와 같다.
  - CyanMist: (0, 2.36, 0/-8/-16), 색 (0.14,0.82,0.62), I 2.6, R 8.5
  - VaultColdFill: (0, 4, 0/-12), 색 (0.16,0.62,0.68), I 2, R 9
- 한 씬에서 Lava와 Dungeon이 섞여 스크롤되면, 고정 프로브는 한 가지 분위기만 담을 수 있다.
- 라이트나 프로브를 바꾸면 Lighting 창에서 `Generate Lighting`을 다시 눌러야 한다(Auto Generate가 꺼져 있음).
