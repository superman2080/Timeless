using UnityEngine;

// 점박문어. 플레이어가 splashDistance 안으로 들어오면 화면에 먹물을 뿌려 시야를 가리고, 부딪히면 화면이 잠시 암전된다.
// 이동/피격/기본 연출은 Obstacle을 그대로 쓴다 (하단 레인, 점프로 회피 가능)
public class Octoomba : Obstacle
{
    [Tooltip("플레이어와의 z 거리가 이 값 이하가 되면 먹물을 뿌린다 (유닛, 활성화당 1회)")]
    [Min(0f)]
    [SerializeField] private float splashDistance = 8f;

    private bool splashed;

    // 풀이 새로 Instantiate할 때는 같은 프레임에 비활성화되므로 Update가 돌지 않아 오발동하지 않는다
    protected override void OnEnable()
    {
        base.OnEnable();
        splashed = false;
    }

    private void Update()
    {
        if (splashed)
            return;

        // 플레이어는 z 고정이고 월드가 -Z로 흐르므로 z 갭으로 거리를 잰다
        var player = GameManager.Instance.player;
        if (player == null || transform.position.z - player.transform.position.z > splashDistance)
            return;

        splashed = true;
        if (OctoombaScreenEffect.Instance != null)
            OctoombaScreenEffect.Instance.SplashInk();
    }

    protected override void OnHitPlayer(Player player)
    {
        if (OctoombaScreenEffect.Instance != null)
            OctoombaScreenEffect.Instance.Blackout();
    }
}
