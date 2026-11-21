# BossLandingSfx — Plan

## 구현 현황 요약

- 완료: 전 항목. 미완료 없음.
- 검증: 천장·울부짖음 SFX를 런타임에서 NONE으로 꺼 착지음만 남긴 상태로 확인. 낙하 중 SFX 소스 재생 없음 → 착지(step Hold) 직후 재생 시작(클립 4초, Loaded, timeScale 0). 미등록 경고 없음.

근거: `docs/BossLandingSfx/Research.md`

## 설계

- `SfxType.BOSS_LANDING`(=6)을 추가하고 `AudioData.asset`에 `monster_landing_v2.wav`를 등록한다. 보스에는 `landSfx` 필드를 추가한다.
- `BossIntroState.Land`에서 착지 이펙트·쉐이크와 같은 프레임에 `SoundManager.Instance.PlaySFX(boss.landSfx)`를 호출한다.

## Phase 1 — 데이터

- [x] `AudioDataSO.cs`: `SfxType` 끝에 `BOSS_LANDING` 추가.
- [x] `Assets/11. Datas/AudioData.asset`: `BOSS_LANDING → monster_landing_v2.wav`, volume **1** 등록.
- [x] MCP: 컴파일/콘솔 확인.

## Phase 2 — 코드

- [x] `Boss.cs` (Intro Cutscene 헤더): `landEffect` 아래에 `landSfx = SfxType.BOSS_LANDING` 추가 (Tooltip: 착지할 때 재생할 효과음).
- [x] `BossIntroState.cs`: `Land`에 `PlaySFX(boss.landSfx)` 추가.
- [x] `BossBowser.prefab`: `landSfx` 직렬화.
- [x] MCP: 컴파일/콘솔 확인.

## Phase 3 — 검증 & 반영

- [x] 플레이: 착지 프레임에 효과음 재생, 미등록 경고 없음, 천장 효과음과 동시에 재생돼도 끊기지 않음.
- [x] 커밋·푸시·머지 (`monster_landing_v2.wav` + `.meta` 포함).

## 확정 사항 (피드백 반영)

1. 볼륨 1.
2. 구현 후 커밋·푸시·머지까지 진행.
