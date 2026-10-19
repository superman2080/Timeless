using UnityEngine;

// 먹으면 플레이어에 형태/효과를 적용하는 아이템. 둘 중 하나만 채워도 됨
public class PowerUpItem : InteractionObject
{
    [Tooltip("먹으면 이 형태로 전환")]
    [SerializeField] private PlayerFormSO form;
    [Tooltip("먹으면 이 효과 추가 (이미 있으면 시간 갱신)")]
    [SerializeField] private PlayerEffectSO effect;
    [Tooltip("먹으면 HP 100% 회복")]
    [SerializeField] private bool fullHeal;
    [Tooltip("플레이어가 이 형태일 때 먹으면 효과 대신 점수")]
    [SerializeField] private PlayerFormSO scoreInsteadForm;
    [SerializeField, Min(0)] private int scoreInstead = 1000;
    public AudioClip pickupSFX;

    protected override void Start()
    {
        base.Start();
        onTargetHitEvent += OnCollided;
    }

    void FixedUpdate()
    {
        Rb.MovePosition(transform.position + Vector3.back * GameManager.Instance.mapSpeed * Time.fixedDeltaTime);
    }

    private void OnCollided(ICollisionable collision)
    {
        if (collision is Player player)
        {
            if (player.IsJump != isUpper)
                return;

            if (scoreInsteadForm != null && player.StateMachine.CurrentForm == scoreInsteadForm)
                FindAnyObjectByType<ScoreUI>()?.AddScore(scoreInstead);
            else
            {
                if (form != null)
                    player.StateMachine.ChangeForm(form);
                if (effect != null)
                    player.StateMachine.AddEffect(effect);
                // 형태 적용 후라 바뀐 최대 HP 기준으로 채움
                if (fullHeal)
                    player.stat.Heal(player.stat.MaxHP);
            }

            if (pickupSFX != null)
                AudioSource.PlayClipAtPoint(pickupSFX, player.transform.position, 0.5f);
            gameObject.SetActive(false);
        }
    }
}
