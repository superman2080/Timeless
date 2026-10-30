using UnityEngine;

// P스위치 효과: 먹으면 현재 뷰를 반대로 전환. 지속 중엔 lockView로 HP 임계값 전환을 막고, 끝나면 Player가 HP 기준으로 재검사
[CreateAssetMenu(fileName = "Effect_", menuName = "Scriptable Objects/Player/P-Switch Effect")]
public class PSwitchEffectSO : PlayerEffectSO
{
    public override void OnEnter(Player player)
    {
        var mode = GameManager.Instance.currentViewMode;
        player.SwitchView(mode == ViewMode.View3D ? ViewMode.View2D : ViewMode.View3D);
    }
}
