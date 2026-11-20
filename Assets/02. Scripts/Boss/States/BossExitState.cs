// 퇴장: -Z로 가속하며 멀어짐. exitZ를 지나면 기본 카메라로 복귀 후 숨김
public class BossExitState : BossState
{
    private float speed;

    public BossExitState(Boss boss) : base(boss) { }

    public override void Enter() => speed = 0f;

    public override void Tick(float dt)
    {
        speed += boss.exitAccel * dt;
        if (boss.MoveTowardsZ(boss.exitZ, speed, dt))
        {
            CameraManager.Instance.SetBossCamera(BossCamMode.None, boss.exitBlend);
            boss.Machine.Change(boss.HiddenState);
        }
    }
}
