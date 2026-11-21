# Bowser(쿠파) 추격 보스 — Plan

근거: `docs/BowserBoss/Research.md`
상태: **구현 완료** (2026-11-20)

## 구현 현황
- **완료**: Phase 1~9 전 항목. 미완료 없음. (Phase 7 = 쿠파 Humanoid 리깅과 모델 교체, Phase 8 = 착지 애니메이션 + Walk 클립 교체, Phase 9 = 모든 상황에서 Walk 사용)
- 컴파일 에러 0. 플레이 중 새로 생긴 에러/경고 0. 기존 Missing Script 경고(prop_fan_large)와 MCP 파이프라인 타임아웃 로그만 남아 있다.
- 플레이 검증 결과:
  - 기본값(30s)으로 자연 등장하는 것을 확인했다. 1회차는 컷씬(정지 → 낙하 → Fire buff 1회)으로 등장하고, 2회차부터는 뒤에서 달려 들어온다.
  - 추격 → 퇴장 → 기본 카메라 복귀까지 확인했다.
  - 2D 컷씬, 2D 추격, 추격 중 3D↔2D 전환을 확인했다.
- 계획과 달라진 점(각 항목 메모 참고):
  - **복도가 아치 천장(3.7~6.9m)이라** 카메라와 낙하 높이 기본값을 바꿨다.
    - `vCam_Boss3D`: (0, 7, -22) → **(0, 5.2, -17.5) / pitch 28°**
    - `vCam_BossIntro`: **(1.6, 1.0, -6.5)**, LookAt 쿠파
    - `dropHeight`: 15 → **6**
    - `chaseZ3D`: -13 → **-14**
    - `chaseZ2D`: -11.5 → **-10.8**
  - `SetBossCamera`는 큐를 거치지 않고 즉시 적용한다.
  - Fire buff 안의 `HS_EffectSound`(clip 없음)는 `Repeating`도 껐다.

## 확정된 결정 (피드백 1차)
- **컷씬**
  - 코드(코루틴 + vCam 우선순위)로 카메라를 전환한다. Timeline은 쓰지 않는다.
  - 컷씬 동안 게임을 **정지**한다(timeScale 0).
  - 2D 상태에서도 재생한다.
  - **게임(씬 로드)당 1번**만 재생한다. 첫 등장에서만 쓴다.
- **첫 등장(컷씬)**: 쿠파가 하늘에서 추격 위치로 떨어진다. 바닥에 닿는 순간 Fire buff 이펙트를 **한 번** 재생하고, 이펙트는 끝나면 사라진다.
- **두 번째 등장부터**: 컷씬 없이 플레이어 **뒤에서 달려 들어온다**. 정지하지 않는다. 3D이면 카메라만 보스 카메라 위치로 전환한다.
- **Fire buff**: 원본 프리팹의 루프를 끈다.
- **출현 간격**: 게임 시작 시점부터 세고, 퇴장이 끝나면 다시 센다.

## 확정된 결정 (피드백 2차)
- **착지 쉐이크**: 착지할 때 카메라를 흔든다. 정지 중에도 동작해야 하므로 unscaled time으로 흔든다.
- **두 번째 등장 이후 2D**: 카메라는 바꾸지 않는다. 쿠파만 뒤에서 달려 들어온다.

## 설계 요약
- **보스 오브젝트**: `BossBowser` 프리팹. 모델은 기존 `bowser_3d_model`을 쓰고 루트에 `Boss` 컴포넌트를 둔다. 충돌·데미지는 없다(이번 범위는 등장/퇴장만).
- **FSM**: 평범한 C# 클래스 FSM이다.
  - `BossState`는 `Enter` / `Tick(dt)` / `Exit`를 가진다.
  - `BossStateMachine`은 `Change`와 `Tick`만 제공한다.
  - 상태는 파일 하나에 하나씩 둔다. 이후 공격 패턴도 상태로 추가한다.

```
            ┌─(첫 등장)──▶ Intro(컷씬: 낙하+착지 이펙트) ─┐
Hidden ─────┤                                              ├──▶ Chase ──(stayDuration)──▶ Exit ──▶ Hidden
 (appearInterval)└─(이후)────▶ Enter(뒤에서 달려옴) ─────────┘
```

