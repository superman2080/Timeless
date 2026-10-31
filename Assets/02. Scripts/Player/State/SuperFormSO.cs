using UnityEngine;

// 슈퍼 버섯 형태: 맞으면 데미지 대신 기본 형태로 작아짐
[CreateAssetMenu(fileName = "Form_", menuName = "Scriptable Objects/Player/Super Form")]
public class SuperFormSO : PlayerFormSO
{
    public override float OnHit(Player player, float damage)
    {
        player.StateMachine.ChangeForm(player.DefaultForm);
        return 0f;
    }
}
