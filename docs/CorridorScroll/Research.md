# CorridorScroll — Research

목표는 세 가지다.
1. `ModularCorridor_Lava`가 MapGenerator(`MapManager`)에 의해 이어 붙으며 계속 생성되게 한다.
2. 라이트는 프리팹이 아니라 씬에 둔다.
3. 맵이 바뀌면 코리도 라이팅이 꺼지게 한다.

이전 작업 기록: `docs/CorridorLighting/`

## 현재 상태 (이전 작업 결과)
- 두 코리도 프리팹 모두 라이트가 **이미 제거**돼 있다.
- 라이트는 씬의 `CorridorBakeLights`(Baked Point 14개)로 옮겨져 있다.
- 씬 고정 `CorridorLightProbes`(105개)에 베이크 완료. 런타임 실시간 라이트는 Directional Light 1개뿐이다.
- 즉 "라이트를 프리팹에서 떼서 씬에 배치"는 이미 끝난 상태다. Baked 라이트는 런타임에 꺼고 켤 수 없다. 이 문제는 아래 "라이팅 끄기"에서 다룬다.

## 맵 생성 코드 (`Assets/02. Scripts/Map/`, MapManager/BackgroundManager는 **CP949 인코딩**)
- 씬 오브젝트 `MapGenerator`에 `MapManager`가 붙어 있다.
  - `trackPrefabs`: `Track_1`, `BrickFloor`, `Track_3`(각 3×10). `nowTrack = Tile2`(BrickFloor).
  - `backgroundPrefabs`: `Background_1~3`(`backgroundLengthZ` 40). `nowBackground = Background2`.
  - **배열 인덱스와 `BackgroundType` enum 값이 1:1로 대응한다**(`backgroundPrefabs[(int)type]`).
- `Start`에서 트랙과 배경을 각각 다음 순서로 처리한다.
  - `Initialize*Fill()`: `GeneratePos.z`(70)부터 `DisposePos.z`(-50)까지 채운다.
  - `Generate*Coroutine()`: 마지막 타일이 자기 길이만큼 이동하면 `GeneratePos`에 다음 타일을 만든다.
- `GenerateBackground(type, pos, rot)`
  - 먼저 `MapGenerator` 자식 중에서 같은 `backgroundType`인 비활성 오브젝트를 찾아 재사용한다.
  - 없으면 `Instantiate`한 뒤 `bg.backgroundType = type`으로 지정한다.
  - **position을 `GeneratePos`로 덮어쓴다**(y=0).
- `BackgroundManager`(배경 프리팹 루트에 필요)
  - `backgroundLengthZ`(수동 길이)와 `parallaxSpeedMultiplier`를 가진다.
  - `Update`에서 `-Z`로 이동하고, `DisposePos`를 지나면 `SetActive(false)`.
  - `meshRenderer`가 없으면 `Awake`에서 `GetComponentInChildren<Renderer>()`로 찾는다.
- `nowBackground`/`nowTrack`을 **런타임에 바꾸는 코드는 없다.** 즉 지금은 "맵 전환" 기능 자체가 없다. 다른 스크립트에서 `nowBackground`를 참조하는 곳도 없다.

## 코리도 프리팹 치수 (씬 인스턴스에서 측정, 인스턴스 y=-0.7)
- 4m 모듈 6개로 이루어져 있다. 모듈 경계는 z +6 ~ -18이다. 따라서 **반복 주기는 24m**다.
  - 사용자가 씬에 놓은 `ModularCorridor_Lava_1`의 z가 23.89다. 주기 24와 거의 같다.
- `Pillar_L/R_0`(z 중심 +6)과 `Pillar_L/R_6`(z 중심 -18)이 양 끝 경계에 있다. 그래서 24m로 이어 붙이면 **기둥이 같은 위치에 2개씩 겹친다.**
- 바닥 상단 y는 -0.25(로컬 +0.45)이고, 중앙 레일 상단은 y 0.01(로컬 +0.71)이다. 씬 인스턴스가 y -0.7에 있어야 레일 상단이 트랙 높이(≈0)에 맞는다.
  - 그런데 MapManager는 y=0으로 스폰하므로 **프리팹 안에서 y -0.7 오프셋을 줘야 한다.**
- 폭은 x ±4.77이다. `Background_2`(폭 20, 길이 41.6)와 같이 쓰면 공간이 겹친다.
- 프리팹 루트에 `BackgroundManager`가 없다.
- 씬에 고정 인스턴스 `ModularCorridor_Lava`, `ModularCorridor_Lava_1`이 있다. 스크롤로 바꾸면 이 둘은 필요 없다.

