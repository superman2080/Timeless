using UnityEngine;

// 양 끝(start, end)이 고정된 사슬. Verlet 로프로 처짐을 계산해 고리 Transform을 구간마다 배치한다.
// 월드가 -Z로 흘러도 관성으로 뒤로 끌리지 않도록 이 오브젝트의 로컬 공간에서 시뮬레이션한다
public class ChainPhysics : MonoBehaviour
{
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;
    [Tooltip("고리 모델 (start 쪽부터). 고리 수 = 구간 수")]
    [SerializeField] private Transform[] links;

    [Header("Simulation")]
    [Tooltip("사슬 전체 길이 (유닛). start~end 거리가 이보다 짧으면 처진다")]
    [Min(0.01f)]
    [SerializeField] private float length = 6f;
    [SerializeField] private float gravity = 9.81f;
    [Tooltip("프레임마다 남는 속도 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float damping = 0.95f;
    [Tooltip("거리 제약 반복 횟수. 높을수록 덜 늘어난다")]
    [Min(1)]
    [SerializeField] private int iterations = 10;
    [Tooltip("바닥 높이 (이 오브젝트 로컬 y)")]
    [SerializeField] private float groundY = 0f;
    [Tooltip("고리 두께 반경. 바닥 아래로 이만큼은 안 꺼진다")]
    [SerializeField] private float linkRadius = 0.06f;
    [Tooltip("홀/짝 고리를 번갈아 비트는 각도")]
    [SerializeField] private float twistAngle = 90f;

    private Vector3[] points;
    private Vector3[] prevPoints;

    public float Length => length;

    private void OnEnable()
    {
        ResetChain();
    }

    // start~end를 직선으로 잇고 속도를 0으로 초기화 (풀 재사용 시 이전 모양이 남지 않도록)
    public void ResetChain()
    {
        if (start == null || end == null || links == null || links.Length == 0)
            return;

        int count = links.Length + 1;
        if (points == null || points.Length != count)
        {
            points = new Vector3[count];
            prevPoints = new Vector3[count];
        }

        Vector3 a = transform.InverseTransformPoint(start.position);
        Vector3 b = transform.InverseTransformPoint(end.position);
        for (int i = 0; i < count; i++)
        {
            points[i] = Vector3.Lerp(a, b, (float)i / (count - 1));
            prevPoints[i] = points[i];
        }
        ApplyLinks();
    }

    // 머리 이동(FixedUpdate)이 끝난 뒤 따라가도록 LateUpdate에서 계산
    private void LateUpdate()
    {
        if (points == null)
            return;

        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        int last = points.Length - 1;
        Vector3 accel = transform.InverseTransformDirection(Vector3.down) * gravity * dt * dt;

        points[0] = transform.InverseTransformPoint(start.position);
        points[last] = transform.InverseTransformPoint(end.position);

        for (int i = 1; i < last; i++)
        {
            Vector3 velocity = (points[i] - prevPoints[i]) * damping;
            prevPoints[i] = points[i];
            points[i] += velocity + accel;
        }

        // 늘어난 구간만 당긴다 (줄어든 구간은 그대로 둬서 느슨하게 처지게)
        float segment = length / last;
        for (int k = 0; k < iterations; k++)
        {
            for (int i = 0; i < last; i++)
            {
                Vector3 delta = points[i + 1] - points[i];
                float dist = delta.magnitude;
                if (dist <= segment || dist < 1e-5f)
                    continue;

                Vector3 correction = delta * ((dist - segment) / dist);
                if (i == 0)
                    points[i + 1] -= correction;
                else if (i + 1 == last)
                    points[i] += correction;
                else
                {
                    points[i] += correction * 0.5f;
                    points[i + 1] -= correction * 0.5f;
                }
            }

            for (int i = 1; i < last; i++)
                points[i].y = Mathf.Max(points[i].y, groundY + linkRadius);
        }

        ApplyLinks();
    }

    // 고리 i는 점 i ~ i+1 구간의 중점에, 캡슐 축(Y)이 구간 방향을 향하도록 배치
    private void ApplyLinks()
    {
        for (int i = 0; i < links.Length; i++)
        {
            if (links[i] == null)
                continue;

            Vector3 a = transform.TransformPoint(points[i]);
            Vector3 b = transform.TransformPoint(points[i + 1]);
            links[i].position = (a + b) * 0.5f;

            Vector3 dir = b - a;
            if (dir.sqrMagnitude < 1e-8f)
                continue;
            Vector3 up = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            links[i].rotation = Quaternion.LookRotation(dir, up) * Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, i % 2 == 0 ? 0f : twistAngle, 0f);
        }
    }
}