| 상태 | 하는 일 |
|---|---|
| `Hidden` | 모델 비활성. scaled time으로 `appearInterval` 카운트. 끝나면 `introPlayed`가 false면 `Intro`, 아니면 `Enter`로 넘어간다 |
| `Intro` | timeScale 0. 쿠파를 추격 위치 위 `dropHeight`에서 활성화하고 `vCam_BossIntro`로 컷 전환한다. unscaled dt로 낙하(가속)한다. 착지하면 Fire buff를 재생하고 카메라 쉐이크(unscaled)를 건 뒤 `landHold`만큼 대기한다. timeScale을 복원하고 `introPlayed = true`로 둔 뒤 `Chase`로 넘어간다 |
| `Enter` | 쿠파를 `spawnZ`(플레이어 뒤, 화면 밖)에서 활성화하고 `SetBossCamera(Chase)`를 호출한다(3D면 보스 카메라로 블렌드). `ChaseZ`까지 달려 들어온 뒤 `Chase`로 넘어간다 |
| `Chase` | `ChaseZ`를 유지하며 플레이어 레인 X를 지연 추적한다(2D는 마지막 레인). `stayDuration`이 지나면 `Exit` |
| `Exit` | -Z로 가속해 멀어진다. `exitZ`를 지나면 `SetBossCamera(None)`, 모델을 끄고 `Hidden`으로 넘어간다 |

- **위치**: 추격 Z는 뷰별로 따로 둔다. `chaseZ3D` 기본값 -13, `chaseZ2D` 기본값 -11.5. 뷰가 바뀌면 Z를 부드럽게 옮긴다.
- **카메라** (`CameraManager`)
  - `vCam_Boss3D`: 3D 추격 전용 고정 카메라. 초기값 (0, 7, -22), pitch 25°.
  - `vCam_BossIntro`: 컷씬용. LookAt을 쿠파로 지정해 낙하를 따라간다. 쿠파 앞쪽 낮은 각도에서 잡는다.
  - 보스 카메라 상태 `BossCamMode { None, Intro, Chase }`를 두고, 우선순위는 `ApplyPriorities()` 한 곳에서 계산한다.
    - Intro: 뷰 모드와 상관없이 `vCam_BossIntro`.
    - Chase + 3D: `vCam_Boss3D`.
    - Chase + 2D: `vCam_2D`.
    - None: 기존 규칙.
  - `SwitchCameraCoroutine`도 이 함수를 사용한다. 그래서 추격 중에 뷰가 바뀌어도 맞는 카메라로 간다.
  - 보스 카메라 전환은 모두 기존 `cameraAction` 큐를 거친다.
  - Brain의 `IgnoreTimeScale`을 켠다. 정지 중에도 컷씬 블렌드가 진행되어야 하기 때문이다.
- **착지 이펙트**: 기존 이펙트 풀을 재사용한다. `EffectType.FIRE_BUFF`를 추가하고 `FX_FireBuff` 래퍼 프리팹을 만든다. 호출은 `EffectPool.Get(FIRE_BUFF, 발 위치)`로 한다.

## 인스펙터 노출 필드 (`Boss`)
- 타이밍:
  - `appearInterval` (s, 기본 30): 출현 간격. 게임 시작부터 세고, 퇴장 후 다시 센다.
  - `stayDuration` (s, 기본 10): 추격 유지 시간.
- 컷씬:
  - `dropHeight` (기본 15), `fallGravity` (기본 40).
  - `introLeadTime` (기본 0.4s): 낙하 전에 카메라를 먼저 보여 주는 시간.
  - `landHold` (기본 1.5s): 착지 후 대기.
  - `landEffect` (기본 `FIRE_BUFF`).
  - `landShakeAmplitude` (기본 3), `landShakeFrequency` (기본 2), `landShakeDuration` (기본 0.6s). `DECREMENT` 모드로 흔든다.
- 이동:
  - `spawnZ` -40, `chaseZ3D` -13, `chaseZ2D` -11.5, `exitZ` -45.
  - `enterSpeed`, `exitAccel`, `laneFollowSpeed`.
- 카메라 블렌드:
  - `chaseBlend` (기본 1s): Enter와 컷씬 종료 후 사용.
  - `exitBlend` (기본 1s).
  - 컷씬 진입은 컷(0s)으로 전환한다.

