# MapLavaLighting — Plan

> **구현 현황 (2026-11-14)**: 모든 항목 완료.
> - 미완료: 없음
> - 계획과 달라진 점: 타일 span이 `[z-18, z+6]`이라 `tileStartOffset = -18`(직렬화 필드)로 둠. 활성 타일 조회는 `Backgrounds`(전체 목록이고 활성 여부는 MapLighting이 거름)
> - 검증: 컴파일 에러 0, 새 경고 0. 첫 화면 Dungeon 5타일, 겹침/틈 없음. 프로브별 가중치가 타일 구간과 일치

근거: `Research.md`

## 결정 사항 (사용자 피드백)
- 기본 배경은 `ModularCorridor_Dungeon`(`BackgroundType.DungeonCorridor`)으로 한다.
- 용암은 **N번째 생성마다 1타일**만 끼운다. N=20이 기본값이고 인스펙터에서 바꿀 수 있다.
- 라이팅은 **프로브별** 방식으로 한다. 각 프로브가 용암 타일 구간 안에 있으면 lit, 밖이면 off.
- 2D 뷰에서도 용암 생성과 조명을 그대로 유지한다.
- 용암 조명은 플레이어/장애물에도 적용된다(같은 프로브를 샘플링).

## 설계

### 생성 (MapManager)
- 새 필드
  - `[SerializeField] BackgroundType specialBackground = LavaCorridor`
  - `[SerializeField, Min(0)] int specialInterval = 20` (0이면 끔)
  - `private int bgSpawnCount`
- 다음 타입 결정: `NextBackgroundType()`
  - `bgSpawnCount++` 후 `specialInterval > 0 && bgSpawnCount % specialInterval == 0`이면 `specialBackground`, 아니면 `nowBackground`
  - Initialize와 코루틴 모두 이 함수를 쓰므로 카운트가 이어진다. 시작 화면 5타일 안에서는 N≤5가 아니면 용암이 안 나온다.
- 첫 회차 중복 생성 수정
  - `InitializeBackgroundFill`이 맨 앞(z=70) 타일을 반환한다. 코루틴은 그 타일을 `lastBg` 초깃값으로 받아 그 뒤(z=94)부터 붙인다.
  - 이렇게 해야 Lava/Dungeon이 같은 위치에 겹치지 않는다.
- `ChangeBackground`, `onBackgroundChanged`, ContextMenu는 남겨 둔다(기본 타입 교체용). 다만 MapLighting은 더 이상 구독하지 않는다.
- 활성 타일 조회용으로 `public IEnumerable<BackgroundManager> ActiveBackgrounds`를 추가한다. 매 프레임 `GetComponentsInChildren` 할당을 피하려고, 생성한 타일을 `List<BackgroundManager>`에 캐시하고 activeSelf로 거른다.

### 라이팅 (MapLighting 재작성)
- 필드
  - `mapManager`
  - `litBackgrounds` (기본 `{ LavaCorridor }`)
  - `[SerializeField, Min(0)] float blendDistance = 4f`: 타일 경계 바깥으로 lit이 0이 될 때까지 걸리는 거리(유닛)
- `Start`: 지금처럼 프로브 사본을 만들고 `lit`/`off`를 준비한다. 추가로 `probeZ[i] = positions[i].z`를 저장한다.
- `LateUpdate`
  - 프로브마다 가중치 `w[i]`를 계산한다.
    - 활성 lit 타일마다 구간 `[z0, z1]`을 구한다. z0 = `transform.position.z + spanOffset`, z1 = z0 + `backgroundLengthZ`.
    - 프로브 z가 구간 안이면 거리 d = 0, 밖이면 가장 가까운 경계까지의 거리다.
    - `w = 1 - saturate(d / blendDistance)`이고, 타일이 여럿이면 max를 쓴다.
  - 이전 프레임과 다른 w가 하나라도 있을 때만 `current[i] = Lerp(off, lit, w)`를 계산해 `bakedProbes`에 대입한다.
- `spanOffset`은 메시 pivot 확인 결과에 따라 정한다. 기본값은 0(span `[z, z+24]`)이다. 확인 결과가 다르면 상수나 직렬화 필드로 맞춘다.
- `fadeTime`, `FadeCoroutine`, `OnBackgroundChanged` 구독은 제거한다.

