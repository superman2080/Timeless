public class BossStateMachine
{
    public BossState Current { get; private set; }

    public void Change(BossState next)
    {
        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    public void Tick(float dt) => Current?.Tick(dt);
}
