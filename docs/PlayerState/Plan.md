# PlayerState — Plan

## 구현 현황 (2026-10-14)
- **완료**: Phase 1~5 전 항목. 미완료 없음.
- 컴파일 에러/경고 0. 콘솔 신규 경고/에러 없음(MCP 타임아웃 로그, 기존 Missing Script 제외).
- 검증: 플레이 모드 진입 후 eval로 21개 항목 확인(아래 Phase 5 메모). 에디터가 백그라운드에서 플레이 프레임을 진행하지 않아 실시간 플레이 관찰은 못 함.
- 계획 외 변경: `PlayerEffectSO`에 `maxHPMultiplier` 추가, `PlayerStateMachine.GetRemainingTime` 추가(아래 메모).

근거: `docs/PlayerState/Research.md`

## 범위
- **하는 것**: 아이템을 잘 수용할 수 있는 플레이어 상태머신 개발 + Player 적용.
- **안 하는 것**: 개별 아이템 내용(슈퍼버섯/스타/미니 등의 수치, 맞았을 때 규칙, 모델/프리팹/`ObjectType`). 아이템마다 기존에 알던 내용과 달라질 수 있으므로 다음 작업에서 아이템별로 정의.
- **삭제**: 기존 "최대 HP 도달 → 무적 + 속도 증가 → 쿨다운" 규칙. 무적은 앞으로 슈퍼스타 상태에서만 발생(스타는 이후 아이템 작업에서 무적 플래그를 켠 상태로 추가).

## 설계 요약

**두 층**으로 나눈다. 둘 다 같은 훅(진입/종료/매 프레임/피격)을 가진 "상태"이고, 차이는 동시에 몇 개가 있을 수 있느냐뿐.

| 층 | 성질 | 이후 예상 사용처 |
|---|---|---|
| **형태(Form)** | 한 번에 하나. `ChangeForm`으로 전환 | 버섯류처럼 플레이어 자체가 바뀌는 아이템 |
| **효과(Effect)** | 형태와 별개로 여러 개 겹침, `duration`이 끝나면 자동 종료(0 = 무기한) | 스타처럼 일정 시간 붙는 아이템 |

최종 스탯 = 형태의 기본 스탯에 활성 효과들의 배율/플래그를 합성. 바뀔 때마다 Player가 한 곳(`ApplyStats`)에서 실제 값에 반영.

```
PlayerStats (struct)                 ← 형태/효과가 바꿀 수 있는 값
  scale, maxHP, mapSpeedMultiplier(월드 속도),
  jumpHeightMultiplier, airTimeMultiplier, hpDecrementMultiplier, invincible

PlayerStateSO : ScriptableObject     ← 형태/효과 공통 베이스 (훅만 가짐)
  virtual void  OnEnter(Player)
  virtual void  OnExit(Player)
  virtual void  OnUpdate(Player, float dt)
  virtual float OnHit(Player, float damage)   ← 받을 데미지를 바꿔 반환 (0 = 무시). 기본: 그대로
  virtual void  ModifyStats(ref PlayerStats)  ← 스탯 기여

PlayerFormSO   : PlayerStateSO       ← stats(절대값). ModifyStats = 덮어쓰기
PlayerEffectSO : PlayerStateSO       ← duration, 배율들 + invincible. ModifyStats = 곱/OR

PlayerStateMachine (Player가 소유하는 일반 C# 클래스)
  CurrentForm, ActiveEffects(남은 시간)
  ChangeForm(form) / AddEffect(effect)(같은 효과면 시간 갱신) / RemoveEffect / Tick(dt)
  float ProcessHit(damage)   ← 효과들 → 형태 순으로 OnHit 통과
  Stats, event onStatsChanged
```

