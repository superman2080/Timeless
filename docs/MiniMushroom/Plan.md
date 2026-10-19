# MiniMushroom — Plan

## 구현 현황 (2026-10-19)
- **완료**: Phase 1~7 전체 + 3차 수정(슈퍼 상태에서 땅콩버섯 → 점수) (2차 수정: 이동 속도 감소 대상을 레인 변경 → 맵 속도로 변경, `moveSpeedMultiplier` 관련 1차 코드는 되돌림).
- **미완료**: 없음.
- 컴파일 에러 0. 콘솔 신규 경고 없음(Missing Script는 원래 있던 것). 씬 오브젝트 삭제 직후 인스펙터 관련 에디터 예외 2건(`SerializedObjectNotCreatableException`, `MissingReferenceException`)은 선택 중인 오브젝트를 지워서 생긴 것 — 게임 코드 무관.
- 계획과 다른 점: `Mini_Mushroom.fbx`의 머티리얼 리맵이 텍스처 없는 `TripoModels/mushroom_3d_model.mat`을 가리키고 있어 `M_Mini_Mushroom.mat`으로 바꿈 (Phase 3 메모).

근거: `docs/MiniMushroom/Research.md`

## 범위
- **하는 것**: 땅콩버섯 아이템(미니 형태), 미니 상태에서 슈퍼버섯 → 점수 보상, 점수 추가 API, 플레이어 이동(레인 변경) 속도 스탯, 디버그 패턴으로 테스트.
- **안 하는 것**: 정식 데이터셋(`CreateObjectData3D/2D`) 등장 확률 밸런스, 전용 SFX.

## 확정된 규칙 (기본값, 인스펙터에서 변경)
| 상황 | 동작 |
|---|---|
| 먹음 (기본/슈퍼 상태) | 미니 형태로 전환 (scale → **0.3**, 기본 0.6의 절반) + 플레이어 이동 속도 감소 + HP를 `ToGlobalRatio(현재 모드, 0.1)`로 **설정** → 3D 55%, 2D 7% (현재 HP보다 높으면 오르는 것도 허용) |
| 이동 속도 감소 | ~~레인 변경 속도~~ → **맵(월드) 흐르는 속도** × `(1 - mapSpeedReduction)`. `[Range(0, 0.9)]`, 기본 0.5. 좌우 레인 변경 속도는 그대로 (2차 수정 참고) |
| 미니 상태에서 피격 | **데미지 없이** 기본 형태로 복귀 (슈퍼버섯과 동일) |
| 미니 상태에서 슈퍼버섯 | 형태/회복 없음, 점수 **+1000**, 점수 강조 연출, 아이템 사라짐 |
| 미니 상태에서 땅콩버섯 | 변화 없음 (형태가 같아 무시) |
| 시간 제한 | 없음 (피격 전까지 유지) |

## 설계 요약
```csharp
// PlayerStats.cs - 항목 추가 (Default 1)
[Tooltip("플레이어 이동(레인 변경) 속도 배율")]
public float moveSpeedMultiplier;

// Player.cs - ChangeLane: 레인 변경 시간을 배율로 나눔
changeLaneCor = StartCoroutine(ChangeLaneCoroutine(laneIndex, 0.1f / StateMachine.Stats.moveSpeedMultiplier));

// Player.cs - Stat<T>
public void SetHP(float hp) => HP = hp;

// MiniFormSO.cs (신규)
public class MiniFormSO : PlayerFormSO
{
    [Tooltip("먹을 때 남길 HP: 현재 뷰 모드 HP 구간 안의 비율 (0.1 → 3D 55%, 2D 7%)")]
    [Range(0, 1)] public float hpRatioInView = 0.1f;
    [Tooltip("플레이어 이동 속도 감소 비율 (0.5 → 절반 속도)")]
    [Range(0, 0.9f)] public float moveSpeedReduction = 0.5f;

    public override void OnEnter(Player player)
    {
        var mode = GameManager.Instance.currentViewMode;
        float ratio = GameManager.Instance.laneManager.patternSet.ToGlobalRatio(mode, hpRatioInView);
        player.stat.SetHP(player.stat.MaxHP * ratio);
    }

    public override void ModifyStats(ref PlayerStats target)
    {
        base.ModifyStats(ref target);
        target.moveSpeedMultiplier = 1f - moveSpeedReduction;
    }

    public override float OnHit(Player player, float damage)
    {
        player.StateMachine.ChangeForm(player.DefaultForm);
        return 0f;
    }
}

// ScoreUI.cs
public void AddScore(int amount) { score += amount; punchTimer = punchDuration; }

// PowerUpItem.cs
[Tooltip("플레이어가 이 형태일 때 먹으면 효과 대신 점수")]
[SerializeField] PlayerFormSO scoreInsteadForm;
[SerializeField, Min(0)] int scoreInstead = 1000;
// OnCollided: CurrentForm == scoreInsteadForm이면 FindAnyObjectByType<ScoreUI>()?.AddScore(scoreInstead) 후 효과 생략
```
- `PlayerStats`에 항목을 추가하면 기존 `Form_Normal`/`Form_Super` 에셋에는 값이 없음 → 필드 초기값(`PlayerStats.Default`, 1)으로 채워지는지 MCP로 확인하고, 0이면 에셋에 1을 명시.