---

## Phase 1 — FSM 골격
- [x] `Assets/02. Scripts/Boss/BossState.cs`: 추상 클래스 `BossState(Boss boss)`. `virtual Enter()`, `Tick(float dt)`, `Exit()`.
- [x] `Assets/02. Scripts/Boss/BossStateMachine.cs`: `Current`, `Change(BossState)`(Exit → Enter), `Tick(dt)`.
- [x] `Assets/02. Scripts/Boss/Boss.cs`:
  - 메모: X 추적은 레인 인덱스 대신 플레이어 X를 지수 Lerp로 따라간다. 2D에서는 플레이어가 마지막 레인에 고정되므로 자동으로 맞는다. 컷씬 도중 비활성화될 때를 대비해 `OnDisable`에서 timeScale을 복원한다.
  - 인스펙터 필드와 상태 인스턴스 5개를 만든다.
  - `introPlayed` 플래그를 둔다(인스턴스 필드라 씬을 다시 로드하면 리셋된다).
  - `Update`에서 `Tick`을 호출한다. Intro 중에는 unscaled dt를 넘긴다.
  - 헬퍼: `ChaseZ`, `TargetX`, `SetModelVisible`.
- [x] MCP: 컴파일 및 콘솔 확인.

## Phase 2 — 상태 구현
- [x] `Boss/States/BossHiddenState.cs`: 타이머 → `Intro` 또는 `Enter`.
- [x] `Boss/States/BossIntroState.cs`: 정지 → `introLeadTime` → 낙하 → 착지 이펙트 + 쉐이크 → `landHold` → 재개 → `Chase`.
- [x] `Boss/States/BossEnterState.cs`: `spawnZ`에서 `ChaseZ`까지 달려온다. Enter 시점에 `SetBossCamera(Chase)`를 호출한다.
- [x] `Boss/States/BossChaseState.cs`: X/Z 추적, `stayDuration` → `Exit`.
- [x] `Boss/States/BossExitState.cs`: -Z 가속 → `exitZ` → 카메라 복귀 → `Hidden`.
- [x] MCP: 컴파일 및 콘솔 확인.

## Phase 3 — CameraManager 확장
- [x] `CameraManager.cs`(CP949 — 인코딩 유지를 위해 바이트 단위로 편집):
  - 필드 `vCamBoss3D`, `vCamBossIntro`를 추가한다.
  - `enum BossCamMode`를 추가한다.
  - `SetBossCamera(mode, blend)`를 추가한다.
    - 메모(계획 변경): 큐에 넣지 않고 **즉시 적용**한다. 우선순위는 (뷰 모드 + 보스 모드)로 계산되므로 순서와 무관하다. 또 큐의 `SwitchCameraCoroutine`은 scaled `WaitForSeconds`를 쓰기 때문에, 정지 중이면 컷씬 카메라가 그 뒤에서 막힐 수 있다.
  - `ApplyPriorities()`를 추가하고, `SwitchCameraCoroutine`의 우선순위 코드를 이 함수로 대체한다.
- [x] 컷씬 중(timeScale 0)에 큐 코루틴의 `WaitForSeconds`가 멈추는 문제 → 위 즉시 적용으로 해결했다. 쉐이크의 unscaled 경로는 `WaitForSecondsRealtime`을 쓴다.
- [x] `CameraShake`에 선택 인자 `bool unscaled = false`를 추가한다. true면 `WaitForSecondsRealtime`과 `Time.unscaledDeltaTime`을 쓴다. 기존 호출부는 바뀌지 않는다.
  - Perlin 노이즈 자체는 Brain의 `IgnoreTimeScale`을 켜면 정지 중에도 흔들린다.
- [x] `vCam_BossIntro`에 Perlin을 붙인다. 쉐이크는 활성 vCam 기준이라 컷씬 중에는 이 카메라가 흔들린다.
- [x] Brain의 `IgnoreTimeScale`을 켠다. 기존 Resize 연출 중 블렌드에 영향이 없는지 확인한다.
  - 메모: 이제 Resize(timeScale 0) 중에도 카메라 블렌드와 노이즈가 진행된다. Resize 중 카메라 전환이 없어서 시각적 차이는 없다.
