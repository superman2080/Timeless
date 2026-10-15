using UnityEngine;

// 형태/효과 공통 베이스. 아이템 고유 규칙은 상속해서 훅을 오버라이드
// SO 에셋은 공유되므로 상태별 런타임 값은 Player 쪽에 두거나 Player를 키로 관리할 것
public abstract class PlayerStateSO : ScriptableObject
{
    public virtual void OnEnter(Player player) { }
    public virtual void OnExit(Player player) { }
    public virtual void OnUpdate(Player player, float deltaTime) { }

    // 받을 데미지를 바꿔 반환 (0 이하 = 무시)
    public virtual float OnHit(Player player, float damage) => damage;

    public abstract void ModifyStats(ref PlayerStats stats);
}
