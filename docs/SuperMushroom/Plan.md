# SuperMushroom — Plan

## 구현 현황 (2026-10-19)
- **완료**: Phase 1~7 (2차 크기 변경 연출 포함).
- **미완료**: Phase 7의 "2D에서 먹었을 때 연출 후 3D 전환" 실측 (구조상 문제 없음, 직접 플레이로 확인 필요).
- 컴파일 에러 0. 콘솔에 새로 생긴 에러/경고 없음 (FBX `MaterialLocation.External`, 배경 Missing Script, `Super_Mushroom.fbx` self-intersecting polygon 경고는 작업 전부터 있던 것).
- 계획과 다른 점: 프리팹을 새로 조립하지 않고 `HP.prefab`을 복사해서 만듦 (HP 프리팹에 이미 같은 버섯 모델이 자식으로 들어가 있었음, 아래 Phase 3 메모).

근거: `docs/SuperMushroom/Research.md`

## 범위
- **하는 것**: 슈퍼 버섯 아이템. 먹으면 슈퍼 형태(크기 1.5배)로 전환 + HP 100% 회복. 디버그 3D 패턴으로 테스트 세팅.
- **안 하는 것**: 커지는 연출, 등장 확률 등 밸런스(정식 데이터셋 `CreateObjectData3D/2D` 미수정).

## 확정된 규칙
| 상황 | 동작 |
|---|---|
| 기본 상태에서 먹음 | 슈퍼 형태(scale 0.6 → 0.9) + HP 100% |
| 슈퍼 상태에서 먹음 | HP 100%만 (형태 그대로 — `ChangeForm`이 같은 형태면 return) |
| 슈퍼 상태 유지 | 시간 제한 없음 |
| 슈퍼 상태에서 피격 | **데미지 없이** 기본 형태(0.6)로 복귀 |
| 2D에서 먹음 | HP 100% → 3D 복귀 + BGM 전환 + 레인 오브젝트 회수 (의도한 동작) |

## Phase 1 — 코드
- [x] `Assets/02. Scripts/Player/State/SuperFormSO.cs` 신규 — `OnHit`에서 `ChangeForm(player.DefaultForm)` 후 0 반환
- [x] `Player.cs`: `public PlayerFormSO DefaultForm => defaultForm;` 추가 (sed로 한 줄 삽입, 기존 비-UTF-8 인코딩 유지)
- [x] `PowerUpItem.cs`: `fullHeal` 필드 + 형태/효과 적용 뒤 `stat.Heal(stat.MaxHP)`
- [x] `InteractionObject.cs`: `ObjectType` 맨 끝에 `SUPER_MUSHROOM`(= 14) 추가
- [x] `PatternEditorWindow.GetColor`: `SUPER_MUSHROOM` → `(0.9, 0.25, 0.1)`
- [x] MCP: 컴파일 성공, 에러 0

## Phase 2 — 데이터
- [x] `Assets/11. Datas/PlayerState/Form_Super.asset` (`SuperFormSO`): `Form_Normal` 값 복사, `scale 0.9`

## Phase 3 — 프리팹
- [x] `Assets/07. Prefabs/Prefab_Item/SUPER_MUSHROOM.prefab`
  - `HP.prefab`을 복사한 뒤 `HealObject`와 `HP_Sphere`를 제거하고 `PowerUpItem` 추가(`form = Form_Super`, `fullHeal = true`, `pickupSFX` = HP의 healSFX).
  - 메모: `HP.prefab`에는 이미 `Super_Mushroom.fbx`(옛 red_mushroom fbx와 같은 GUID) 모델이 구체와 함께 자식으로 들어가 있었음. BoxCollider/Rigidbody/모델 배치를 그대로 재사용. **HP.prefab 자체는 수정하지 않음.**
- [x] `ObjectData.asset`에 `SUPER_MUSHROOM`(14) → 프리팹 등록
- [x] MCP: 프리팹 컴포넌트 `Transform, BoxCollider, Rigidbody, PowerUpItem`, Missing 없음

## Phase 4 — 디버그 패턴 배치
- [x] `Patterns/3D/Pattern3D_24.asset` 신규: 1행, 가운데 레인 lower에 `SUPER_MUSHROOM`만
- [x] `DebugCreateObject3D.asset`: 점박문어 패턴(`Pattern3D_23`) → `Pattern3D_24`로 교체 (확률 50 그대로)
  - 디버그 후 원래대로 되돌리려면 이 항목의 패턴을 다시 `Pattern3D_23`으로 바꾸면 됨