- [x] MCP: 컴파일, 콘솔, 기존 2D↔3D 전환이 그대로 동작하는지 확인.

## Phase 4 — 착지 이펙트
- [x] `Fire buff.prefab`(원본) 수정:
  - ParticleSystem 19개의 `looping`을 0으로 바꾼다.
  - `useUnscaledTime`을 켠다(정지 중 재생용).
  - 메모: 19개 모두 적용했다(looping 0 / useUnscaledTime 1).
  - 메모(추가): 자식의 `HS_EffectSound`가 `Repeating=1`(3초 `InvokeRepeating`)이었다. 이 때문에 풀로 비활성화된 뒤에도 `PlayOneShot`이 호출되어 "Can not play a disabled audio source" 경고가 반복됐다. 이것도 `Repeating=0`으로 껐다. AudioSource clip이 비어 있어서 원래 소리는 나지 않는다.
- [x] `EffectDataSO.cs`: `EffectType` 끝에 `FIRE_BUFF`를 추가한다.
- [x] 래퍼 프리팹 `Assets/07. Prefabs/Effect/FX_FireBuff.prefab`: 루트에 `PooledEffect`, 자식으로 `Fire buff`를 중첩한다. `FX_DustExplosion`과 같은 구조다.
- [x] `Assets/11. Datas/EffectData.asset`에 `FIRE_BUFF → FX_FireBuff`를 등록한다.
- [x] MCP: 이펙트를 단독 재생해서 한 번만 재생되고 비활성화되는지 확인한다.

## Phase 5 — 보스 에셋 / 씬
- [x] `Bowser.controller`에 `Run` 스테이트를 추가하고 기본 스테이트로 지정한다. Walk는 남겨 둔다.
- [x] 프리팹 `Assets/07. Prefabs/Boss/BossBowser.prefab`:
  - 루트에 `Boss`를 둔다.
  - 자식 모델은 scale 3이며, 정면이 +Z인지 확인한다.
  - Animator `updateMode`는 `UnscaledTime`으로 둔다. 컷씬 중에도 움직여야 하기 때문이다.
- [x] SampleScene:
  - 메모: 씬에 있던 `Bowser`(Animator가 있는 모델)를 그대로 `BossBowser` 루트의 자식으로 옮겨 프리팹화했다. 그래서 씬의 `Bowser` 루트는 사라졌다. 씬을 저장할 때 에디터에 열려 있던 미저장 변경도 함께 저장됐다.
  - 기존 루트 `Bowser`를 삭제하고 `BossBowser`를 배치한다.
  - `vCam_Boss3D`와 `vCam_BossIntro`를 만든다. Perlin을 붙이고 Priority는 0으로 둔다.
  - `CameraManager`에 두 카메라를 연결한다.
- [x] MCP: 씬 참조와 Missing 여부를 확인한다.

## Phase 6 — 플레이 검증
- [x] 테스트용으로 `appearInterval`과 `stayDuration`을 짧게 해서 플레이한다(씬 값은 변경 후 원복).
  - 1회차: 정지 → 낙하 → 착지 이펙트 1회 + 쉐이크 → 재개 → 추격 → 퇴장.
  - 2회차: 컷씬 없이 뒤에서 등장하며, 3D면 보스 카메라로 전환된다.
- [x] 2D에서 첫 등장할 때 컷씬이 재생되고 vCam_2D로 돌아가는지 확인한다.
  - 메모: 2D 추격 시 `chaseZ2D` -11.5에서는 쿠파가 화면 왼쪽 끝에 절반 잘려 -10.8로 당겼다. 여전히 일부 잘린다. vCam_2D는 기존 카메라라 건드리지 않았다.
- [x] 추격 중 3D↔2D 전환 시 카메라와 쿠파 Z가 맞는지 확인한다.
- [x] 컷씬 중에는 HP/장애물이 정지하고, 이후 정상 재개되는지 확인한다.
- [x] 사망해서 재시작하면 컷씬이 다시 1회 재생되는지 확인한다(게임당 1회).
  - 메모: 실제로 죽어서 확인하지는 않았다. `introPlayed`는 씬 오브젝트(`Boss`)의 인스턴스 필드이고 DDOL이 아니다. 사망하면 SampleScene을 다시 로드하므로 리셋된다(코드로 확인).
