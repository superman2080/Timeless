using UnityEngine;

// 땅콩버섯 형태: 먹으면 HP를 현재 뷰 모드 구간의 비율로 맞추고 맵 이동 속도 감소. 맞으면 데미지 대신 기본 형태로 복귀
[CreateAssetMenu(fileName = "Form_", menuName = "Scriptable Objects/Player/Mini Form")]
public class MiniFormSO : PlayerFormSO
{
    [Tooltip("먹을 때 남길 HP: 현재 뷰 모드 HP 구간 안의 비율 (0.1 → 3D 55%, 2D 7%)")]
    [Range(0, 1)] public float hpRatioInView = 0.1f;
    [Tooltip("맵 이동 속도 감소 비율 (0.5 → 절반 속도)")]
    [Range(0, 0.9f)] public float mapSpeedReduction = 0.5f;

    public override void OnEnter(Player player)
    {
        var mode = GameManager.Instance.currentViewMode;
        float ratio = GameManager.Instance.laneManager.patternSet.ToGlobalRatio(mode, hpRatioInView);
        player.stat.SetHP(player.stat.MaxHP * ratio);
    }

    public override void ModifyStats(ref PlayerStats target)
    {
        base.ModifyStats(ref target);
        target.mapSpeedMultiplier = 1f - mapSpeedReduction;
    }

    public override float OnHit(Player player, float damage)
    {
        player.StateMachine.ChangeForm(player.DefaultForm);
        return 0f;
    }
}
