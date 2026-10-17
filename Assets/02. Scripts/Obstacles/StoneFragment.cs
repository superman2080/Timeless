using UnityEngine;

// Whomp가 자폭할 때 레인을 봉쇄하는 돌 파편. 바닥 아래에서 생성되어 솟아오른 뒤 일반 하단 장애물처럼 흘러온다
public class StoneFragment : Obstacle
{
    [Tooltip("솟아오르는 시간")]
    [SerializeField] private float riseDuration = 0.25f;
    [Tooltip("생성 시 바닥 아래로 내려가 있는 깊이")]
    [SerializeField] private float riseDepth = 1.2f;

    private float groundY;
    private float riseElapsed;
    private bool rising;

    // 지면 위치를 받아 바닥 아래에서 시작한다. 풀에서 꺼낸 직후(활성화 후)에 호출
    public void Rise(Vector3 groundPosition)
    {
        groundY = groundPosition.y;
        riseElapsed = 0f;
        rising = true;
        transform.position = new Vector3(groundPosition.x, groundY - riseDepth, groundPosition.z);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        rising = false;
    }

    protected override void ObstacleMovement()
    {
        if (!rising)
        {
            base.ObstacleMovement();
            return;
        }

        // 스크롤과 상승을 한 번의 MovePosition으로 합성
        riseElapsed += Time.fixedDeltaTime;
        float t = riseDuration <= 0f ? 1f : Mathf.Clamp01(riseElapsed / riseDuration);
        Vector3 pos = transform.position + Vector3.back * GameManager.Instance.mapSpeed * Time.fixedDeltaTime;
        pos.y = Mathf.Lerp(groundY - riseDepth, groundY, t);
        Rb.MovePosition(pos);

        if (t >= 1f)
            rising = false;
    }
}
