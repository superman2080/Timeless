using UnityEngine;

// 첫 등장 컷씬 (게임당 1회). 게임 정지 → Idle로 낙하 → 착지 이펙트 + 쉐이크 → Idle → Screaming → Walk 블렌드 시작 시 재개 후 추격
public class BossIntroState : BossState
{
    private enum Step { Lead, Fall, Hold }

    private static readonly int ScreamHash = Animator.StringToHash("Scream");
    private const string WalkState = "Walk";

    private Step step;
    private bool screamed;
    private float timer;
    private float fallSpeed;
    private float prevTimeScale;

    public BossIntroState(Boss boss) : base(boss) { }

    public override bool UseUnscaledTime => true;

    public override void Enter()
    {
        boss.introPlayed = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        boss.SetPosition(boss.groundY + boss.dropHeight, boss.ChaseZ);
        boss.SetModelVisible(true);
        boss.Animator.Play("Idle", 0, 0f);
        CameraManager.Instance.SetBossCamera(BossCamMode.Intro, 0f);

        step = Step.Lead;
        timer = 0f;
        fallSpeed = 0f;
        screamed = false;
    }

    public override void Tick(float dt)
    {
        timer += dt;
        switch (step)
        {
            case Step.Lead:
                if (timer >= boss.introLeadTime)
                    step = Step.Fall;
                break;

            case Step.Fall:
                fallSpeed += boss.fallGravity * dt;
                Vector3 pos = boss.transform.position;
                pos.y -= fallSpeed * dt;
                if (pos.y <= boss.groundY)
                {
                    pos.y = boss.groundY;
                    Land(pos);
                }
                boss.transform.position = pos;
                break;

            case Step.Hold:
                if (!screamed && timer >= boss.landIdleTime)
                {
                    boss.Animator.SetTrigger(ScreamHash);
                    SoundManager.Instance.PlaySFX(boss.roarSfx);
                    screamed = true;
                }
                // Screaming → Walk 블렌드가 시작되면 재개 (블렌드는 게임 재개 후에도 이어짐)
                if ((screamed && IsBlendingToWalk()) || timer >= boss.maxLandHold)
                    boss.Machine.Change(boss.ChaseState);
                break;
        }
    }

    private void Land(Vector3 pos)
    {
        (Pool<PooledEffect>.Instance as EffectPool)?.Get(boss.landEffect, pos);
        CameraManager.Instance.CameraShake(boss.landShakeAmplitude, boss.landShakeFrequency, boss.landShakeDuration, CameraShakeMode.DECREMENT, true);
        step = Step.Hold;
        timer = 0f;
    }

    private bool IsBlendingToWalk()
    {
        var anim = boss.Animator;
        return anim.GetCurrentAnimatorStateInfo(0).IsName(WalkState)
            || (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName(WalkState));
    }

    public override void Exit()
    {
        Time.timeScale = prevTimeScale;
    }
}
