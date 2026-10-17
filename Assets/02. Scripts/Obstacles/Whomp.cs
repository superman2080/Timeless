using System.Collections;
using UnityEngine;

// 꽈당꽈당. 레인에 서서 흘러오다 플레이어가 같은 레인에 진입하면 카메라 방향으로 90° 넘어지며 자폭한다.
// 자폭 시 그 레인의 모든 오브젝트를 파괴하고, 앞/뒤로 돌 파편을 깔아 레인 진입을 봉쇄한다.
// 본체에는 피격 판정이 없다 (Obstacle을 상속하지 않음)
public class Whomp : InteractionObject
{
    // 동시 1마리 제한용 (LaneManager에서 확인)
    public static int ActiveCount { get; private set; }

    [Header("Trigger")]
    [Tooltip("발동 거리(유닛). 플레이어가 같은 레인에 있고 플레이어와의 z 거리가 이 값 이하가 되면 발동")]
    [Range(2f, 20f)]
    [SerializeField] private float triggerDistance = 20f;
    [Tooltip("켜면 씬 뷰에서 발동 구간을 선으로 표시")]
    [SerializeField] private bool drawTriggerGizmo = true;

    [Header("Fall")]
    [Tooltip("90° 넘어지는 시간")]
    [SerializeField] private float fallDuration = 0.45f;
    [SerializeField] private float fallAngle = 90f;
    [Tooltip("넘어지는 회전축. -X 기준 +90°면 윗면이 카메라 쪽(-Z)으로 넘어간다")]
    [SerializeField] private Vector3 fallAxis = Vector3.left;
    [Tooltip("넘어진 뒤 숨길 모델 루트")]
    [SerializeField] private Transform model;

    [Header("Fragments")]
    [Tooltip("한쪽 방향 파편 개수. 앞/뒤 각각 이 개수만큼 생성되어 총 2배가 된다")]
    [SerializeField] private int fragmentCount = 5;
    [Tooltip("파편 간 z 간격 (유닛)")]
    [SerializeField] private float fragmentSpacing = 1.5f;
    [Tooltip("파편 전체 생성에 걸리는 총 시간(초). 단계 간격 = 이 값 / fragmentCount")]
    [SerializeField] private float fragmentSpawnDuration = 0.1f;

    [Header("Effect")]
    [SerializeField] private EffectType bodyEffect = EffectType.DUST_EXPLOSION;
    [SerializeField] private EffectType fragmentEffect = EffectType.TINY_EXPLOSION;

    private bool triggered;
    private Coroutine eruptCor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveCount = 0; // 도메인 리로드 꺼져 있음