## Phase 1 — 확인 (MCP)
- [x] Lava/Dungeon 프리팹 루트 pivot 대비 렌더러 bounds의 z 범위를 확인해 tile span(`[z, z+24]` 여부)을 확정하고 `spanOffset`을 결정
  - 결과: 두 프리팹 모두 pivot 기준 렌더러 z 범위가 -18 ~ 6.6이다. span = `[z-18, z+6]`이므로 MapLighting에 `tileStartOffset = -18` 직렬화 필드를 뒀다.

## Phase 2 — MapManager.cs
- [x] `specialBackground`, `specialInterval`(기본 20), `bgSpawnCount` 필드 추가 (Header: 특수 배경)
- [x] `NextBackgroundType()` 추가. `InitializeBackgroundFill`과 `GenerateBackgroundCoroutine`에서 `nowBackground` 대신 사용
- [x] `InitializeBackgroundFill`이 맨 앞 타일을 반환하고, 코루틴이 그 타일을 `lastBg` 초깃값으로 받도록 변경(첫 회차 중복 생성 제거)
- [x] 활성 배경 캐시 리스트와 `ActiveBackgrounds` 프로퍼티 추가(`GenerateBackground`의 Instantiate 경로에서 등록)
  - 변경: 이름은 `Backgrounds`(`IReadOnlyList`, 비활성 포함)로 했다. 활성 여부는 MapLighting에서 `activeInHierarchy`로 거른다.
- [x] 인코딩 확인: 파일이 UTF-8이면 그대로, CP949면 CP949를 유지
  - 결과: CP949 + CRLF였다. UTF-8 사본에서 편집한 뒤 CP949로 되돌려 저장했다(diff는 변경 줄만).

## Phase 3 — MapLighting.cs
- [x] 이벤트 구독/페이드 코루틴 제거, `blendDistance` 필드 추가
- [x] `probeZ` 캐시, 프로브별 가중치 계산, 변경이 있을 때만 `bakedProbes` 대입하는 `LateUpdate` 구현
- [x] 클래스 summary 주석을 새 방식에 맞게 수정

## Phase 4 — 씬 (SampleScene)
- [x] MapManager `nowBackground` → `DungeonCorridor`(4), `specialBackground` = `LavaCorridor`, `specialInterval` = 20
- [x] MapLighting의 `litBackgrounds` = [LavaCorridor] 유지, `blendDistance` = 4 확인. 제거된 `fadeTime` 필드는 직렬화에서 자동으로 정리됨
- [x] 씬 저장

## Phase 5 — 검증 (MCP)
- [x] 컴파일 후 콘솔 에러/경고 확인(기존 Missing Script, `Ray does not intersect` 경고는 제외)
  - 에러 0. 경고는 기존 Missing Script, fbx MaterialLocation, cubemap 버전 경고뿐이다.
- [x] 플레이 검증(runInBackground 켜기): 처음 화면은 전부 Dungeon인지, 겹친 타일이 없는지 확인
  - 시작 시 Dungeon 5타일(z 69.8 ~ -26.2), 중복 없음. 이후에도 연속 타일 간격 24 유지(틈/겹침 없음)
- [x] 검증 중에만 `specialInterval`을 작게(예: 3) 줘서 Lava 타일이 나오는지 확인. 프로브 z별 SH DC(`current[i][0,0]`)를 로그로 찍어, 타일이 덮는 프로브만 lit이고 지나간 뒤 off로 돌아오는지 확인
  - 씬 값은 바꾸지 않고 플레이 중 리플렉션으로 3을 줬다. 예: Lava@8.5(span -9.5~14.5)일 때 z -6~6 = 1.00, z -10 = 0.87(경계 밖 0.5), z -14 = 0. 지나간 뒤 전부 0으로 복귀.
  - 참고: 베이크 lit과 off의 SH 차이 자체가 작다(평균 DC 0.820 vs 0.784). 그래서 벽/플레이어 밝기 변화는 은은하고, 눈에 띄는 건 용암 바닥 에미션이다. 조명을 더 강하게 하려면 `CorridorBakeLights` 세기를 올려 다시 베이크해야 한다(범위 밖).
- [x] `capture_game_view`로 Lava 통과 전/중/후를 눈으로 확인하고, 캡처 파일과 `.meta`는 삭제
- [x] 검증용으로 바꾼 값을 원래대로(20) 되돌리고, `LightingData.asset`이나 렌더러 에셋에 의도치 않은 diff가 없는지 `git status`로 확인
  - 씬 `specialInterval`은 20 그대로. git 변경은 SampleScene, MapManager.cs, MapLighting.cs와 docs뿐이다.

## 남은 의문점
- 없음. 2D 유지와 장애물 적용은 사용자 결정으로 확정됐다.
