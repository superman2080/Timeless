// 추격: 추격 위치를 유지하며 플레이어 X를 따라감. 뷰 전환 시 Z도 뷰별 위치로 이동
public class BossChaseState : BossState
{
    private float timer;

    public BossChaseState(Boss boss) : base(boss) { }

    public override void Enter()
    {
        timer = 0f;
        // 컷씬에서 넘어온 경우 추격 카메라로 블렌드 (Enter에서 넘어온 경우는 이미 Chase라 변화 없음)
        CameraManager.Instance.SetBossCamera(BossCamMode.Chase, boss.chaseBlend);
    }

    public override void Tick(float dt)
    {
        boss.MoveTowardsZ(boss.ChaseZ, boss.enterSpeed, dt);

        timer += dt;
        if (timer >= boss.stayDuration)
            boss.Machine.Change(boss.ExitState);
    }
}
