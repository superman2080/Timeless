using UnityEngine;

// ZombieBee가 던지는 공. 포물선으로 떨어진 뒤 일반 하단 장애물처럼 흘러옴
public class SpikeBall : Obstacle
{
    [SerializeField] private float flightTime = 0.7f;
    [Tooltip("시작점과 착지점을 잇는 직선 위로 더해지는 정점 높이")]
    [SerializeField] private float arcHeight = 1.5f;
    [Tooltip("굴러가는 연출을 줄 모델 (피벗이 공 중심)")]
    [SerializeField] private Transform model;
    [Tooltip("굴림 반지름. 월드 속도 / 반지름 = 회전 속도")]
    [SerializeField] private float rollRadius = 0.24f;

    private Vector3 from, to;
    private float elapsed;
    private bool landed = true;

    public void Throw(Vector3 start, Vector3 landing)
    {
        from = start;
        to = landing;
        elapsed = 0f;
        landed = false;
        transform.position = start;
    }

    // 비행 중에도 계속 플레이어 쪽(-Z)으로 굴러오는 것처럼 회전 (+X축 음의 방향 = 윗면이 -Z로 감)
    private void Update()
    {
        float deg = GameManager.Instance.mapSpeed / rollRadius * Mathf.Rad2Deg * Time.deltaTime;
        model.Rotate(Vector3.right, -deg, Space.World);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        landed = true;
    }

    protected override void ObstacleMovement()
    {
        if (landed)
        {
            base.ObstacleMovement();
            return;
        }

        // 비행 중에는 월드 스크롤 없이 던진 벌(화면 고정) 기준으로 날아가고, 착지 후부터 흘러옴
        elapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed / flightTime);
        Rb.MovePosition(Vector3.Lerp(from, to, t) + Vector3.up * (arcHeight * 4f * t * (1f - t)));
        if (t >= 1f)
            landed = true;
    }
}