## Phase 1 — 코드
- [x] `Player/State/PlayerStats.cs`: `moveSpeedMultiplier` 추가, `Default`에 1
- [x] `Player/State/PlayerEffectSO.cs`: `moveSpeedMultiplier` 배율 추가 (다른 스탯과 동일하게 곱함)
- [x] `Player/Player.cs`: `Stat<T>.SetHP` 추가, `ChangeLane`에 이동 속도 배율 적용 (CP949 유지)
  - 메모: ASCII 코드만 sed로 삽입해 기존 CP949 바이트 유지 (iconv 디코드 확인)
- [x] `Player/State/MiniFormSO.cs` 신규 (위 설계)
- [x] `UI/ScoreUI.cs`: `AddScore` 추가
- [x] `Obstacles/PowerUpItem.cs`: `scoreInsteadForm`, `scoreInstead` + 분기
- [x] `Obstacles/InteractionObject.cs`: `ObjectType` 끝에 `MINI_MUSHROOM`(= 15) (CP949 유지)
- [x] `Editor/PatternEditorWindow.GetColor`: `MINI_MUSHROOM` → 파랑 `(0.2, 0.45, 0.95)`
- [x] MCP: 컴파일 성공, 에러 0. `Form_Normal`/`Form_Super`의 `moveSpeedMultiplier`가 1인지 확인
  - 메모: 에셋에 값이 없어도 필드 초기값(`PlayerStats.Default`)으로 1이 들어옴 → 에셋 수정 안 함

## Phase 2 — 데이터
- [x] `Assets/11. Datas/PlayerState/Form_Mini.asset` (`MiniFormSO`): Normal 값 복사, `scale 0.3`, `hpRatioInView 0.1`, `moveSpeedReduction 0.5`

## Phase 3 — 프리팹
- 모델: `Assets/03. Models/Static/Mini_Mushroom.fbx` (머티리얼 `M_Mini_Mushroom` 연결됨). SampleScene에 열려 있는 `Mini_Mushroom` 오브젝트의 회전(X 270°)을 참고.
- [x] `Assets/07. Prefabs/Prefab_Item/MINI_MUSHROOM.prefab`: `SUPER_MUSHROOM.prefab` 복사 → 모델 자식을 `Mini_Mushroom.fbx`로 교체(슈퍼버섯 모델과 비슷한 크기로 맞춤), `PowerUpItem.form = Form_Mini`, `fullHeal = false`
  - 메모: 모델 자식 `Mini_Mushroom`은 슈퍼버섯 모델과 같은 위치/회전(y 0.07, rot 270/180/0), scale 1 — 메시 크기가 거의 같아 그대로 맞음. `objectData.type = 15`
  - 메모: fbx 머티리얼 리맵 `tripo_mat_0d10698e` → `M_Mini_Mushroom.mat`(텍스처 있음)으로 변경
- [x] `SUPER_MUSHROOM.prefab`: `scoreInsteadForm = Form_Mini`, `scoreInstead = 1000`
- [x] `ObjectData.asset`에 `MINI_MUSHROOM`(15) → 프리팹 등록
- [x] MCP: 프리팹 Missing 없음
- [x] 씬의 `Mini_Mushroom` 임시 오브젝트 삭제 (피드백 반영). 디스크의 씬 파일에는 원래 없던 오브젝트라 씬은 저장하지 않음 — 에디터에서 씬이 dirty 상태로 남아 있음

## Phase 4 — 디버그 패턴
- [x] `Patterns/3D/Pattern3D_25.asset` 신규: 1행 땅콩버섯, 다음 행 슈퍼버섯 (가운데 레인 lower)
- [x] `DebugCreateObject3D.asset`: `Pattern3D_24` → `Pattern3D_25` 교체

## Phase 5 — 검증 (플레이 모드 + eval)
- 방법: 일시정지 상태에서 eval로 실제 프리팹을 생성해 `OnCollided`를 직접 호출 (시간 감소/다른 스폰 영향 제거). 2D는 `currentViewMode`를 리플렉션으로 바꿔 테스트 후 복원
- [x] 3D 기본 상태(HP 80)에서 먹음: scale 0.3, HP 55, moveSpeedMultiplier 0.5, mapSpeedMultiplier 1, 뷰 전환 없음
- [x] 3D HP 52%에서 먹음: HP 55로 상승
- [x] 2D(HP 30)에서 먹음: HP 7, 뷰 전환 없음
- [x] 미니 상태에서 또 먹음: 변화 없음
- [x] 미니 상태 피격(TakeHit 10): scale 0.6, Form_Normal, HP 변화 0, moveSpeedMultiplier 1
- [x] 미니 상태에서 슈퍼버섯: 형태/HP 그대로, 점수 +1000(정확히), 아이템 비활성화
- [x] 기본 상태에서 슈퍼버섯: 기존 동작 그대로 (형태 전환 + HP 100%, 점수 +0)
- [x] 슈퍼 상태에서 땅콩버섯: scale 0.9 → 0.3, HP 55
- [x] 게임뷰 캡처로 땅콩버섯 모델 표시 확인: 파란 텍스처, 플레이어 쪽을 바라봄, 슈퍼버섯과 비슷한 크기 (캡처 파일 삭제)
- [x] 콘솔 신규 에러/경고 없음

