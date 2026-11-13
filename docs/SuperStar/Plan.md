# SuperStar — Plan

## 구현 현황 (2026-10-19)
- **완료**: Phase 1~5 전체.
- **미완료**: 없음. 다만 실측하지 못한 항목 2개: Super 형태에서 별 효과 중 충돌(코드로 확인), 3D 화면에서 별 아이템 모델과 튕김 모습 캡처(데이터로 확인).
- 컴파일 에러 0, 새 경고 없음.
- 계획과 다른 점:
  - `Player.cs`에 `OnDestroy`를 추가해 인스턴스 머티리얼을 정리함 (Plan에서는 선택 사항이었음)
  - 프리팹 모델 크기를 0.5로 맞춤 (씬 원본은 약 0.98 크기, 버섯은 0.49)
  - 디버그 패턴의 장애물은 `BARREL` 타입인데, 실제 프리팹은 `Gm_Obstacle_Goomba`(굼바)임 (`ObjectData` 매핑)
  - 씬 저장 후 SampleScene git diff 없음 (작업 전 dirty 원인이 SuperStar뿐이었음)

근거: `docs/SuperStar/Research.md`

## 범위
- **하는 것**: 슈퍼스타 아이템. 먹으면 5초 동안 무지개색 발광, 무적, 맵 속도 증가. 무적 중 부딪힌 `Obstacle`은 레인 방향으로 튕겨 나간 뒤 사라진다. 디버그 3D 패턴으로 테스트 세팅.
- **안 하는 것**: 정식 데이터셋(`CreateObjectData3D/2D`) 등장 확률 조정, 별 전용 BGM, 적 처치 점수.

## 확정된 규칙
| 상황 | 동작 |
|---|---|
| 먹음 | `Effect_Star` 추가: 5초, 무적, 맵 속도 ×1.5 |
| 별 효과 중 또 먹음 | 남은 시간만 5초로 갱신 (`AddEffect` 기존 동작) |
| 효과 중 | HP 자연 감소 정지, 피격 무시, 형태(Super/Mini) 유지 (기존 무적 처리) |
| 효과 중 `Obstacle`과 부딪힘 | 데미지/글리치/셰이크 없이 튕겨 나감 |
| 튕기는 방향 (3D) | 레인 0(왼쪽) → 왼쪽 위로, 레인 2(오른쪽) → 오른쪽 위로, 레인 1(가운데) → 위로 솟았다가 아래로 떨어짐 (X 이동 없음) |
| 튕기는 방향 (2D) | 왼쪽/가운데/오른쪽 3방향 중 랜덤 |
| 튕긴 뒤 | Z는 계속 월드와 같이 흘러오고, `knockDuration`(기본 0.5초, 인스펙터에서 수정) 후 비활성화 |
| 튕길 때 효과음 | `knockSFX` 필드 추가, 지금은 비워 둠 (null이면 재생 안 함) |
| 종료 예고 | 남은 시간 `blinkTime`(기본 1초) 이하이면 무지개 발광이 `blinkInterval`(기본 0.1초) 간격으로 켜졌다 꺼짐 |
| Whomp / Lakitu | 판정이 없는 오브젝트라 대상 아님 (기존 동작 유지) |
| 효과 종료 | 발광 끔(발광 색 검정), 스탯 원복 |

## 설계 요약

