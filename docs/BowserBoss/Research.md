# Bowser(쿠파) 추격 보스 — Research

작업 목표: 일정 시간마다 쿠파가 플레이어 **뒤에서** 등장해 추격하다가 퇴장하는 방해 요소. 이번 범위는 **등장/퇴장까지만**이다(공격 패턴은 이후).
- 등장: Cinemachine 카메라 컷씬.
- 퇴장: 단순히 뒤로 멀어진다.
- 동작은 FSM으로 상태별로 나눈다.
- 3D에서는 쿠파가 있을 때만 별도 추격 카메라를 쓴다. 2D에서는 기존 카메라를 그대로 쓴다.
- 인스펙터에서 출현 간격과 유지 시간을 조절할 수 있어야 한다.

## 1. 관련 에셋 (확인한 사실)

| 종류 | 경로 / 위치 | 비고 |
|---|---|---|
| 모델 FBX | `Assets/TripoModels/bowser_3d_model/bowser_3d_model.fbx` | Generic 리그, globalScale 1 |
| 애니 FBX | `bowser_3d_model_Run.fbx`, `bowser_3d_model_Walk.fbx` | `Run` 0.667s(loop), `Walk` 1.333s |
| 컨트롤러 | `Assets/04. Animations/Animator/Bowser.controller` | 스테이트는 **`Walk` 하나뿐**(기본). Run 스테이트 없음 |
| 씬 인스턴스 | SampleScene 루트 `Bowser` | 프리팹 아님(씬에 직접 배치). 위치 (12.32, 0, -14.17), 회전 0, **scale 3**, 크기 약 2.9×2.9×2.5m. 루트에 `Animator`만 있음(rootMotion 꺼짐, AlwaysAnimate, avatar 없음). 레이어 Default |
| 보스 스크립트 폴더 | `Assets/02. Scripts/Boss/` | 비어 있음 |
| Timeline 패키지 | `com.unity.timeline` 설치됨 | 씬에 PlayableDirector는 없음 |

## 2. 카메라 현재 구조

- `CameraManager`(Main Camera에 붙은 싱글톤)
  - 필드: `vCam3D`, `vCam2D`, `cinemachineBrain`.
  - 카메라 동작은 `cameraAction` 큐에 넣은 코루틴을 **순차 실행**한다.
  - `SwitchCamera(mode, time)` → `SwitchCameraCoroutine`:
    - `DefaultBlend`를 `EaseOut, time`으로 **덮어쓴다**.
    - 3D: cullingMask 전체, `vCam3D` 10 / `vCam2D` 5.
    - 2D: `"Right"` 레이어 컬링, `vCam3D` 5 / `vCam2D` 10.
  - `CameraShake`는 현재 활성 vCam(`ActiveVirtualCamera`)의 `CinemachineBasicMultiChannelPerlin`을 쓴다. → **새 vCam에도 Perlin을 붙여야 쉐이크가 동작한다.**
- 씬 vCam (에디터 값)

| 이름 | 위치 | 회전 | Priority | Follow/LookAt |
|---|---|---|---|---|
| `vCam_3D` | (0, 3, -12) | (20, 0, 0) | 10 | 없음(고정) |
| `vCam_2D` | (5.64, 0.6, -6.43) | (0, 270, 0) | 9 | 없음(고정), -X 방향 측면 뷰 |

- Brain: 기본 블렌드 EaseInOut 1s, SmartUpdate, **IgnoreTimeScale = false**.

## 3. 월드/플레이어 수치
- Player (0, 0, -9), scale 0.6. Z 고정, X만 레인 이동. 월드가 `-Z`로 `mapSpeed`(9)만큼 흐른다.
- `GeneratePos` z 70, `DisposePos` z -50.
- 2D에서는 플레이어가 마지막 레인(x = 1)에 고정된다.

### 쿠파 배치 관련 계산
- `vCam_3D`(z -12, y 3)는 플레이어 바로 뒤다. 쿠파(높이 약 2.9m)를 플레이어 뒤 z -12~-9에 두면 화면을 가리고, z -12보다 뒤에 두면 카메라 뒤라서 안 보인다. → **3D 추격 카메라를 따로 세워야 하는 이유**(요구사항과 일치).
- `vCam_2D`(z -6.43, 거리 약 4.6, FOV 60, 16:9 가정)가 보는 Z 범위는 대략 **-11.2 ~ -1.7**이다. 쿠파를 z -14에 두면 2D 화면 밖이다. → **2D에서는 추격 Z를 3D와 다르게(약 -11 근처) 둬야 보인다.**