## 라이팅 구조와 끄는 방법
- 프로브는 씬에 고정돼 있고, 지나가는 오브젝트(코리도, 플레이어, 장애물)가 현재 위치에서 프로브를 샘플링한다. 스크롤과 호환된다.
- Baked 라이트는 GameObject를 꺼도 런타임 조명이 바뀌지 않는다. 결과가 프로브 SH 데이터에 이미 구워져 있기 때문이다.
- 런타임에 바꿀 수 있는 것은 `LightmapSettings.lightProbes.bakedProbes`(`SphericalHarmonicsL2[]`, set 가능)다.
  - **켜짐** = 베이크된 배열. **꺼짐** = 모든 프로브를 `RenderSettings.ambientProbe`(하늘 앰비언트)로 채운 배열. 꺼진 상태는 프로브가 없던 기존 배경 맵의 조명과 같다.
  - 두 배열 사이를 Lerp하면 크로스페이드도 된다.
- 위험: 에디터 플레이 중에 `bakedProbes`를 바꾸면 LightingData 에셋의 LightProbes 객체가 수정될 수 있다. 이 프로젝트는 이미 렌더러 에셋이 플레이 중에 오염되는 문제를 겪었다.
  - 대응: Start에서 `LightmapSettings.lightProbes = Instantiate(...)`로 **런타임 사본**을 만들어 쓴다.
- 베이크 시 주의: 씬 고정 코리도 인스턴스를 지우면 용암 에미션 메시가 베이크에 참여하지 않는다. 프로브는 `CorridorBakeLights`(용암 언더글로 12개, 청록 2개)와 하늘에서만 빛을 받는다. 용암 언더글로 라이트가 같은 역할을 하므로 차이는 작다.

## 뷰 전환과의 관계
- `GameManager.ChangeViewMode`(HP 기반 3D↔2D)는 카메라와 픽셀화만 바꾸고 맵은 그대로 둔다. "맵 변경"과는 별개다.

## 2D 뷰와 오른쪽 벽
- `CameraManager.SwitchCameraCoroutine`(`Camera/CameraManager.cs:116-127`)
  - 2D로 전환하면 `Camera.main.cullingMask &= ~LayerMask.GetMask("Right")`, 3D로 돌아오면 `~0`으로 되돌린다.
  - 즉 **`Right` 레이어(6번)에 넣기만 하면 2D에서 자동으로 숨는다.** 코드를 바꿀 필요가 없다.
- 기존 배경과 트랙 프리팹도 같은 방식을 쓴다. `Background_1`에 20개, `Background_2`에 9개, `Track_1`에 12개 오브젝트가 `Right` 레이어다.
- `vCam_2D`는 위치 (5.64, 0.6, -6.43), forward (-1, 0, 0)이다. 즉 **+X 쪽에서 -X를 바라본다.** 플레이어는 2D에서 마지막 레인(x=+1)에 고정된다.
- 따라서 카메라와 플레이어 사이를 가리는 쪽은 +X 벽이다. 코리도에서 +X에 있는 메시는 이름이 `_L_`로 끝난다(이름과 좌표가 반대).
  - `Wall_L_0~5`: x 3.54~4.77
  - `Pillar_L_0~6`: x 3.15~4.35
  - `Ceiling_Arch_L_0~5`: x 3.59~4.31
  - `Floor_L_*`(x 0~4)는 바닥이라 시야를 가리지 않는다.
  - `Ceiling_Vault_*`는 x -4.2~4.2 전체를 덮는다. 카메라 높이(y 0.6)보다 한참 위(로컬 y 3.7 이상)에 있어서 대부분 프레임 밖이다. 검증 단계에서 캡처로 확인한다.
- 두 코리도 프리팹 모두 현재 `Right` 레이어 오브젝트는 0개다.

## 트랙(BrickFloor)
- `Track`은 `MapManager`만 사용한다. 다른 스크립트에서 참조하지 않는다.
- 플레이어 Rigidbody는 `useGravity = false`이고, 점프는 커브로 y 오프셋을 계산한다(`Player.cs:266`). 따라서 **트랙 콜라이더가 없어도 플레이어가 떨어지지 않는다.**
- `GenerateTile`은 `trackPrefabs[(int)type]`로 인덱싱한다. 그래서 배열을 비우면 IndexOutOfRange가 난다. 트랙 생성을 끄려면 코드에 스위치가 필요하다.