### 1) 효과: 코드 없이 데이터 + 발광 훅만
```csharp
// StarEffectSO.cs (신규, UTF-8)
[CreateAssetMenu(fileName = "Effect_", menuName = "Scriptable Objects/Player/Star Effect")]
public class StarEffectSO : PlayerEffectSO
{
    [Tooltip("무지개 한 바퀴 도는 시간(초)")]
    public float rainbowPeriod = 0.5f;
    [Tooltip("발광 세기 (HDR 배율)")]
    public float glowIntensity = 2f;
    [Tooltip("남은 시간이 이 값(초) 이하가 되면 깜빡임")]
    public float blinkTime = 1f;
    [Tooltip("깜빡임 켜짐/꺼짐 한 단계 시간(초)")]
    public float blinkInterval = 0.1f;

    public override void OnUpdate(Player player, float deltaTime)
    {
        // 종료 예고: 남은 시간이 blinkTime 이하면 발광을 번갈아 끔
        bool off = player.StateMachine.GetRemainingTime(this) <= blinkTime
            && Mathf.Repeat(Time.time, blinkInterval * 2f) >= blinkInterval;
        player.SetGlow(off ? Color.black
            : Color.HSVToRGB(Mathf.Repeat(Time.time / rainbowPeriod, 1f), 1f, 1f) * glowIntensity);
    }

    public override void OnExit(Player player) => player.SetGlow(Color.black);
}
```
무적과 맵 속도는 기존 `PlayerEffectSO` 필드(`invincible`, `mapSpeedMultiplier`, `duration`)가 처리한다.

### 2) Player 발광 (런타임 머티리얼 인스턴스)
```csharp
// Player.cs
private Material[] glowMaterials;
public void SetGlow(Color color)
{
    // 첫 호출 때 자식 렌더러 머티리얼을 인스턴스화하고 _EMISSION 키워드를 켬 (에셋은 건드리지 않음)
    glowMaterials ??= GetComponentsInChildren<Renderer>(true).SelectMany(r => r.materials)
        .Where(m => m.HasProperty(EmissionColorID)).ToArray();
    foreach (var m in glowMaterials) { m.EnableKeyword("_EMISSION"); m.SetColor(EmissionColorID, color); }
}
```
- 인스턴스 머티리얼은 Player가 씬 재로드 때 파괴되면서 같이 정리된다(Player는 DDOL이 아님). 누수가 걱정되면 `OnDestroy`에서 Destroy를 추가한다.

### 3) Obstacle 튕겨내기
```csharp
// Obstacle.cs
[Header("Knockback (무적 플레이어에게 부딪혔을 때)")]
[SerializeField] float knockSideSpeed = 6f, knockUpSpeed = 8f, knockGravity = 25f, knockSpin = 720f;
[Tooltip("튕긴 뒤 사라질 때까지 시간(초)")]
[SerializeField] float knockDuration = 0.5f;
[Tooltip("튕길 때 효과음 (비워 두면 재생 안 함)")]
public AudioClip knockSFX;
private bool knocked; private Vector3 knockVelocity; private float knockElapsed;

protected override void OnEnable() { base.OnEnable(); knocked = false; }

void FixedUpdate() { if (knocked) KnockMovement(); else ObstacleMovement(); }

// OnCollded: if (!hit || knocked) return;  if (player.IsInvincible) { Knock(player); return; }

void Knock(Player player)
{
    // 3D: 레인 위치로 방향 결정 (-1 왼쪽, 0 가운데, 1 오른쪽). 2D: 3방향 중 랜덤
    int dir = GameManager.Instance.currentViewMode == ViewMode.View2D
        ? Random.Range(-1, 2)
        : Math.Sign(currentLane - (GameManager.Instance.laneLength - 1) / 2);
    knockVelocity = new Vector3(dir * knockSideSpeed, knockUpSpeed, 0f);
    knocked = true; knockElapsed = 0f;
    if (knockSFX != null)
        AudioSource.PlayClipAtPoint(knockSFX, player.transform.position, 0.5f);
}

void KnockMovement()
{
    // 포물선 + 월드 스크롤 + 회전. 시간이 지나면 풀로 반환
    knockVelocity.y -= knockGravity * Time.fixedDeltaTime;
    Rb.MovePosition(transform.position + (knockVelocity + Vector3.back * GameManager.Instance.mapSpeed) * Time.fixedDeltaTime);
    Rb.MoveRotation(Rb.rotation * Quaternion.Euler(0f, 0f, knockSpin * Time.fixedDeltaTime * -Mathf.Sign(knockVelocity.x == 0 ? 1 : knockVelocity.x)));
    if ((knockElapsed += Time.fixedDeltaTime) >= knockDuration) gameObject.SetActive(false);
}
```
- 서브클래스의 `ObstacleMovement` override(SpikeBall 비행, StoneFragment 상승)보다 먼저 분기하므로, 비행 중이거나 솟는 중에 맞아도 튕겨 나간다.
- 루트 회전은 풀에서 다시 꺼낼 때 `Quaternion.identity`로 초기화된다(Research 1).
- 가운데 레인: X 속도 0, 위로 솟았다가 떨어진다. 기본값(0.5초, 상승 8, 중력 25)이면 높이는 정점 약 +1.3을 거쳐 사라질 때 약 +0.9, 옆 이동은 약 3유닛(레인 3칸 거리)이다. 더 높이/멀리 날리려면 인스펙터에서 속도를 조절한다.

