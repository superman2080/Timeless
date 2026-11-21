// 숨어 있는 상태. appearInterval(scaled time)이 지나면 첫 등장은 컷씬, 이후는 뒤에서 달려 들어옴
public class BossHiddenState : BossState
{
    private float timer;

    public BossHiddenState(Boss boss) : base(boss) { }

    public override void Enter()
    {
        boss.SetModelVisible(false);
        timer = 0f;
    }

    public override void Tick(float dt)
    {
        timer += dt;
        if (timer >= boss.appearInterval)
            boss.Machine.Change(boss.introPlayed ? boss.EnterState : boss.IntroState);
    }
}
