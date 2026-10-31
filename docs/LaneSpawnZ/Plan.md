# LaneSpawnZ — Plan

## 구현 현황
- 완료: Phase 1, Phase 3
- 제외: Phase 2 (사용자 결정으로 Utils 유지)

목표: 레인 Z를 카메라 교차점 대신 `LaneManager.spawnZ` 고정값으로 정한다.

## Phase 1. LaneManager
- [x] `Assets/02. Scripts/Map/LaneManager.cs`
  - 필드 추가: `public float spawnZ = 20f;` (Tooltip: Lane/object spawn Z position)
  - `InitLanes()`: `basePosition = new Vector3(0f, 0f, spawnZ);`로 교체
  - CP949 인코딩과 LF 줄바꿈을 유지했다 (diff 4줄).

## Phase 2. Utils 정리
- [ ] ~~`Utils.cs` 삭제~~ — 사용자 결정으로 남겨둔다. 현재 호출하는 곳은 없다.

## Phase 3. 검증 (MCP)
- [x] 컴파일 완료, 콘솔 에러 0 (경고는 기존 배경 Missing Script 경고만 있음)
- [x] 에디터에서 카메라 pitch 25/40/60으로 `InitLanes`를 실행 → lower (-1, 0, 20), upper (-1, 1.5, 20)으로 동일. 카메라는 원래대로(40°) 되돌렸다.
- [x] CLAUDE.md "레인 & 스폰" 설명을 `spawnZ` 기준으로 갱신
  - 참고: 에디터에서 레인 Transform이 Z=20으로 다시 배치되어 SampleScene이 dirty 상태일 수 있다 (저장 안 함).