## Phase 1 — 코드
- [x] `Assets/02. Scripts/Player/State/StarEffectSO.cs` 신규: `OnUpdate`에서 무지개 발광 + 종료 전 `blinkTime` 동안 깜빡임, `OnExit`에서 끄기
- [x] `Player.cs`: `SetGlow(Color)` + `glowMaterials` 캐시 + `EmissionColorID` 추가 (CP949 유지: 디코드 → 편집 → 재인코드, 기존 바이트 왕복 일치 확인)
- [x] `Obstacle.cs`: 튕겨내기 필드(`knockDuration` 0.5 등 인스펙터 노출, `knockSFX` 비워 둠)/상태, `OnEnable` override, `FixedUpdate` 분기, `OnCollded` 무적 분기 → `Knock()`(3D 레인 방향 / 2D 랜덤), `KnockMovement()`
- [x] `InteractionObject.cs`: `ObjectType` 끝에 `SUPER_STAR`(= 16) 추가 (CP949 유지)
- [x] `PatternEditorWindow.GetColor`: `SUPER_STAR` → 노랑 `(1, 0.85, 0.1)`
- [x] MCP: 컴파일 대기 → 콘솔 에러/경고 확인

## Phase 2 — 데이터
- [x] `Assets/11. Datas/PlayerState/Effect_Star.asset` (`StarEffectSO`): `duration 5`, `invincible true`, `mapSpeedMultiplier 1.5`, 나머지 배율 1

## Phase 3 — 프리팹
- [x] `Assets/07. Prefabs/Prefab_Item/SUPER_STAR.prefab`: `SUPER_MUSHROOM.prefab`을 복사 → 버섯 모델 자식을 SuperStar 모델 자식으로 교체(`SuperStar.fbx` 메시 + `M_SuperStar.mat`, 씬 오브젝트의 회전 (270,0,0) 사용, 크기는 버섯과 비슷하게 맞춤) → `PowerUpItem`: `form = None`, `effect = Effect_Star`, `fullHeal = false`, `scoreInsteadForm = None`, `pickupSFX` 유지
- [x] `ObjectData.asset`에 `SUPER_STAR` → 프리팹 등록
- [x] MCP: 프리팹 컴포넌트 확인, Missing 없음
- [x] 프리팹 확인 후 씬의 `SuperStar` 오브젝트 삭제 → `SampleScene` 저장
  - 씬이 작업 전부터 dirty였으므로 저장하면 다른 미저장 변경도 같이 저장됨 (사용자 확인함)

## Phase 4 — 디버그 패턴
- [x] `Patterns/3D/Pattern3D_26.asset` 신규: 1행 가운데 lower `SUPER_STAR` → 다음 행부터 3레인 모두 lower에 장애물(BARREL 등) 2~3행
- [x] `DebugCreateObject3D.asset`: `Pattern3D_25` → `Pattern3D_26`으로 교체 (되돌릴 때는 다시 `Pattern3D_25`)