---

# 2차 수정: 이동 속도 감소 대상 변경 — Plan

피드백: 감소 대상은 좌우 레인 변경이 아니라 **맵이 흘러오는 속도**(`GameManager.mapSpeed`).

## 설계
- 기존 스탯 `mapSpeedMultiplier`(`Player.ApplyStats`가 `mapSpeed = 시작 속도 × 배율`로 반영)를 그대로 사용 → 새 스탯 항목 불필요.
- 1차에서 추가한 `moveSpeedMultiplier`는 다른 곳에서 안 쓰므로 **되돌림**.
```csharp
// MiniFormSO.cs
[Tooltip("맵 이동 속도 감소 비율 (0.5 → 절반 속도)")]
[Range(0, 0.9f)] public float mapSpeedReduction = 0.5f;

public override void ModifyStats(ref PlayerStats target)
{
    base.ModifyStats(ref target);
    target.mapSpeedMultiplier = 1f - mapSpeedReduction;
}
```
- 영향: 패턴 생성 간격은 거리 기준이라 유지됨. HP 시간 감소·점수는 시간 기준이라 그대로(속도만 느려짐). 형태 복귀 시 `ApplyStats`가 `mapSpeed`를 원래대로 되돌림.
- `Form_Mini.asset`의 직렬화 필드 이름이 `moveSpeedReduction` → `mapSpeedReduction`으로 바뀌므로 에셋 값 0.5를 다시 설정.

## Phase 6 — 코드/데이터
- [x] `PlayerStats.cs`: `moveSpeedMultiplier` 항목/Default 제거
- [x] `PlayerEffectSO.cs`: `moveSpeedMultiplier` 필드/곱셈 제거
- [x] `Player.cs`: `ChangeLane`의 `/ StateMachine.Stats.moveSpeedMultiplier` 제거 → `0.1f` 원복 (CP949 유지, `SetHP`는 유지)
- [x] `MiniFormSO.cs`: `moveSpeedReduction` → `mapSpeedReduction`, `ModifyStats`에서 `mapSpeedMultiplier` 설정
- [x] `Form_Mini.asset`: `mapSpeedReduction 0.5`
- [x] MCP: 컴파일 성공, 에러 0

## Phase 7 — 검증
- [x] 미니 먹음: `mapSpeed` 9 → 4.5, 레인 변경 시간 0.1초 그대로(코드 원복 확인)
- [x] 미니 상태 피격: `mapSpeed` 4.5 → 9, 데미지 0
- [x] 1차 검증 항목 재확인: 3D HP 55, HP 52 → 55, 2D HP 7, 미니 중 슈퍼버섯 점수 +1000, 기본 상태 슈퍼버섯 기존 동작, 슈퍼 → 미니 전환
- [x] 콘솔 신규 에러/경고 없음
  - 메모: 에디터 인스펙터 예외(`SerializedObjectNotCreatableException`, `MissingReferenceException`, Transform/Renderer 인스펙터)가 계속 나옴 → 삭제한 씬 오브젝트 `Mini_Mushroom`을 인스펙터가 아직 표시 중이라서 생김(잠긴 인스펙터 가능성). 게임 코드 무관, 인스펙터에서 다른 오브젝트를 선택하면 사라짐

---

# 3차 수정: 슈퍼 상태에서 땅콩버섯 → 점수

피드백: 슈퍼 상태에서 땅콩버섯을 먹으면 미니로 바뀌지 않고 점수 +1000 (미니 상태에서 슈퍼버섯과 대칭).

- 코드 변경 없음 — 1차에서 만든 `PowerUpItem.scoreInsteadForm`/`scoreInstead`를 땅콩버섯 프리팹에도 설정.
- [x] `MINI_MUSHROOM.prefab`: `scoreInsteadForm = Form_Super`, `scoreInstead = 1000`
- [x] 검증(플레이 모드): 슈퍼 상태에서 땅콩버섯 → Form_Super/scale 0.9/HP 100 유지, 점수 +1000. 나머지 항목도 그대로 통과
  - 메모: 검증 시점 `Form_Mini`가 `hpRatioInView 0.5`, `mapSpeedReduction 0.2`로 바뀌어 있었음(사용자 조정으로 보고 유지). 결과도 이 값과 일치: 3D HP 75, 2D HP 35, mapSpeed 9 → 7.2
- [x] 콘솔 신규 에러/경고 없음 (인스펙터 예외는 2차 메모와 같은 원인)