    protected override void OnEnable()
    {
        base.OnEnable();
        ActiveCount++;
        triggered = false;
        // 풀에서 재사용될 때 넘어진 회전이 남지 않도록 초기화
        transform.rotation = Quaternion.identity;
        if (model != null)
            model.gameObject.SetActive(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ActiveCount--;
        if (eruptCor != null)
        {
            StopCoroutine(eruptCor);
            eruptCor = null;
        }
    }

    private void FixedUpdate()
    {
        // 발동 중에도 월드와 같이 흘러온다
        Rb.MovePosition(transform.position + Vector3.back * GameManager.Instance.mapSpeed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        if (triggered || !CanTrigger())
            return;

        triggered = true;
        eruptCor = StartCoroutine(EruptCoroutine());
    }

    private bool CanTrigger()
    {
        if (GameManager.Instance.currentViewMode == ViewMode.View2D)
            return false;

        var player = GameManager.Instance.player;
        if (player == null || player.currentLane != currentLane)
            return false;

        // 플레이어보다 앞(+Z)에 있을 때만 발동한다 (뒤에서 터지면 파편이 의미가 없음)
        float deltaZ = transform.position.z - player.transform.position.z;
        return deltaZ > 0f && deltaZ <= triggerDistance;
    }

    // 발동 구간(플레이어 ~ triggerDistance)을 씬 뷰에 표시
    private void OnDrawGizmosSelected()
    {
        if (!drawTriggerGizmo || !Application.isPlaying)
            return;

        var player = GameManager.Instance.player;
        if (player == null)
            return;

        float x = transform.position.x;
        float y = transform.position.y;
        float near = player.transform.position.z;
        float far = player.transform.position.z + triggerDistance;
        Gizmos.color = triggered ? Color.red : Color.yellow;
        Gizmos.DrawLine(new Vector3(x, y, near), new Vector3(x, y, far));
        Gizmos.DrawWireCube(new Vector3(x, y, near), new Vector3(0.6f, 0.1f, 0.05f));
        Gizmos.DrawWireCube(new Vector3(x, y, far), new Vector3(0.6f, 0.1f, 0.05f));
    }

    // 넘어짐 → 본체 폭발 + 레인 청소 → 가까운 쪽부터 먼 쪽까지 파편 순차 생성 → 비활성화
    private IEnumerator EruptCoroutine()
    {
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.AngleAxis(fallAngle, fallAxis);
        for (float elapsed = 0f; elapsed < fallDuration; elapsed += Time.fixedDeltaTime)
        {
            Rb.MoveRotation(Quaternion.Lerp(start, end, elapsed / fallDuration));
            yield return new WaitForFixedUpdate();
        }
        Rb.MoveRotation(end);

        PlayEffect(bodyEffect, transform.position);
        if (model != null)
            model.gameObject.SetActive(false);
        ClearLane();

        yield return SpawnFragmentsCoroutine();

        eruptCor = null;
        gameObject.SetActive(false);
    }

    // 같은 레인 인덱스의 오브젝트를 상단/하단 구분 없이 전부 회수
    private void ClearLane()
    {
        foreach (var obj in (Pool<InteractionObject>.Instance as InteractionObjectPool).GetChildList(false))
        {
            if (obj == this || obj is Player || obj.currentLane != currentLane)
                continue;
            obj.gameObject.SetActive(false);
        }
    }

    // 단계 간격은 fragmentSpawnDuration / fragmentCount. 프레임보다 짧아도 순서가 밀리지 않도록 누적 시간으로 판정
    private IEnumerator SpawnFragmentsCoroutine()
    {
        if (fragmentCount <= 0)
            yield break;

        var lane = GetLane();
        if (lane == null)
            yield break;

        float step = fragmentSpawnDuration / fragmentCount;
        float start = Time.time;

        for (int i = 0; i < fragmentCount; i++)
        {
            float due = start + step * i;
            while (Time.time < due)
                yield return null;

            // 생성 시점의 자기 z를 앵커로 쓰면 월드 스크롤이 자동으로 보정된다
            float anchorZ = transform.position.z;
            float offset = (i + 1) * fragmentSpacing;
            SpawnFragment(lane, anchorZ - offset); // 앞 (플레이어 쪽)
            SpawnFragment(lane, anchorZ + offset); // 뒤
        }
    }

    private void SpawnFragment(Lane lane, float z)
    {
        Vector3 groundPos = new Vector3(lane.LaneX, lane.transform.position.y, z);
        PlayEffect(fragmentEffect, groundPos);

        // Lane을 거쳐 생성해야 2D 전환 시 함께 회수된다
        if (lane.GenerateObstacle(ObjectType.STONE_FRAGMENT, groundPos) is StoneFragment fragment)
            fragment.Rise(groundPos);
    }

    private Lane GetLane()
    {
        var lanes = GameManager.Instance.laneManager.lanes;
        return currentLane >= 0 && currentLane < lanes.Length ? lanes[currentLane] : null;
    }

    private void PlayEffect(EffectType type, Vector3 position)
    {
        if (type == EffectType.NONE)
            return;
        (Pool<PooledEffect>.Instance as EffectPool)?.Get(type, position);
    }
}