## Phase 5 — 검증 (플레이 모드 + eval)
- [x] 기본 상태에서 먹음(HP 80): scale 0.9, HP 100/100, Form_Super, 아이템 비활성화
- [x] HP 70에서 다시 먹음(슈퍼): HP 100/100, 형태 유지
- [x] 슈퍼 상태 피격(TakeHit 30): scale 0.6, Form_Normal, HP 변화 0
- [x] 기본 상태 피격(TakeHit 10): HP -10 (기존 동작 그대로)
- [x] 디버그 패턴으로 `SUPER_MUSHROOM(Clone)`이 레인 1에 스폰되는 것 확인
- [x] 게임뷰 캡처: 커진 마리오가 땅에 붙어 있고 버섯 모델 정상 표시 (캡처 파일 삭제함)
- [x] 콘솔 신규 에러/경고 없음

---

# 2차: 크기 변경 연출 — Plan

근거: `Research.md`의 "2차: 커지는 연출" 섹션

## 범위
- **하는 것**: 크기가 **바뀔 때(커질 때 + 작아질 때)** 시간을 멈추고(`Time.timeScale = 0`), 이전 크기와 새 크기 2단계를 번갈아 보여준 뒤 새 크기로 끝나는 연출.
- **안 하는 것**: 연출 전용 SFX, BGM 정지(연출 중 BGM은 계속 재생).

## 설계 요약
크기를 실제로 바꾸는 유일한 곳인 `Player.ApplyStats`에서, 새 크기가 현재와 다르면 즉시 바꾸지 않고 연출 코루틴을 시작한다. 버섯 외에 이후 크기를 바꾸는 아이템도 같은 연출을 쓴다.

```csharp
// Player.cs
[Header("Resize")]
[SerializeField] float resizeStepTime = 0.07f;   // 한 단계 표시 시간(실제 시간)
static readonly float[] resizeSteps = { 1f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f }; // 0 = 이전, 1 = 새 크기
private Coroutine resizeCor;

// ApplyStats
if (resizeCor == null && !Mathf.Approximately(stats.scale, transform.localScale.x))
    resizeCor = StartCoroutine(ResizeCoroutine(transform.localScale.x, stats.scale));

// ResizeCoroutine: timeScale 0 → resizeSteps대로 Lerp(from, to, t), WaitForSecondsRealtime
//                 → StateMachine.Stats.scale로 마무리 → timeScale 복원

// Update: 연출 중엔 Move() 생략
```
- 총 연출 시간 = 11단계 × 0.07초 ≈ 0.77초 (`resizeStepTime`으로 조절).
- 연출 도중 코루틴을 멈추고 다시 시작하면 `prevTimeScale`이 0으로 기록돼 시간이 영영 멈추므로, 연출 중에는 재시작하지 않고 끝날 때 최신 스탯 크기로 맞춘다.

## Phase 6 — 코드
- [x] `Player.cs`: `resizeStepTime`, `resizeSteps`, `resizeCor`, `ResizeCoroutine` 추가 / `ApplyStats`에서 크기가 다르면 연출 시작 / `Update`에서 연출 중 `Move()` 생략
  - 메모: 파일을 CP949로 디코드→재인코드해서 편집. 기존 바이트는 그대로 유지(왕복 일치 확인), 새 한글 주석도 CP949로 저장.
  - 메모: 계획의 "커질 때만" 대신 피드백대로 "크기가 다를 때"로 조건 변경, 이름도 Grow → Resize.
- [x] MCP: 컴파일 성공, 에러/경고 0

## Phase 7 — 검증 (플레이 모드, `set_autotick`으로 백그라운드 틱 활성화)
- [x] 먹음: 직후 `timeScale 0`, 연출 코루틴 실행 중 → 약 1초 뒤 `timeScale 1`, scale 0.9
- [x] 슈퍼 상태 피격: 직후 `timeScale 0`, HP 변화 0, Form_Normal → 연출 후 scale 0.6
- [x] 연출 중 월드/HP 정지: `timeScale 0`이면 `deltaTime`·물리가 0이 되는 구조(Research 표)로 확인. 실측은 샘플 간격 때문에 연출 중 두 번 찍지 못함
- [x] 연출 중 입력 무시: 코드 확인(`resizeCor != null`이면 `Move()` 생략). 키 입력 시뮬레이션은 하지 않음
- [x] 슈퍼 상태에서 또 먹음: 크기가 같아 연출 없음 (조건상 `Approximately` → 시작 안 함)
- [ ] 2D에서 먹었을 때 연출 후 3D 전환: 별도 실측 안 함. 카메라 전환 코루틴/Cinemachine은 scaled time이라 연출 후 이어서 진행되는 구조
- [x] 콘솔 신규 에러/경고 없음 (eval 타임아웃 로그 2건은 autotick 켜기 전 MCP 요청이 프레임 정지로 타임아웃된 것, 게임 코드 무관)
