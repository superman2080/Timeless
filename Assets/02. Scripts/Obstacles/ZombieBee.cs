using System.Collections;
using UnityEngine;

// 김수한무. 패턴으로 생성된 뒤 플레이어 앞 상공에서 좌우로 유영하다 랜덤 레인 위로 이동해 스파이크 볼을 던지고, lifeTime 후 퇴장
public class ZombieBee : InteractionObject
{
    // 동시 1마리 제한용 (LaneManager에서 확인)
    public static int ActiveCount { get; private set; }

    [Header("Move")]
    [SerializeField] private float hoverZ = -6f; // 3D 카메라 화면 상단(위쪽 끝이 수평 아래 10°)에 몸 전체가 보이는 위치
    [Tooltip("상단 레인 높이 + 이 값에 떠 있음")]
    [SerializeField] private float hoverOffset = 0.5f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float bobAmplitude = 0.1f;
    [Tooltip("유영 좌우 폭 (중앙 기준, 1 = 양끝 레인)")]
    [SerializeField] private float swayAmplitude = 1f;
    [SerializeField] private float swayPeriod = 2f;
    [Tooltip("x 이동 가감속 시간")]
    [SerializeField] private float xSmoothTime = 0.12f;
    [Tooltip("이 x 속도에서 최대로 기울어짐")]
    [SerializeField] private float tiltSpeed = 3f;
    [SerializeField] private float tiltAngle = 10f;

    [Header("Attack")]
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float attackInterval = 2f;
    [Tooltip("Attack 트리거 후 공이 손을 떠나는 시간 (클립 2.08s / 스테이트 속도 2.958)")]
    [SerializeField] private float releaseDelay = 0.7f;
    [Tooltip("레인 선택 후 Attack 트리거까지 이동 시간")]
    [SerializeField] private float laneMoveTime = 0.3f;
    [SerializeField] private float exitTime = 1.5f;
    [Tooltip("벌 z 기준 착지 지점 오프셋 (음수 = 플레이어 쪽)")]
    [SerializeField] private float landingOffsetZ = -1f;
    [SerializeField] private Transform ballSocket;
    [SerializeField] private Animator animator;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private Vector3 target;
    private Lane attackLane; // 다음에 공을 던질 레인. null이면 좌우 유영
    private float xVelocity;
    private float swayStart;
    private bool exiting;
    private Coroutine lifeCor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveCount = 0; // 도메인 리로드 꺼져 있음

    protected override void OnEnable()
    {
        base.OnEnable();
        ActiveCount++;
        exiting = false;
        attackLane = null;
        xVelocity = 0f;
        swayStart = Time.time;
        lifeCor = StartCoroutine(LifeCoroutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ActiveCount--;
        if (lifeCor != null)
        {
            StopCoroutine(lifeCor);
            lifeCor = null;
        }
    }

    private void FixedUpdate()
    {
        var laneManager = GameManager.Instance.laneManager;
        if (!exiting)
        {
            target.x = attackLane != null
                ? attackLane.LaneX
                : swayAmplitude * Mathf.Sin((Time.time - swayStart) * 2f * Mathf.PI / swayPeriod);
            target.y = laneManager.upperLaneHeight + hoverOffset + Mathf.Sin(Time.time * 2f) * bobAmplitude;
            target.z = hoverZ;
        }
        Vector3 pos = Vector3.MoveTowards(Rb.position, target, moveSpeed * Time.fixedDeltaTime);
        pos.x = Mathf.SmoothDamp(Rb.position.x, target.x, ref xVelocity, xSmoothTime, moveSpeed, Time.fixedDeltaTime);
        Rb.MovePosition(pos);
        // 이동 방향으로 몸을 기울임 (+x 이동 → 오른쪽으로 기울도록 -z 회전)
        Rb.MoveRotation(Quaternion.Euler(0f, 0f, -Mathf.Clamp(xVelocity / tiltSpeed, -1f, 1f) * tiltAngle));
    }

    // 사이클(attackInterval): 유영 → release - releaseDelay - laneMoveTime에 레인 선택·이동 → releaseDelay 전에 Attack 트리거 → 투척 후 다시 유영.
    // 수명은 진입(호버 z 도착) 후부터 계산
    private IEnumerator LifeCoroutine()
    {
        yield return new WaitForFixedUpdate(); // 풀에서 위치가 세팅된 뒤부터 판정
        yield return new WaitUntil(() => Mathf.Abs(Rb.position.z - hoverZ) < 0.1f);
        float start = Time.time;
        for (float release = attackInterval; release <= lifeTime; release += attackInterval)
        {
            yield return new WaitUntil(() => Time.time >= start + release - releaseDelay - laneMoveTime);
            PickAttackLane();
            yield return new WaitUntil(() => Time.time >= start + release - releaseDelay);
            animator.SetTrigger(AttackHash);
            yield return new WaitUntil(() => Time.time >= start + release);
            ThrowBall();
            ResumeSway();
        }
        yield return new WaitUntil(() => Time.time >= start + lifeTime);

        exiting = true;
        target = transform.position + new Vector3(0f, 4f, 8f);
        yield return new WaitForSeconds(exitTime);
        lifeCor = null;
        gameObject.SetActive(false);
    }

    private void ThrowBall()
    {
        if (GameManager.Instance.currentViewMode == ViewMode.View2D)
            return;

        var lane = attackLane;
        lane.goombaOnlyUntil = Time.time + attackInterval;

        Vector3 start = ballSocket.position;
        if (lane.GenerateObstacle(ObjectType.SPIKE_BALL, start) is SpikeBall ball)
            ball.Throw(start, new Vector3(lane.LaneX, 0f, transform.position.z + landingOffsetZ));
    }

    // 현재 x에서 사인 곡선이 이어지도록 위상을 맞춰 유영 재개 (목표가 튀어 급이동하는 것 방지)
    private void ResumeSway()
    {
        attackLane = null;
        float phase = Mathf.Asin(Mathf.Clamp(Rb.position.x / swayAmplitude, -1f, 1f));
        swayStart = Time.time - phase * swayPeriod / (2f * Mathf.PI);
    }

    private void PickAttackLane()
    {
        var lanes = GameManager.Instance.laneManager.lanes;
        attackLane = lanes[Random.Range(0, lanes.Length)];
    }
}
