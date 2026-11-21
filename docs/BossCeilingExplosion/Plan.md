# BossCeilingExplosion — Plan

## 구현 현황 요약

- 완료: Phase 1~3 전 항목.
- 미완료: 없음.
- 계획과 달라진 점:
  - `ceilingY` 기본값은 **6.3**으로 정했다. 실측 결과 천장 안쪽 면이 레인 X(-1~1)에서 6.21~6.49m였다.
  - `FX_Explosion` 래퍼 안의 Explosion 인스턴스는 localPos **y=1**(원본 오프셋)로 둔다. 뒤집으면 폭발 중심은 천장 1m 아래에 오고, 지면 링(WaveBorders)은 천장 면에 붙는다. 처음에는 0으로 뒀더니 링이 천장 위로 올라갔고, 폭발도 천장 면에 박혀 보이지 않았다.
  - `BossBowser.prefab`을 저장하면서, 프리팹에 남아 있던 구 필드 `landHold`가 현재 필드(`landIdleTime`, `maxLandHold`, `roarSfx`)로 재직렬화됐다. 값은 코드 기본값과 같아 동작에는 변화가 없다.
  - `Explosion.prefab`을 재직렬화하면서 diff가 크게 나왔다(-35k줄). 기본값 커브 데이터가 생략된 것뿐이고, 오브젝트 수(51개)는 같다.

근거: `docs/BossCeilingExplosion/Research.md`

## 목표

- 쿠파 첫 등장 컷씬에서 보스가 **천장을 뚫고** 떨어진다. 보스 발이 천장 높이(`ceilingY`)를 지나는 **그 프레임**에 `(보스 X, ceilingY, 보스 Z)`에서 Explosion 이펙트를 1회 재생한다.
- Explosion 이펙트의 루프(파티클 looping + 사운드 반복)를 끈다.
- 기존 착지 이펙트(FIRE_BUFF)와 쉐이크는 그대로 둔다.

## 확정 사항 (피드백 반영)

1. 보스가 천장 위에서 시작해 천장을 **뚫고** 내려오는 순간에 생성.
2. 원본 사운드(`HS_EffectSound`)는 끈다. SoundManager SFX 추가 안 함.
3. 원본 `Explosion.prefab` 직접 수정 허용 → 사운드 끄기도 원본에서 처리.
4. 이펙트는 X축 180° 뒤집어 생성(천장에서 아래로).

## 설계

### 타이밍
- 낙하 시작 높이 `dropHeight` 6 → **9**(천장 바깥면 최고 6.89m 위). 보스는 천장에 가려진 채 시작한다.
- `Fall` 단계에서 이동 전 y(`prevY`)와 이동 후 y를 비교한다. `prevY > ceiling && pos.y <= ceiling`인 프레임에 이펙트를 생성한다. 프레임 단위로 교차를 검출하므로 낙하 속도와 상관없이 정확히 1회 생성된다.
- 같은 프레임에 천장과 바닥을 모두 지나도 천장 이펙트 → 착지 순서로 둘 다 처리된다.
- 시작 높이가 천장 아래면 생성하지 않는다.

### 이펙트
- `EffectType.CEILING_EXPLOSION` → `FX_Explosion`(Explosion 프리팹 인스턴스 + `PooledEffect`) → `EffectData.asset` → `EffectPool.Get(type, pos, Quaternion.Euler(180,0,0))`.
- 원본 `Explosion.prefab`: ParticleSystem 12개 `looping 0`, `useUnscaledTime 1`, `HS_EffectSound` 비활성.

## Phase 1 — 이펙트 프리팹

- [x] `Explosion.prefab`(원본): ParticleSystem 12개 `looping: 0`, `useUnscaledTime: 1`. `HS_EffectSound` 컴포넌트 비활성(사운드 off). `PrefabUtility.LoadPrefabContents`로 수정.
- [x] `EffectDataSO.cs`: `EffectType` 끝에 `CEILING_EXPLOSION`(=4) 추가.
- [x] `Assets/07. Prefabs/Effect/FX_Explosion.prefab` 생성: Explosion 프리팹 인스턴스(localPos y=1) + `PooledEffect`.
  - 메모: 사운드는 원본에서 껐으므로 래퍼 오버라이드는 없음.
- [x] `Assets/11. Datas/EffectData.asset`에 `CEILING_EXPLOSION → FX_Explosion` 등록.
- [x] MCP: 컴파일 성공, 콘솔 에러 0.

## Phase 2 — 보스 코드

- [x] `Boss.cs`: `ceilingY`(기본 6.3), `ceilingEffect`(기본 `CEILING_EXPLOSION`) 추가, `dropHeight` 기본 9 + Tooltip 수정.
- [x] `BossIntroState.cs`: `Fall`에서 천장 교차 검출 → `SpawnCeilingEffect`(X 180° 회전). 상단 주석 갱신.
- [x] MCP: 컴파일 성공, 콘솔 에러 0.

## Phase 3 — 값 조정 & 플레이 검증

- [x] 천장 높이 측정: 복도 메시에 콜라이더가 없어 `Ceiling_Vault`에 임시 MeshCollider를 붙여 레이캐스트했다. 안쪽 면은 x=-1/0/1에서 6.43/6.21/6.49, 바깥 면은 6.70~6.89였다. → `ceilingY = 6.3`.
- [x] `BossBowser.prefab`: `dropHeight 9`, `ceilingY 6.3`, `ceilingEffect CEILING_EXPLOSION`.
- [x] 플레이 검증:
  - 이펙트가 `(0, 6.3, -14)`에 180° 회전으로 1회 생성됐다.
  - timeScale 0 상태에서도 파티클이 재생됐다(139개, `isPlaying`).
  - 컷씬 → 추격 → 퇴장 정상, 이후 이펙트 비활성화, 재생성 없음(루프 없음).
- [x] 화면 확인: 낙하 중 보스가 천장을 통과하는 장면과 천장에 붙은 폭발 이펙트를 게임 뷰 캡처로 확인했다. Lead 동안의 카메라(천장 올려다봄)도 이상 없다.
- [x] 캡처 파일 삭제(.meta 포함). 의도치 않은 렌더러/머티리얼 diff 없음(Hovl 머티리얼·SampleScene 변경은 작업 전부터 있던 것).
