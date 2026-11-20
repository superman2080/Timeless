// 두 번째 등장부터: 플레이어 뒤(화면 밖)에서 추격 위치까지 달려 들어옴. 3D면 보스 카메라로 전환
public class BossEnterState : BossState
{
    public BossEnterState(Boss boss) : base(boss) { }

    public override void Enter()
    {
        boss.SetPosition(boss.groundY, boss.spawnZ);
        boss.SetModelVisible(true);
        CameraManager.Instance.SetBossCamera(BossCamMode.Chase, boss.chaseBlend);
    }

    public override void Tick(float dt)
    {
        if (boss.MoveTowardsZ(boss.ChaseZ, boss.enterSpeed, dt))
            boss.Machine.Change(boss.ChaseState);
    }
}