- **데이터 중심 + 필요 시 코드**: 수치만 다른 아이템은 에셋만 만들면 되고, 고유 규칙(예: 맞으면 형태 변경)이 있는 아이템은 SO를 상속해 `OnHit`/`OnEnter` 등을 오버라이드. 상태머신은 그 규칙을 모름.
- **피격 흐름**: `Obstacle` → `Player.TakeHit(damage)` → 무적이면 무시 → `StateMachine.ProcessHit(damage)` → 남은 데미지만 `stat.TakeDamage`.
- **아이템 줍기**: `PowerUpItem : InteractionObject`에 `PlayerFormSO form` / `PlayerEffectSO effect` 필드. 닿으면 적용 후 비활성화. 새 아이템 = 상태 에셋(필요하면 상속 클래스) → 프리팹에 이 컴포넌트 → `ObjectType` 추가 → `ObjectData.asset` 등록 → Pattern Editor 배치.
- **최대 HP 변경 시 현재 HP (결정: A)**: 늘어나면 늘어난 만큼 HP도 더하고, 줄어들면 최대치로 자름.
- `Stat<T>`는 HP만 담당(무적/쿨다운 제거). `HP` setter 이벤트 순서 버그(대입 전 호출) 수정.

## Phase 1 — 상태머신 코어 (신규, UTF-8, `Assets/02. Scripts/Player/State/`)
- [x] `PlayerStats.cs`: 구조체 + `static PlayerStats Default`(현재 Player 값: scale 0.6, maxHP 100, 배율 1, invincible false).
- [x] `PlayerStateSO.cs`: 추상 베이스, 훅 5개(`OnEnter/OnExit/OnUpdate/OnHit/ModifyStats`).
- [x] `PlayerFormSO.cs`: `[CreateAssetMenu]`, `PlayerStats stats`, `ModifyStats`에서 덮어쓰기.
- [x] `PlayerEffectSO.cs`: `[CreateAssetMenu]`, `duration`, 배율 필드(`scaleMultiplier`, `mapSpeedMultiplier`, `jumpHeightMultiplier`, `airTimeMultiplier`, `hpDecrementMultiplier`), `invincible`, `ModifyStats`에서 곱/OR.
  - 메모: 계획 외로 `maxHPMultiplier`도 추가(형태 스탯 중 효과가 못 바꾸는 값이 없도록).
- [x] `PlayerStateMachine.cs`: `ChangeForm`, `AddEffect`, `RemoveEffect`, `HasEffect`, `Tick`, `ProcessHit`, `Stats`, `onStatsChanged`. 효과 시간 만료 시 `OnExit` 후 제거. 스탯은 변경 시에만 재계산.
  - 메모: 훅 안에서 효과 추가/제거·형태 전환이 일어나도 안전하게 순회는 복사본으로. UI/디버그용 `GetRemainingTime` 추가. `RecalculateStats`는 public — 상태가 런타임에 스탯을 바꿨을 때 직접 호출.
- [x] MCP: 컴파일 에러 0.

## Phase 2 — Player 적용 (`Player.cs`, CP949 유지)
- [x] `Stat<T>`: 무적/쿨다운 필드·코루틴·이벤트(`onInvincibleStart/End`) 제거, `TakeDamage`의 무적 체크 제거, `HP` setter에서 대입 후 `onHPChanged` 호출. `SetMaxHP`에 결정 A 반영(증가분만큼 HP 추가).
  - 메모: `SetMaxHP`의 HP 반영도 setter를 거치므로 `onHPChanged` → 뷰 전환 판정이 바로 갱신됨.
- [x] `Player`: `[SerializeField] PlayerFormSO defaultForm` 추가. `maxHP`, `invincibleDuration`, `invincibleCooldown`, `invincibleSpeed`, `originSpeed` 제거(값은 `defaultForm`이 가짐).
- [x] `Player.StateMachine`, `IsInvincible => StateMachine.Stats.invincible`, `baseMapSpeed`(Start 시 `GameManager.mapSpeed` 캡처).
- [x] `Player.TakeHit(float damage)`: 무적이면 무시 → `ProcessHit` → 남은 데미지 `stat.TakeDamage`.
- [x] `Player.ApplyStats()` (`onStatsChanged`): `localScale`, `stat.SetMaxHP`, `GameManager.mapSpeed = baseMapSpeed × mapSpeedMultiplier`.
- [x] `Update`: `StateMachine.Tick(Time.deltaTime)`, HP 감소 = `hpDecrement × hpDecrementMultiplier`(무적이면 0).
- [x] 점프: 높이 `jumpCurve.Evaluate(t) × jumpHeightMultiplier`, 체공시간 `airTime × airTimeMultiplier`(`PlayerAnimator.Jump`에도 같은 값).
- [x] MCP: 컴파일 에러 0.

