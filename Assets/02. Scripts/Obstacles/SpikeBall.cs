using UnityEngine;

// ZombieBee가 던지는 공. 포물선으로 떨어진 뒤 일반 하단 장애물처럼 흘러옴
public class SpikeBall : Obstacle
{
    [SerializeField] private float flightTime = 0.7f;
    [Tooltip("시작점과 착지점을 잇는 직선 위로 더해지는 정점 높이")]
    [SerializeField] private float arcHeight = 1.5f;

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
