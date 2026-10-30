# PSwitch — Plan

## 구현 현황 (2026-10-30)
- **완료**: Phase 1~5 전체.
- **미완료**: 없음.
- 컴파일 에러 0. 콘솔 신규 게임 에러/경고 없음 (MCP 요청 타임아웃 에러 1건은 `com.unity.pipeline` 서버 쪽 로그 — 게임 코드 무관).
- 계획과 다른 점: `ObjectType.P_SWITCH`는 **18** (Research에서 19로 잘못 셈). 모델은 임시 메시 대신 씬의 `PSwitch` 오브젝트(메시 `rusty_parking_bollard_3d_model.fbx`) 사용.
- 참고: 작업 중 `ProjectSettings/UnityConnectSettings.asset`이 에디터에 의해 바뀜(`m_Enabled 0 → 1`) — 이 작업에서 건드린 것 아님.

근거: `docs/PSwitch/Research.md`

## 범위
- **하는 것**: P스위치 효과(뷰 강제 반전 + 10초 전환 잠금), 아이템 프리팹(씬의 `PSwitch` 모델 사용), `ObjectType`/`ObjectData` 등록, 3D·2D 패턴 각 1개, 플레이 모드 검증.
- **안 하는 것**: 남은 시간 UI, 전용 SFX/BGM, 정식 데이터셋 등장 확률 밸런스.

## 규칙 (기본값)
| 상황 | 동작 |
|---|---|
| 먹음 | 현재 뷰의 반대로 즉시 전환 (3D→2D, 2D→3D). 기존 HP 전환과 같은 처리: 카메라 1.5초 + BGM 트리거 + 레인 오브젝트 회수 |
| 10초 동안 | HP가 임계값을 넘어도 뷰 전환 없음. HP 감소/피격/다른 아이템은 그대로 |
| 10초 끝 | 잠금 해제 후 즉시 HP 기준 재검사 → 임계값 밖이면 바로 원래 뷰로 복귀 (예: HP 90%에서 강제 2D → 10초 뒤 3D) |
| 활성 중 다시 먹음 | 시간만 10초로 갱신, 뷰는 그대로 (`AddEffect` 기존 동작) |
| 무적 중 끝남 | HP가 안 변해도 종료 시 재검사하므로 정상 복귀 |

## 설계
잠금은 범용 스탯 플래그로 두고, 뒤집기만 P스위치 전용 SO에서 처리.

```csharp
// PlayerStats.cs
[Tooltip("HP 임계값에 의한 3D/2D 전환 막음")]
public bool lockView;            // Default false

// PlayerEffectSO.cs
public bool lockView;
// ModifyStats: stats.lockView |= lockView;

// Player.cs (CP949 유지)
private bool viewLocked;

// 두 분기의 중복 묶음을 추출. P스위치도 이걸 호출
public void SwitchView(ViewMode mode)
{
    if (mode == GameManager.Instance.currentViewMode) return;
    GameManager.Instance.ChangeViewMode(mode, 1.5f);
    GameManager.Instance.audioPlayer.trigger = true;
    GameManager.Instance.laneManager.RemoveAllLaneObjects();
}

private void CheckChangeView()
{
    if (StateMachine.Stats.lockView) return;
    // 기존 분기 → SwitchView(View2D / View3D)
}

private void ApplyStats(PlayerStats stats)
{
    ...기존
    // 잠금이 풀리는 순간 재검사 (RemoveEffect는 OnExit 뒤에 스탯을 갱신하므로 OnExit에서 하면 아직 잠겨 있음)
    bool unlocked = viewLocked && !stats.lockView;
    viewLocked = stats.lockView;
    if (unlocked) CheckChangeView();
}

// PSwitchEffectSO.cs (신규)
[CreateAssetMenu(fileName = "Effect_", menuName = "Scriptable Objects/Player/P-Switch Effect")]
public class PSwitchEffectSO : PlayerEffectSO
{
    public override void OnEnter(Player player)
    {
        var mode = GameManager.Instance.currentViewMode;
        player.SwitchView(mode == ViewMode.View3D ? ViewMode.View2D : ViewMode.View3D);
    }
}
```
- `OnEnter`는 `RecalculateStats` 전에 불리지만, 뒤집기와 스탯 반영 사이에 HP 변경이 없고 `RecalculateStats`가 `Stats`를 먼저 대입한 뒤 `ApplyStats`를 부르므로 그 안의 `CheckChangeView`는 이미 잠긴 상태로 실행됨.
- `ApplyStats`는 `Start`의 `ChangeForm(defaultForm)`에서 처음 불림 → `viewLocked` 초기값 false라 문제 없음.

