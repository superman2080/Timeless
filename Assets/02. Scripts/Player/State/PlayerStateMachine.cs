using System;
using System.Collections.Generic;

// 형태(1개) + 효과(여러 개, 시간제)를 관리하고 최종 스탯을 합성
public class PlayerStateMachine
{
    private class ActiveEffect
    {
        public PlayerEffectSO effect;
        public float remaining;
    }

    private readonly Player owner;
    private readonly List<ActiveEffect> effects = new List<ActiveEffect>();

    public PlayerFormSO CurrentForm { get; private set; }
    public PlayerStats Stats { get; private set; } = PlayerStats.Default;
    public event Action<PlayerStats> onStatsChanged;

    public PlayerStateMachine(Player owner)
    {
        this.owner = owner;
    }

    public void ChangeForm(PlayerFormSO form)
    {
        if (form == CurrentForm)
            return;

        CurrentForm?.OnExit(owner);
        CurrentForm = form;
        CurrentForm?.OnEnter(owner);
        RecalculateStats();
    }

    // 이미 있는 효과면 남은 시간만 갱신
    public void AddEffect(PlayerEffectSO effect)
    {
        if (effect == null)
            return;

        var active = effects.Find(e => e.effect == effect);
        if (active != null)
        {
            active.remaining = effect.duration;
            return;
        }

        effects.Add(new ActiveEffect { effect = effect, remaining = effect.duration });
        effect.OnEnter(owner);
        RecalculateStats();
    }

    public void RemoveEffect(PlayerEffectSO effect)
    {
        int index = effects.FindIndex(e => e.effect == effect);
        if (index < 0)
            return;

        effects.RemoveAt(index);
        effect.OnExit(owner);
        RecalculateStats();
    }

    public bool HasEffect(PlayerEffectSO effect) => effects.Exists(e => e.effect == effect);

    public float GetRemainingTime(PlayerEffectSO effect) => effects.Find(e => e.effect == effect)?.remaining ?? 0f;

    public void Tick(float deltaTime)
    {
        CurrentForm?.OnUpdate(owner, deltaTime);

        // 훅 안에서 효과가 추가/제거될 수 있으므로 복사본으로 순회
        foreach (var active in effects.ToArray())
        {
            active.effect.OnUpdate(owner, deltaTime);

            if (active.effect.duration <= 0f)
                continue;
            active.remaining -= deltaTime;
            if (active.remaining <= 0f)
                RemoveEffect(active.effect);
        }
    }

    // 효과 → 형태 순으로 OnHit 통과. 남은 데미지 반환
    public float ProcessHit(float damage)
    {
        foreach (var active in effects.ToArray())
            damage = active.effect.OnHit(owner, damage);
        if (CurrentForm != null)
            damage = CurrentForm.OnHit(owner, damage);
        return Math.Max(0f, damage);
    }

    // 상태가 런타임에 스탯을 바꿨을 때 직접 호출
    public void RecalculateStats()
    {
        var stats = PlayerStats.Default;
        CurrentForm?.ModifyStats(ref stats);
        foreach (var active in effects)
            active.effect.ModifyStats(ref stats);

        Stats = stats;
        onStatsChanged?.Invoke(stats);
    }
}