## 4. 의존 관계와 영향 범위
- **`CameraManager.SwitchCameraCoroutine`**: 2D/3D 전환 시 우선순위를 고정값으로 덮는다. 보스 카메라가 켜진 상태에서 뷰가 바뀌면 이 함수가 보스 카메라 우선순위도 함께 정해야 한다. 또 `DefaultBlend`를 덮어쓰므로 보스 카메라 블렌드 시간도 직접 지정해야 한다.
- **`Player.CheckChangeView` → `GameManager.ChangeViewMode`**: HP 비율로 언제든 뷰가 바뀔 수 있다. 보스 추격 중에도 바뀔 수 있다.
- **`LaneManager.RemoveAllLaneObjects`**: 뷰 전환 시 레인 오브젝트만 회수한다. 쿠파는 레인을 거치지 않으므로 영향이 없다.
- **`Time.timeScale`**: `Player.ResizeCoroutine`이 이미 timeScale 0으로 게임을 멈추는 방식을 쓴다. 컷씬 중 정지를 하려면 Brain `IgnoreTimeScale`과 쿠파 Animator `updateMode = UnscaledTime`이 필요하다. 반대로 정지하지 않으면 컷씬 동안 HP 감소와 장애물이 계속 진행된다.

## 5. 착지 이펙트 (Fire buff)
- 원본: `Assets/07. Prefabs/Effect/Hovl Studio/Epic Toon VFX 2/Prefabs/Fire buff/Fire buff.prefab`
  - 루트 `Fire buff` 아래에 ParticleSystem이 여러 개 있다(GroundWaves, FireCore, ShockWave, Sparks, StartFlash, FireUp, MainTrails 등).
  - `looping: 1`이 **19개**다. 모두 `playOnAwake 1`, `lengthInSec 3`, `stopAction 0`이다.
- 기존 이펙트 풀 구조
  - `EffectType` enum(`EffectDataSO.cs`): `NONE`, `DUST_EXPLOSION`, `TINY_EXPLOSION`.
  - `Assets/11. Datas/EffectData.asset`: 타입 → 프리팹 매핑. type 1 → `FX_DustExplosion`, type 2 → `FX_TinyExplosion`.
  - 래퍼 프리팹 `Assets/07. Prefabs/Effect/FX_DustExplosion.prefab`은 루트에 `PooledEffect`(lifetime 0 = 자동 계산)를 두고, 그 아래에 원본 벤더 프리팹을 중첩 인스턴스로 넣는다.
  - 호출 방법: `(Pool<PooledEffect>.Instance as EffectPool)?.Get(type, position)`. 사용 예시는 `Whomp.cs:198`.
  - `PooledEffect`
    - `OnEnable`에서 모든 파티클을 `Clear`하고 `Play`한다.
    - `lifetime`(자동 계산: duration + 최대 startLifetime)이 지나면 `SetActive(false)`로 처리한다. 이때 **scaled time**(`WaitForSeconds`)을 쓴다.
- **위험**: 착지는 컷씬 중(timeScale 0)에 일어난다. ParticleSystem은 기본적으로 scaled time을 쓰므로 정지 중에는 **재생되지 않는다.** 원본 프리팹의 `useUnscaledTime`을 켜야 한다.
- **위험**: `looping`을 꺼도 `lifetime` 자동 계산은 그대로 동작한다. 정지 중에는 `WaitForSeconds`가 흐르지 않으므로, 비활성화는 정지가 풀린 뒤에 일어난다. 파티클은 이미 끝난 상태라 화면상 문제는 없다.

## 6. 위험 요소
- **모델 정면 방향 미확인**: 씬 인스턴스가 회전 0이다. 모델이 +Z를 바라보는지는 프리팹 작업 시 눈으로 확인해야 한다. 추격하려면 +Z(플레이어 쪽)를 봐야 한다.
- **Animator avatar 없음**: Generic 클립이 경로 바인딩으로 재생되는지 확인이 필요하다(현재 Walk가 씬에서 재생되는지).
- **컷씬과 뷰 전환 충돌**: 컷씬 중에 HP 임계값으로 뷰가 바뀌면 카메라 큐가 꼬일 수 있다. timeScale 0이면 HP 감소도 멈추므로 문제가 생기지 않는다.
- **블렌드 덮어쓰기**: `SwitchCamera`가 `DefaultBlend`를 바꾸므로 보스 카메라 전환도 같은 큐에서 블렌드를 지정해야 일관된다.
- **씬의 쿠파 인스턴스**: 지금은 (12.32, 0, -14.17)에 항상 보이게 서 있다. 보스 오브젝트로 바꾸거나 삭제해야 한다.