## Phase 1 — 코드
- [x] `Player/State/PlayerStats.cs`: `lockView` + Tooltip, `Default`에 false
- [x] `Player/State/PlayerEffectSO.cs`: `lockView` 필드 + `ModifyStats`에 `|=`
- [x] `Player/Player.cs`: `SwitchView` 추출, `CheckChangeView` 잠금 확인, `ApplyStats` 해제 시 재검사 (CP949 유지 — 한글 주석은 CP949로 인코딩해 삽입)
- [x] `Player/State/PSwitchEffectSO.cs` 신규 (UTF-8)
- [x] `Obstacles/InteractionObject.cs`: `ObjectType` 끝에 `P_SWITCH`(= 18, 계획의 19는 오기) — ASCII만 삽입
- [x] `Editor/PatternEditorWindow.GetColor`: `P_SWITCH` → 파랑 계열 `(0.15f, 0.35f, 0.8f)` (땅콩버섯 파랑보다 어둡게)
- [x] MCP: 컴파일 대기 → 콘솔 에러 0, 기존 Form 에셋 `lockView` false 확인
  - 메모: `Player.cs`는 Python으로 cp949 디코드/인코드해 편집 → diff는 변경 줄만. Form 에셋은 필드 없음 → 기본값 false

## Phase 2 — 데이터
- [x] `Assets/11. Datas/PlayerState/Effect_PSwitch.asset` (`PSwitchEffectSO`): `duration 10`, `lockView true`, 나머지 배율 1, `invincible false`

## Phase 3 — 프리팹
- [x] `Assets/07. Prefabs/Prefab_Item/P_SWITCH.prefab`: `MINI_MUSHROOM.prefab` 복사 → 모델 자식을 씬 `PSwitch`의 메시/머티리얼로 교체, `PowerUpItem.form = None`, `effect = Effect_PSwitch`, `fullHeal false`, `scoreInsteadForm = None`, `objectData.type = 18`
  - 메모: 모델 자식 `PSwitch` = MeshFilter/MeshRenderer(`rusty_parking_bollard_3d_model.fbx` 메시 + 머티리얼), 위치는 땅콩버섯과 같은 y 0.07, 회전은 씬 오브젝트 그대로 (270, 180, 0) → P가 플레이어 쪽을 향함. 씬의 `PSwitch`는 저장 안 된 오브젝트라 그대로 둠(씬 저장 안 함)
- [x] `ObjectData.asset`에 `P_SWITCH` → 프리팹 등록 (기존 미커밋 변경 보존)
  - 메모: diff는 기존 미커밋 16, 17 항목 + 새 18 항목만
- [x] MCP: 프리팹 Missing 없음

## Phase 4 — 패턴
- [x] `Patterns/3D/Pattern3D_29.asset`: 1행 가운데 레인 lower `P_SWITCH`
- [x] `Patterns/2D/Pattern2D_05.asset` (`viewMode 2D`): 1행 레인 2 lower `P_SWITCH`
- 데이터셋 등록은 안 함 (Debug 데이터셋에 미커밋 변경이 있어서). 필요하면 사용자 지정에 따라 추가.

## Phase 5 — 검증 (플레이 모드 + eval, 버섯 작업과 같은 방식)
- 방법: 플레이 모드에서 `hpDecrement` 0, 레인 생성 중지, `player.enabled = false`(Update의 재검사 제외 → `ApplyStats` 경로만 확인) 후 프리팹 생성 + `OnCollided` 직접 호출, `StateMachine.Tick`으로 시간 진행. 이후 실제 프레임으로도 확인
- [x] 3D, HP 80%에서 먹음 → 2D 전환, `lockView` true, 10초 동안 3D 복귀 없음 (HP ≥ 70%여도)
- [x] 10초 후 → HP ≥ 70%면 즉시 3D 복귀
- [x] 2D, HP 30%에서 먹음 → 3D 전환, 10초 동안 HP ≤ 50%여도 2D 전환 없음 → 10초 후 2D 복귀
- [x] 강제 3D 중 레인 이동 가능 / 강제 2D 중 마지막 레인 고정
- [x] 활성 중 다시 먹음 → 뷰 그대로, 남은 시간 10초로 갱신
- [x] 무적 상태 + 종료 → 재검사로 복귀
  - 메모: `player.enabled = false`로 HP 변화/Update 없이 만료시켜 같은 조건을 확인 (3D 복귀, 2D 복귀 둘 다 OK)
- [x] (추가) HP가 두 임계값 사이(60%)에서 만료 → 뷰 그대로 (강제 3D 유지)
- [x] (추가) 실제 시간 10초 만료 확인 (Update → Tick 경로)
- [x] 아이템 표시 확인 (캡처 파일 + .meta 삭제)
  - 메모: 게임뷰는 글리치 연출에 가려져 임시 카메라 → RenderTexture로 scratchpad에 저장해 확인. 녹슨 파란 버튼 + 흰 P, 플레이어 쪽을 향함
- [x] 콘솔 신규 에러/경고 없음

## 확정 (피드백)
- 10초 끝 → 즉시 HP 기준 재검사.
- 활성 중 다시 먹음 → 10초로 갱신, 뷰 그대로.
- 남은 시간 표시 → 아직 안 만듦.
- 모델 → 씬의 `PSwitch` 오브젝트 사용.
