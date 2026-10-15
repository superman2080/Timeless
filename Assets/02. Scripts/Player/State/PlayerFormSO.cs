using UnityEngine;

// 형태: 한 번에 하나. 기본 스탯(절대값)을 정함
[CreateAssetMenu(fileName = "Form_", menuName = "Scriptable Objects/Player/Form")]
public class PlayerFormSO : PlayerStateSO
{
    public PlayerStats stats = PlayerStats.Default;

    public override void ModifyStats(ref PlayerStats target) => target = stats;
}
