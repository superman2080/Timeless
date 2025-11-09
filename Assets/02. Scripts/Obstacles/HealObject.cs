using UnityEngine;

public class HealObject : InteractionObject
{
    [SerializeField] private float healAmount = 10;
    public float HealAmount => healAmount;

    protected override void Start()
    {
        base.Start();
        onTargetHitEvent += OnCollded;
    }

    void FixedUpdate()
    {
        ObstacleMovement();
    }

    protected virtual void ObstacleMovement()
    {
        Rb.MovePosition(transform.position + Vector3.back * GameManager.Instance.mapSpeed * Time.fixedDeltaTime);
    }



    private void OnCollded(ICollisionable collision)
    {
        if (collision is Player player)
        {
            if (player.IsJump)
                return;

            player.stat.Heal(HealAmount);
            gameObject.SetActive(false);    
        }
    }
}