- [x] `capture_game_view`로 구도를 확인해 vCam 위치를 조정한다(캡처 파일은 `.meta`까지 삭제).
  - 메모: 처음 값으로는 보스 카메라가 복도 천장 밖에 있었다. 위 '계획과 달라진 점'의 값으로 조정했다. 추격 화면에서 쿠파 등껍질이 앞에 보이고 그 너머로 플레이어가 보이며, 모델이 +Z를 바라보는 것도 확인했다.
- [x] 콘솔 신규 에러/경고 0 확인.

---

## 남은 확인 사항 / 후속
- 쉐이크는 정지 상태 unscaled 경로가 동작하는 것을 확인했다(진폭 감소, 카메라 흔들림). 단, `CameraShake`는 호출 시점의 활성 vCam을 쓴다. 그래서 카메라 전환과 **같은 프레임**에 호출하면 이전 카메라가 흔들린다. 컷씬에서는 전환 후 0.4초 이상 지나 착지하므로 문제가 없다.
- 공격 패턴은 `BossState`를 상속한 새 상태로 추가하면 된다(예: `Chase`에서 분기).

---

## Phase 7 — 쿠파 Humanoid 리깅 (Screaming 리타겟팅용)
사용자 지시: 현재 쿠파 모델의 복사본을 만들어 Humanoid로 리깅한다. 동작이 확인되면 `BossBowser`의 모델을 복사본으로 교체한다.
근거: Screaming 클립(Mixamo, Humanoid로 변경됨)은 `mixamorig:` 경로를 쓰고, 쿠파는 `Armature/Hips/...`(Unity Humanoid 표준 본 이름)를 쓴다. Generic 상태에서는 바인딩이 0개라 동작하지 않는다.
- [x] `bowser_3d_model.fbx`를 같은 폴더에 `bowser_3d_model_Humanoid.fbx`로 복사한다. 머티리얼 리맵 설정도 함께 복사한다.
- [x] 복사본 Rig: Humanoid, Avatar *Create From This Model*. Avatar가 valid/human인지, 필수 본 매핑을 확인한다.
  - 메모: `bowser_3d_model_HumanoidAvatar`는 valid/human이다. 본 이름이 Unity 표준이라 몸통, 팔다리, 손가락, UpperChest까지 전부 자동 매핑됐다.
- [x] 검증: 임시 오브젝트에 Screaming과 기존 Run 클립을 재생해 본이 움직이는지 확인한다. 임시 오브젝트는 확인 후 삭제한다.
  - 결과: Screaming(Humanoid)은 정상 재생됐다. **기존 Run(Generic, `Armature/...` 경로)은 Humanoid 아바타에서 재생되지 않았다**(근육 기본 자세로 고정).
  - 메모(계획 추가): 그대로 교체하면 달리기가 깨진다. 그래서 Run/Walk도 같은 방식으로 **복사본**을 만들었다. `bowser_3d_model_Run_Humanoid.fbx`, `bowser_3d_model_Walk_Humanoid.fbx`는 Humanoid이고, Avatar는 *Copy From Other*로 쿠파 Humanoid Avatar를 쓴다. 원본 Run/Walk FBX는 그대로 두었다.
  - Humanoid Run 복사본이 매 주기 본 각도가 바뀌며 달리는 것을 확인했다. 임시 컨트롤러(`Assets/_tmp_bowser_test.controller`)와 테스트 오브젝트는 삭제했다.
- [x] 교체: `BossBowser` 프리팹의 모델 자식을 복사본 인스턴스로 바꾼다. 다음 설정은 유지한다.
  - scale 3
  - `Bowser.controller`
  - Animator UnscaledTime
  - `Boss.model` 참조
  - 메모: `Bowser.controller`의 Run/Walk 모션도 Humanoid 복사본 클립으로 바꿨다. 씬의 `BossBowser`는 프리팹을 따라가므로 자동으로 반영됐다(avatar human=True, UnscaledTime, scale 3).
- [x] MCP: 컴파일, 콘솔, 씬/프리팹 참조, 플레이 확인.
  - 메모: 컴파일 에러 0. 새 경고 0이며, 기존 Missing Script와 MCP 타임아웃 로그만 남아 있다. 플레이 결과 컷씬(정지 중)과 추격에서 Run 애니메이션이 정상 재생되는 것을 캡처로 확인했다.
  - 남은 일: Screaming 연결 → Phase 8에서 완료.

