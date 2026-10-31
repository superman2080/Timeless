# Addressables — Plan

> 상태: **구현 중**

## 설계 요약

목표: 빌드 씬 2개가 실제로 쓰는 파일(Research 3번, 508개 중 스크립트/패키지 제외)을 Addressables 그룹으로 관리.

Research 5번(중복 포함) 때문에 **"그룹에 담기만"은 의미가 없다.** 그래서 기본안은:

- **SampleScene을 Addressable로 전환**하고 Build Settings에서 제외 → SampleScene이 끌고 오는 프리팹/머티리얼/텍스처/오디오가 전부 번들로 들어가고 플레이어 빌드와 중복되지 않는다.
- Title은 부트스트랩이라 Build Settings에 유지.
- 씬 로딩 2곳(`StartButton`, `Player.Restart`)만 `Addressables.LoadSceneAsync`로 교체. 프리팹 참조(`ObjectData.asset`, `MapManager` 배열 등)는 **코드 변경 없이 하드 참조 유지** — 씬 번들의 의존성으로 같이 로드된다.
- 주요 에셋은 명시적 엔트리로 그룹에 담아 번들 구성을 통제하고, 모델/텍스처/머티리얼 같은 하위 의존성은 Addressables가 참조 에셋의 번들에 자동 포함(암시적)하게 둔다.

### 그룹 구성 (안)

| 그룹 | 엔트리 | 비고 |
|---|---|---|
| `Scenes` | `SampleScene` (주소 `SampleScene`) | |
| `Prefabs_Obstacle` | `07. Prefabs/Obstacle3D/*`, `Enemy/*`, `Prefab_Item/*` 사용 중인 것 | 라벨 `obstacle` |
| `Prefabs_Map` | `Prefab_Track/*`, `Prefab_Background/*` 사용 중인 것 | 라벨 `map` |
| `Prefabs_FX` | `Effect/*`, `Sound/BgmChangeSystem` | 라벨 `fx` |
| `Datas` | `11. Datas/` 사용 중인 SO (`ObjectData`, `EffectData`, `CreateObjectData3D/2D`, `Patterns/*`, `PlayerState/*`) | 라벨 `data` |
| `Audio` | `08. Audio/` 사용 중인 클립 | 라벨 `audio` |

- 주소(address)는 파일명(확장자 제외)으로 단순화.
- 모든 그룹: 로컬 빌드/로드 경로(기본 `Local`), Pack Together.

---

## Phase 1 — 패키지 설치 / 설정 생성

- [x] `com.unity.addressables` `4.1.1` 설치
- [x] `AddressableAssetSettings` 생성 (`Assets/AddressableAssetsData/`)

## Phase 2 — 그룹/엔트리 생성 (에디터 스크립트 1회 실행, 프로젝트에 스크립트는 남기지 않음)

- [x] 빌드 씬 의존성 목록에서 위 표대로 그룹 생성 + 엔트리 추가 + 라벨
  - 결과: Scenes 1 / Prefabs_Obstacle 18 / Prefabs_Map 6 / Prefabs_FX 3 / Datas 8 / Audio 7 (총 43, 주소 중복 없음)
  - 구현 메모: SampleScene의 의존성 중 Title도 쓰는 에셋은 플레이어 빌드와 중복되지 않게 엔트리에서 제외(해당 그룹 규칙에 걸린 것 0개)
  - 구현 메모: Datas는 **현재 씬이 실제로 참조하는 것만** 들어감 — 씬의 LaneManager가 `DebugCreateObject3D/2D`를 쓰고 있어 패턴은 Pattern3D_09, Pattern3D_23, Pattern2D_00만 포함. `CreateObjectData3D/2D`로 되돌리면 그룹 재생성 필요
- [ ] Analyze 실행 → 중복 의존성 확인. 여러 그룹이 공유하는 머티리얼/텍스처가 크면 `Shared` 그룹으로 분리

## Phase 3 — 씬 로딩 교체

- [x] `StartButton.cs`: `SceneManager.LoadSceneAsync(sceneName)` → `Addressables.LoadSceneAsync(sceneName)` (진행률 `PercentComplete` 사용). ⚠ CP949 파일 — 인코딩 유지하며 해당 줄만 수정
- [x] `Player.Restart`: `SceneManager.LoadScene("SampleScene")` → `Addressables.LoadSceneAsync("SampleScene")`
- [x] Build Settings에서 `SampleScene` 제거 (Title만 남김)

## Phase 4 — 검증

- [x] 컴파일/콘솔 에러 0
- [ ] (미확인) Play Mode Script "Use Asset Database"로 Title → SampleScene 진입, 사망 후 재로드 동작
- [ ] Addressables 콘텐츠 빌드 성공 (번들 크기 기록)
  - 미완료: 에디터에서 빌드 실행 후 결과 확인 전에, 시스템 메모리 부족으로 대기/확인 명령이 강제 종료됨. 빌드 완료 여부 미확인
- [ ] (선택) "Use Existing Build"로 한 번 더 플레이 확인

---

## 확정된 결정

1. **범위: 기본안** — SampleScene Addressable 전환 + 씬 로딩 2곳 수정.
2. **하위 의존성은 암시적** — 별도 지시 없어 기본안대로 모델/텍스처/머티리얼은 엔트리로 명시하지 않음.
3. **전부 로컬 그룹** — 원격(CDN) 없음.
4. **프리팹 하드 참조 유지** — `AssetReference` 전환은 이번 범위 밖.
