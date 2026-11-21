# BossCeilingSfx — Plan

## 구현 현황 요약

- 완료: 전 항목. 미완료 없음.
- 검증: timeScale 0 컷씬에서 보스가 6.3m를 지난 직후 SFX 소스 재생 시작(클립 5초, Loaded). `AudioData` 미등록 경고 없음.
- 메모: 작업 중 `SampleScene` → `GameScene` 이름 변경(사용자 작업, GUID 동일)이 있었다. Addressables 주소는 `SampleScene` 그대로라 `Player`의 재로드는 영향 없음. 같은 커밋에 포함.

근거: `docs/BossCeilingSfx/Research.md`

## 설계

- 기존 SFX 구조를 그대로 쓴다. `SfxType.BOSS_CEILING_BREAK`를 추가하고 `AudioData.asset`에 `building_explosion_v3.wav`를 등록한다. 보스에는 `ceilingSfx` 필드를 둔다(`roarSfx`와 같은 패턴).
- 천장 이펙트를 생성하는 프레임(`SpawnCeilingEffect`)에서 `SoundManager.Instance.PlaySFX(boss.ceilingSfx)`를 같이 호출한다. 그래서 이펙트와 소리 타이밍이 정확히 같다.

## Phase 1 — 데이터

- [x] `AudioDataSO.cs`: `SfxType` 끝에 `BOSS_CEILING_BREAK`(=5) 추가.
- [x] `Assets/11. Datas/AudioData.asset`: `BOSS_CEILING_BREAK → building_explosion_v3.wav`, volume **1** 등록.
- [x] MCP: 컴파일/콘솔 확인.

## Phase 2 — 코드

- [x] `Boss.cs` (Intro Cutscene 헤더): `ceilingSfx = SfxType.BOSS_CEILING_BREAK` 추가 (Tooltip: 천장을 깨고 나올 때 재생할 효과음).
- [x] `BossIntroState.cs`: `SpawnCeilingEffect`에서 이펙트 생성과 함께 `PlaySFX(boss.ceilingSfx)` 호출. 주석 갱신.
- [x] `BossBowser.prefab`: `ceilingSfx` 값 직렬화 확인(기본값 5).
- [x] MCP: 컴파일/콘솔 확인.

## Phase 3 — 검증

- [x] 플레이: 컷씬 천장 통과 시 `PlaySFX` 1회 호출, `AudioData` 미등록 경고 없음, SFX 소스 재생 확인.
- [x] 커밋·푸시·머지 (사용자 지시)

## 확정 사항 (피드백 반영)

1. 볼륨 1.
2. 구현 후 커밋·푸시·머지까지 진행.
