# DistanceSpawn — Plan

## 구현 현황 (2026-10-14)
- **완료**: Phase 1, 2 전 항목, Phase 3의 간격/속도 변경 검증(편집 모드 시뮬레이션으로 대체).
- **미완료**: 실제 플레이 모드 관찰 — 에디터가 백그라운드에서 플레이 프레임을 진행하지 않아(frameCount 1~2에서 정지) 측정 불가. 사용자가 직접 플레이로 확인 필요.
- 컴파일 에러 0. 콘솔 에러 1건은 MCP 파이프라인 타임아웃 로그(게임 코드 무관), 경고는 기존 Missing Script.

근거: `docs/DistanceSpawn/Research.md`

## 설계 요약
- 모든 `WaitForSeconds`를 `WaitDistance(유닛)`으로 교체. `WaitDistance`는 `WaitForFixedUpdate`마다 `mapSpeed * fixedDeltaTime`을 차감 → 오브젝트 실제 이동량과 정확히 일치.
- 초과 이동분은 `LaneManager`의 필드 하나(`distanceRemain`)로 다음 대기에 이월 → 누적 오차 없음.
- 필드 이름을 단위에 맞게 변경, 기존 값은 `초 × mapSpeed(9)`로 환산해 재설정(현재 체감 간격 유지).

```csharp
float distanceRemain; // 남은 대기 거리. 음수 = 직전 대기에서 초과 이동한 거리(이월)

IEnumerator WaitDistance(float distance)
{
    distanceRemain += distance;
    while (distanceRemain > 0f)
    {
        yield return new WaitForFixedUpdate();
        distanceRemain -= GameManager.Instance.mapSpeed * Time.fixedDeltaTime;
    }
}
```

## Phase 1 — 코드
- [x] `CreateObjectSO.cs`: `patternIntervalTime` → `rowDistance` (기본 1.8), 툴팁 "패턴 내 각 줄 사이 거리 (유닛)", `ObjectSpawnDataSet.rows` 툴팁의 `patternIntervalTime` 언급 수정. CP949/CRLF 유지.
- [x] `LaneManager.cs`: `createInterval` → `patternDistance` (기본 9), `distanceRemain` 필드 + `WaitDistance` 추가, `WaitForSeconds` 2곳 교체. CP949/CRLF 유지.
- [x] MCP: 컴파일 대기 → 콘솔 에러 0 확인.

## Phase 2 — 데이터
- [x] `SampleScene` `LaneManager.patternDistance` = 9 (1s × 9)
- [x] `CreateObjectData3D.asset` `rowDistance` = 1.8 (0.2s × 9)
- [x] `CreateObjectData2D.asset` `rowDistance` = 0.9 (0.1s × 9)
- [x] 씬에 남은 옛 키 `createInterval`, 에셋의 `patternIntervalTime` 제거 확인(저장 시 자동 정리).
  - 메모: 저장 후 diff는 키 이름+값 1줄씩만 변경됨.

## Phase 3 — 검증
- [ ] 플레이 → 같은 레인 연속 오브젝트의 Z 간격이 `rowDistance`와 일치하는지 확인.
  - 메모: 플레이 모드가 진행되지 않아 미실시. 대신 편집 모드에서 실제 `WaitDistance`를 리플렉션으로 50회 실행(1.8 유닛, 25번째에 속도 9→10) → 누적 이동거리 오차 최대 0.18(1 물리 스텝 미만), 누적되지 않음 확인.
- [x] 무적 발동(속도 9→10) 중에도 간격 유지 확인.
  - 메모: 위 시뮬레이션으로 확인.
- [x] 콘솔 신규 에러/경고 0 (기존 Missing Script / `Ray does not intersect` 경고 제외).

## 의문점
- 무적 중엔 속도가 빨라져 **생성 빈도가 올라감**(간격은 동일). 이게 의도인지? 아니면 무적 중 스폰을 멈추거나 줄이길 원하는지?
- 환산 기준을 현재 `mapSpeed` 9로 잡았음. 다른 값을 원하면 알려줘.