## Phase 3 — 외부 사용처
- [x] `Obstacle.cs`: `player.stat.isInvincible` → `player.IsInvincible`, `player.stat.TakeDamage` → `player.TakeHit`.
- [x] `PowerUpItem.cs` 신규 (`Assets/02. Scripts/Obstacles/`, UTF-8): `HealObject`와 같은 이동/상하단 판정, 닿으면 `form`/`effect` 적용 + SFX + 비활성화.
- [x] MCP: 컴파일 에러 0.

## Phase 4 — 데이터
- [x] `Assets/11. Datas/PlayerState/Form_Normal.asset`: 현재 Player 값(scale 0.6, maxHP 100, 배율 1).
- [x] `SampleScene` Player에 `defaultForm` 연결, 씬 저장. 옛 직렬화 키 정리 확인.
  - 메모: 씬 diff: `maxHP/invincibleDuration/invincibleCooldown/invincibleSpeed` 4줄 삭제, `defaultForm` 1줄 추가.

## Phase 5 — 검증 (eval 위주, 임시 SO 사용 후 정리)
- [x] 회귀: 기본 형태에서 크기 0.6, 최대 HP 100, mapSpeed 9 그대로. 최대 HP 도달해도 무적 안 걸림. 장애물 피격 데미지 그대로.
  - 메모: 기본 형태 Form_Normal, 크기 0.6, 최대 HP 100, mapSpeed 9. 최대 HP 회복 후에도 무적 아님. `TakeHit(10)` → HP 90.
- [x] 형태 전환: 임시 형태(scale/maxHP 다름) 적용 → 크기·최대 HP 반영, HP는 결정 A대로. 기본으로 복귀 시 원래대로.
  - 메모: 임시 형태(scale 0.9, maxHP 150): HP 90 → 140/150, 기본 복귀 시 100/100·0.6, 뷰 3D 유지.
- [x] 효과: 임시 효과 2개 중첩 시 배율 곱, 같은 효과 재적용 시 시간 갱신, 만료 시 자동 제거·스탯 복구. invincible 효과 중 `TakeHit` 무시.
  - 메모: ×2(1s) + ×1.5·scale×2(3s) → mapSpeed 27, scale 1.2. 0.5s 후 재적용 → 남은 시간 1로 갱신. 만료 순서대로 13.5 → 9, scale 0.6 복구. 무적 효과 중 피격 무시, duration 0은 Tick(100s) 후에도 유지.
- [x] `OnHit` 오버라이드: 테스트용 상속 SO로 데미지 변경/형태 전환이 되는지 확인.
  - 메모: 임시 `TempHitTestForm`(데미지 절반 / 피격 시 다른 형태로 전환하고 데미지 0)으로 확인 후 파일 삭제. 형태 전환 케이스는 최대 HP 150→100 축소로 HP가 잘린 것을 처음에 데미지로 잘못 셌음 → `onTakeDamaged` 호출 0회로 재확인.
- [x] `HP` setter 수정 후 HPBar/2D·3D 전환 정상.
  - 메모: `onHPChanged` 리스너가 변경 후 HP(90)를 받는 것 확인.
- [x] 콘솔 신규 에러/경고 0. 플레이 가능하면 실제 플레이 확인.
- [x] `CLAUDE.md` "플레이어 스탯 & 2D/3D 전환" 섹션 갱신(형태/효과 구조, 새 아이템 추가 절차).

## 의문점
- 없음. (테스트용 상속 SO 클래스는 검증 후 삭제해 코드에 남기지 않을 예정)
