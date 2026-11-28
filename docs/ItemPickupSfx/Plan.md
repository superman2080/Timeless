# ItemPickupSfx — Plan

근거: `docs/ItemPickupSfx/Research.md`
상태: **구현 완료 (2026-11-28)** — 확인 필요 1~3은 모두 기본안으로 확정

## 구현 현황 요약
- 완료: Phase 1~4 전 항목 (12/12). 미완료 없음.
- 플레이 검증: 아이템 6종의 충돌 이벤트를 호출해 획득 시 풀에서 재생된 클립을 확인.
  - HP / KEY_CARD → `keycard_pickUp`
  - P_SWITCH → PSwitch
  - SUPER_STAR → SuperStar
  - MINI_MUSHROOM → MiniMushroom
  - SUPER_MUSHROOM(일반 형태) → SuperMushroom
  - SUPER_MUSHROOM(미니 형태, 점수 대체) → ScoreBonus
- 컴파일 에러 0, 새 경고 0. 콘솔의 Missing Script 경고와 `MaterialLocation.External` 모델 임포트 경고는 원래 있던 것.
- 메모: `HP.prefab` diff에는 프리팹 재저장으로 생긴 순서 변경(같은 오버라이드 재배치)이 섞여 있다. 동작 영향 없음.

## 설계 요약
- 아이템별 `SfxType`을 추가하고, 각 프리팹의 `pickupSfx`/`healSfx`에 지정한다. 재생 코드는 이미 풀을 쓰므로 그대로 둔다.
- `SfxType` 끝에 추가: `ITEM_SUPER_MUSHROOM`, `ITEM_MINI_MUSHROOM`, `ITEM_P_SWITCH`, `ITEM_SUPER_STAR`, `ITEM_SCORE_BONUS`.
- `AudioData.asset` `sfx` 목록에 추가 (volume 기본 0.7, 플레이 후 조정 가능)

| SfxType | 클립 |
|---|---|
| ITEM_SUPER_MUSHROOM | `SFX_Item_01_SuperMushroom_v2.wav` |
| ITEM_MINI_MUSHROOM | `SFX_Item_04_MiniMushroom_v2.wav` |
| ITEM_P_SWITCH | `SFX_Item_06_PSwitch_v2.wav` |
| ITEM_SUPER_STAR | `SFX_Item_02_SuperStar_v2.wav` |
| ITEM_SCORE_BONUS | `SFX_Item_09_ScoreBonus.wav` |

- 프리팹 지정
  - `SUPER_MUSHROOM` → ITEM_SUPER_MUSHROOM
  - `MINI_MUSHROOM` → ITEM_MINI_MUSHROOM
  - `P_SWITCH` → ITEM_P_SWITCH
  - `SUPER_STAR` → ITEM_SUPER_STAR
  - `KEY_CARD` → PICKUP(keycard_pickUp) 유지. 값을 명시적으로 저장한다.
  - `HP` → PICKUP 유지 (의문점 1)
- 점수 대체 획득: `PowerUpItem`에 `public SfxType scoreSfx = SfxType.ITEM_SCORE_BONUS;`를 추가한다. 점수 대체 분기에서는 `pickupSfx` 대신 이 소리를 재생한다.
- `KEY_CARD.prefab`의 잔재 필드 `healSFX`는 프리팹을 다시 저장하면서 정리한다.

## Phase 1 — 코드
- [x] `Datas/AudioDataSO.cs`: `SfxType` 끝에 5개 추가.
- [x] `Obstacles/PowerUpItem.cs`: `scoreSfx` 필드 추가, 점수 분기면 `scoreSfx`, 아니면 `pickupSfx` 재생.
- [x] 컴파일 + 콘솔 확인.

## Phase 2 — 데이터 / 프리팹 (eval)
- [x] `AudioData.asset`에 5개 항목 추가(위 표).
- [x] 프리팹 6개의 `pickupSfx`/`healSfx` 지정 후 저장. 저장 후 YAML 값 확인.
- [x] Missing 참조 없음 확인.

## Phase 3 — 검증
- [x] 에디트 모드 eval: 각 프리팹의 SFX 타입 → `AudioData`에서 기대한 클립으로 조회되는지.
- [x] GameScene 플레이
  - 아이템 6종을 각각 플레이어 레인에 생성(`LaneManager.GenerateRow` 리플렉션 또는 풀 `Get`)해 먹게 한다.
  - 풀에 해당 클립 인스턴스가 활성화되는지 확인.
  - 큰 형태에서 SUPER_MUSHROOM을 먹으면 ScoreBonus가 나는지 확인.
- [x] 콘솔 새 에러/경고 0.
- [x] `runInBackground` 원복, `ProjectSettings` diff 없음 확인.

## Phase 4 — 문서
- [x] `CLAUDE.md` 오디오 단락: 아이템 효과음은 프리팹의 `pickupSfx`/`healSfx`(+`scoreSfx`)로 지정한다고 한 줄 추가.
- [x] Plan.md 상단에 구현 현황 요약.

## 확인 필요 (피드백 요청)
1. **HP 아이템 소리**: 전용 클립이 없다. 기본안은 keycard 소리(PICKUP)를 그대로 쓰는 것이다. 다른 선택지는 두 가지다.
   - (a) 남는 클립 중 하나를 쓴다. 예: `SFX_Item_08_GhostMushroom`, `SFX_Item_05_PowBlock`.
   - (b) 로컬 오디오 생성(Stable Audio)으로 회복음을 새로 만든다.
2. **점수 대체 획득 소리**: 기본안은 `SFX_Item_09_ScoreBonus`. 원래 아이템 소리를 그대로 낼까요?
3. **볼륨**: 새 항목은 0.7로 시작한다. 기존 PICKUP은 0.5다. 다른 기준이 있으면 알려 주세요.