## Phase 5 — 검증 (플레이 모드 + eval, `set_autotick`)
- [x] 먹음: `IsInvincible` true, `mapSpeed` 9 → 13.5 (×1.5), 마리오 머티리얼 인스턴스 `_EMISSION` 켜짐, `_EmissionColor`가 샘플마다 다른 색상(파랑→분홍→초록→주황)
  - 디버그 패턴의 별을 가운데 레인에서 실제로 먹어 남은 시간이 5초로 갱신되는 것도 확인
- [x] 종료 전 깜빡임: eval 왕복이 약 0.85초라 1초 구간에선 샘플이 1~2개뿐이어서, `blinkTime`을 잠시 4.5로 올려 확인 → 남은 시간 4.07~0.73 구간에서 ON/OFF 교대. 확인 후 에셋 값 1로 복원
- [x] 5초 후: 효과 제거, 무적 false, `mapSpeed` 13.5 → 9, 발광 검정 `(0,0,0)`
- [x] 효과 중 HP 변화 없음: 장애물과 여러 번 부딪히는 동안 HP 86.21 유지(자연 감소도 멈춤)
- [x] 3D 실제 충돌: 레인 0 → `vel.x -6`, 레인 1 → `vel.x 0`(위로 솟음), 레인 2 → `vel.x +6`. 튕긴 오브젝트가 회전하며 날아가고 다음 샘플(약 0.85초 뒤)에서는 비활성화돼 있음. 플레이어 데미지 0
- [x] 2D: 뷰 모드를 2D로 바꾸고 `Knock`을 300회 호출 → x -6 / 0 / +6 = 107 / 102 / 91. 2D 실제 충돌도 확인(가운데 방향이 나옴)
- [x] `knockSFX`가 비어 있어도 에러 없음 (모든 충돌이 null 상태에서 진행됨)
- [x] 풀 재사용 초기화: 튕겼다가 다시 스폰된 장애물이 회전 `(0,0,0)`, `knocked=False`로 정상 이동
  - 메모: `knockVelocity`에는 이전 값이 남지만, `Knock()`에서 매번 다시 설정하므로 영향 없음
- [x] Super 형태 유지: 무적이면 `Knock` 후 바로 return해서 `TakeHit`/`ProcessHit`이 호출되지 않음 (코드 확인, Super 형태로 실측은 안 함)
- [x] 게임뷰 캡처: 발광 색이 프레임마다 바뀌는 것 확인(캡처 2장, 파일과 `.meta` 삭제). 캡처 시점이 2D 뷰여서 별 아이템 모델과 튕김 모습은 화면으로 확인하지 못함 → 프리팹 bounds(0.49×0.48×0.23, 중심 높이 0.31, 버섯과 같은 크기와 높이)로 대신 확인
  - 메모: 발광 세기 2에서는 텍스처가 거의 단색으로 덮임. 너무 강하면 `Effect_Star.glowIntensity`를 낮추면 됨
- [x] 콘솔: 컴파일 에러 0. 경고는 배경 프리팹 Missing Script와 FBX `MaterialLocation.External`뿐(작업 전부터 있던 것). 플레이 중 에디터 에러 3건(`SerializedObjectNotCreatableException`, `MissingReferenceException m_Targets`)은 삭제한 씬 SuperStar를 인스펙터가 계속 선택하고 있어서 난 에디터 UI 에러였고, 선택을 해제한 뒤 사라짐. 게임 코드와 무관

## 확정된 사항 (피드백)
- 씬 `SuperStar`는 프리팹 생성 후 삭제하고 씬 저장
- 2D 튕김 방향은 왼쪽/가운데/오른쪽 3방향 중 랜덤
- 종료 예고 깜빡임은 무지개 발광 켜짐/꺼짐 방식
