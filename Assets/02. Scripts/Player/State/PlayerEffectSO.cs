using UnityEngine;

// 효과: 형태와 별개로 여러 개 겹침. duration이 지나면 자동 종료
[CreateAssetMenu(fileName = "Effect_", menuName = "Scriptable Objects/Player/Effect")]
public class PlayerEffectSO : PlayerStateSO
{
    [Tooltip("지속 시간(초). 0 이하 = 직접 제거할 때까지 유지")]
    public float duration = 5f;

    [Header("Stat Modifiers (형태 스탯에 곱함)")]
    public float scaleMultiplier = 1f;
    public float maxHPMultiplier = 1f;
    public float mapSpeedMultiplier = 1f;
    public float jumpHeightMultiplier = 1f;
    public float airTimeMultiplier = 1f;
    public float hpDecrementMultiplier = 1f;
    public bool invincible;
    [Tooltip("켜져 있는 동안 HP 임계값에 의한 3D/2D 전환 막음")]
    public bool lockView;

    public override void ModifyStats(ref PlayerStats stats)
    {
        stats.scale *= scaleMultiplier;
        stats.maxHP *= maxHPMultiplier;
        stats.mapSpeedMultiplier *= mapSpeedMultiplier;
        stats.jumpHeightMultiplier *= jumpHeightMultiplier;
        stats.airTimeMultiplier *= airTimeMultiplier;
        stats.hpDecrementMultiplier *= hpDecrementMultiplier;
        stats.invincible |= invincible;
        stats.lockView |= lockView;
    }
}
