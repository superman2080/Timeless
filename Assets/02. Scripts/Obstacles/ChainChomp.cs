using System.Collections;
using UnityEngine;

// 멍멍이. 생성 위치 뒤(+Z) 바닥의 말뚝에 사슬로 묶인 채 월드와 같이 흘러온다. 움직이는 건 머리뿐이며 루트 로컬 공간에서 이동한다.
// 같은 레인의 플레이어가 attackRange 안에 들어오면 즉시 플레이어 z까지 돌진하고, 아니면 앞쪽 랜덤 방향으로 위협 돌진을 반복한다.
// 점프로는 피할 수 없다(ignoreJumpedPlayer). 머리는 자기 레인 안에서만 움직이므로 다른 레인에 있으면 안전하다
public class ChainChomp : Obstacle
{
    [Header("Parts")]
    [Tooltip("움직이는 머리. +Z가 바라보는 방향")]
    [SerializeField] private Transform head;
    [Tooltip("사슬이 박힌 말뚝")]
    [SerializeField] private Transform anchor;
    [SerializeField] private ChainPhysics chain;

    [Header("Attack")]
    [Tooltip("같은 레인이고 머리와 플레이어의 z 거리가 이 값 이하면 돌진 (유닛)")]
    [Min(0f)]
    [SerializeField] private float attackRange = 4f;
    [Tooltip("돌진 속도 (루트 기준, 유닛/s)")]
    [SerializeField] private float lungeSpeed = 25f;
    [Tooltip("복귀 속도 (루트 기준, 유닛/s)")]
    [SerializeField] private float returnSpeed = 6f;
    [Tooltip("돌진 끝에서 멈춰 있는 시간(초)")]
    [SerializeField] private float lungeHold = 0.15f;
    [Tooltip("공격 후 복귀하고 다음 판정까지 대기(초)")]
    [SerializeField] private float attackCooldown = 0.6f;
    [Tooltip("말뚝에서 머리까지 최대 거리 = 사슬 길이 × 이 값 (사슬이 팽팽해지기 직전)")]
    [Range(0.5f, 1f)]
    [SerializeField] private float reachRatio = 0.9f;

    [Header("Threat")]
    [Tooltip("범위 밖일 때 위협 돌진 간격(초)")]
    [SerializeField] private float threatInterval = 1.2f;
    [Tooltip("정면(-Z) 기준 좌우 최대 각도")]
    [SerializeField] private float threatAngle = 35f;
    [SerializeField] private float threatDistance = 2.5f;
    [Tooltip("머리 x 이동 한계 = 레인 간격 × 이 값. 옆 레인을 침범하지 않도록")]
    [Range(0f, 0.5f)]
    [SerializeField] private float laneMargin = 0.3f;

    [Header("Idle")]
    [SerializeField] private float hopHeight = 0.15f;
    [Tooltip("초당 깡충 횟수")]
    [SerializeField] private float hopFrequency = 3f;
    [Tooltip("머리 회전 속도 (도/s)")]
    [SerializeField] private float turnSpeed = 720f;

    [Header("Collider")]
    [Tooltip("머리 피벗(바닥) 기준 콜라이더 중심 높이. 점프한 플레이어도 맞도록 캡슐을 세로로 길게 둔다")]
    [SerializeField] private float colliderHeight = 1.5f;
    [Tooltip("켜면 씬 뷰에서 공격 범위와 사슬 반경을 표시")]
    [SerializeField] private bool drawGizmo = true;

    private CapsuleCollider capsule;
    private Vector3 restPos;          // 머리 휴식 위치 (루트 로컬)
    private Vector3 headPos;          // 현재 머리 위치 (루트 로컬, 깡충 제외)
    private Vector3 facing = Vector3.back;
    private bool hopping;
    private Coroutine behaviourCor;

    private float MaxReach => chain != null ? chain.Length * reachRatio : float.MaxValue;

    // InteractionObject.Col은 Start에서 할당되어 첫 OnEnable 시점엔 null이므로 직접 캐싱
    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        restPos = head.localPosition;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 풀에서 재사용될 때 돌진 중이던 머리/사슬 모양이 남지 않도록 초기화
        headPos = restPos;
        facing = Vector3.back;
        hopping = true;
        head.localRotation = Quaternion.LookRotation(facing);
        ApplyHead();
        if (chain != null)
            chain.ResetChain();
        behaviourCor = StartCoroutine(BehaviourCoroutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (behaviourCor != null)
        {
            StopCoroutine(behaviourCor);
            behaviourCor = null;
        }
    }

    // 루트는 월드와 같이 흘러오고, 머리는 그 위에서 로컬로 움직인다
    protected override void ObstacleMovement()
    {
        base.ObstacleMovement();
        ApplyHead();
    }

    private void ApplyHead()
    {
        float hop = hopping ? Mathf.Abs(Mathf.Sin(Time.time * hopFrequency * Mathf.PI)) * hopHeight : 0f;
        Vector3 pos = headPos + Vector3.up * hop;
        head.localPosition = pos;
        head.localRotation = Quaternion.RotateTowards(head.localRotation, Quaternion.LookRotation(facing), turnSpeed * Time.fixedDeltaTime);
        // 판정은 루트 콜라이더 하나로 받으므로 중심을 머리에 맞춘다
        if (capsule != null)
            capsule.center = headPos + Vector3.up * colliderHeight;
    }

