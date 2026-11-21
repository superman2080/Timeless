// 보스 FSM 상태 베이스. 공격 패턴 등 새 동작은 이 클래스를 상속해 상태로 추가한다
public abstract class BossState
{
    protected readonly Boss boss;

    protected BossState(Boss boss) => this.boss = boss;

    // true면 Time.timeScale을 무시하고 unscaled dt로 Tick (컷씬 정지 중)
    public virtual bool UseUnscaledTime => false;

    public virtual void Enter() { }
    public virtual void Tick(float dt) { }
    public virtual void Exit() { }
}