---

## Phase 8 — 착지 애니메이션 (Idle → Screaming → Run, 블렌드) + Walk 클립 교체
사용자 지시: 착지 시 Idle → Screaming → 달리기로 이어지도록 블렌드를 쓴다. Walk는 `Vanguard By T. Choonyung_Zombie Running.fbx`에서 클립을 추출해 쓰고, FBX는 삭제한다.
확인 사항:
- Idle 클립은 `Assets/04. Animations/humanoid_idle.fbx`의 `Idle`(Humanoid, 2.4s, loop)뿐이다.
- Zombie Running FBX는 Humanoid이고 클립 `mixamo.com` 0.8s, loop 꺼짐이다. 이 FBX를 참조하는 프리팹/씬/컨트롤러는 없다.
- [x] 클립 추출: `Assets/04. Animations/Clip/Zombie_Running.anim`(loop 켬, 루트 회전/높이 Bake). 원본 FBX와 `.meta`를 삭제한다.
  - 메모: 추출본은 Humanoid, loop 켬, 0.8s다. 추출을 확인한 뒤 `AssetDatabase.DeleteAsset`으로 FBX를 삭제했다. 이 FBX는 git 미추적 파일이라 diff에는 나오지 않는다.
- [x] `Bowser.controller`:
  - Trigger `Scream`을 추가한다.
  - 상태 `Idle`, `Screaming`을 추가한다. Walk 모션은 `Zombie_Running`으로 바꾼다.
  - 전이:
    - Idle → Screaming: Trigger `Scream`, 블렌드 0.25s.
    - Screaming → Run: exit time 0.85, 블렌드 0.3s.
  - 기본 상태는 Run으로 유지한다(두 번째 등장 이후 달려 들어올 때 사용).
- [x] `Boss.cs`:
  - 모델 Animator를 캐시한다.
  - `landHold`를 `landIdleTime`(착지 후 Idle 유지 시간, 기본 0.3s)과 `maxLandHold`(안전 타임아웃, 기본 6s)로 바꾼다.
  - 메모: 프리팹에 남은 `landHold` 직렬화 값은 사용하지 않으며, 다음에 저장할 때 정리된다.
- [x] `BossIntroState`:
  - Enter에서 `Idle`을 재생한다(낙하 중에도 Idle).
  - 착지 후 `landIdleTime`이 지나면 `Scream` 트리거를 건다.
  - Run으로 블렌드가 시작되면 게임을 재개하고 Chase로 넘어간다.
- [x] MCP: 컴파일, 콘솔, 플레이 확인(애니 전이와 블렌드).
  - 결과: 낙하 중 `Idle`, 착지 후 `Screaming`, 재개 후 `Chase`에서 `Run` 순서로 전환되는 것을 확인했다(캡처로 Screaming 자세 확인). 새 에러/경고 0.
  - 메모: 블렌드 구간(0.25s/0.3s)은 샘플링 간격(약 1s)보다 짧아 수치로 잡지는 못했다. 컨트롤러 전이의 fixed duration 설정으로 처리된다.

---

## Phase 9 — 모든 상황에서 Walk 사용
사용자 지시: Run 대신 모든 상황(첫 컷씬 이후, 두 번째 등장부터, 추격, 퇴장)에서 Walk를 쓴다.
- [x] `Bowser.controller`: 기본 상태를 Walk로 바꾸고, Screaming → Walk 전이(exit 0.85, 블렌드 0.3s)를 만든다. 쓰지 않게 된 Run 상태는 삭제한다(`bowser_3d_model_Run_Humanoid.fbx` 파일은 남긴다).
- [x] `BossIntroState`: 재개 판정 대상 상태를 `Run`에서 `Walk`로 바꾼다.
- [x] MCP: 컴파일, 콘솔, 플레이 확인.
  - 결과: 컷씬은 Idle → Screaming → (블렌드) → Walk 순서로 재생된다. 블렌드 구간도 샘플에 잡혔다. 추격, 퇴장, 두 번째 등장(Enter)에서는 모두 Walk를 재생한다. 새 에러/경고 0.