    // 대기 → (공격 | 위협) → 복귀 → 대기
    private IEnumerator BehaviourCoroutine()
    {
        yield return new WaitForFixedUpdate(); // 풀에서 위치가 세팅된 뒤부터 판정
        float nextThreat = Time.time + threatInterval;
        while (true)
        {
            if (CanAttack())
            {
                yield return AttackCoroutine();
                nextThreat = Time.time + threatInterval;
            }
            else if (Time.time >= nextThreat)
            {
                yield return ThreatCoroutine();
                nextThreat = Time.time + threatInterval;
            }
            else
            {
                FacePlayer();
                yield return new WaitForFixedUpdate();
            }
        }
    }

    // 목표 z를 매 스텝 플레이어 위치로 갱신한다 (월드가 흐르는 동안에도 정확히 플레이어까지).
    // 돌진 중 플레이어를 지나쳐도 휴식 위치 뒤로는 쫓아가지 않는다
    private IEnumerator AttackCoroutine()
    {
        hopping = false;
        while (!MoveHead(ClampReach(new Vector3(restPos.x, restPos.y, Mathf.Min(PlayerLocalZ(), restPos.z))), lungeSpeed, true))
            yield return new WaitForFixedUpdate();

        yield return new WaitForSeconds(lungeHold);
        yield return ReturnCoroutine();
        yield return new WaitForSeconds(attackCooldown);
    }

    // 앞쪽 랜덤 방향으로 돌진했다 돌아온다. 도중에 공격 조건이 되면 그 자리에서 공격으로 넘어간다
    private IEnumerator ThreatCoroutine()
    {
        hopping = false;
        float angle = Random.Range(-threatAngle, threatAngle) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle));
        Vector3 target = ClampReach(restPos + dir * threatDistance);

        while (!MoveHead(target, lungeSpeed, true))
        {
            if (CanAttack())
                yield break;
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(lungeHold);
        yield return ReturnCoroutine();
    }

    // 사슬에 끌려가듯 정면을 유지한 채 휴식 위치로 복귀
    private IEnumerator ReturnCoroutine()
    {
        while (!MoveHead(restPos, returnSpeed, false))
            yield return new WaitForFixedUpdate();
        hopping = true;
    }

    // 한 스텝 이동. 목표에 도달하면 true. 앞(-Z)으로 나갈 때만 이동 방향을 바라본다
    private bool MoveHead(Vector3 target, float speed, bool faceMove)
    {
        Vector3 next = Vector3.MoveTowards(headPos, target, speed * Time.fixedDeltaTime);
        Vector3 move = next - headPos;
        move.y = 0f;
        if (faceMove && move.z < -1e-3f)
            facing = move.normalized;
        headPos = next;
        return (headPos - target).sqrMagnitude < 1e-4f;
    }

    // 자기 레인 폭 안 + 사슬이 닿는 거리 안으로 제한
    private Vector3 ClampReach(Vector3 target)
    {
        float margin = GameManager.Instance.laneManager.laneInterval * laneMargin;
        target.x = Mathf.Clamp(target.x, restPos.x - margin, restPos.x + margin);

        Vector3 anchorPos = anchor.localPosition;
        anchorPos.y = target.y;
        Vector3 offset = target - anchorPos;
        if (offset.magnitude > MaxReach)
            target = anchorPos + offset.normalized * MaxReach;
        return target;
    }

    private bool CanAttack()
    {
        if (GameManager.Instance.currentViewMode == ViewMode.View2D)
            return false;

        var player = GameManager.Instance.player;
        if (player == null || player.currentLane != currentLane)
            return false;

        // 플레이어보다 앞(+Z)에 있을 때만 (지나친 뒤에는 돌진하지 않음)
        float deltaZ = head.position.z - player.transform.position.z;
        return deltaZ > 0f && deltaZ <= attackRange;
    }

    private float PlayerLocalZ() => GameManager.Instance.player.transform.position.z - transform.position.z;

    // 대기 중에는 플레이어 쪽을 바라보되 정면(-Z) 기준 threatAngle 안으로 제한한다.
    // 플레이어가 옆이나 뒤에 있으면 방향이 튀므로 정면 유지
    private void FacePlayer()
    {
        var player = GameManager.Instance.player;
        if (player == null)
            return;

        Vector3 dir = transform.InverseTransformDirection(player.transform.position - head.position);
        dir.y = 0f;
        if (dir.z > -0.5f)
        {
            facing = Vector3.back;
            return;
        }
        float angle = Mathf.Clamp(Mathf.Atan2(dir.x, -dir.z) * Mathf.Rad2Deg, -threatAngle, threatAngle) * Mathf.Deg2Rad;
        facing = new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle));
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo || head == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(head.position, head.position + Vector3.back * attackRange);
        if (anchor != null && chain != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(anchor.position, MaxReach);
        }
    }
}
