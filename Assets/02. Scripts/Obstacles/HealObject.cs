using UnityEngine;

public class HealObject : InteractionObject
{
    [SerializeField] private float healAmount = 10;
    public float HealAmount => healAmount;
    public AudioClip healSFX;

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

            AudioSource.PlayClipAtPoint(healSFX, player.transform.position, 10000f);
            player.stat.Heal(HealAmount);
            gameObject.SetActive(false);    
        }
    }
}
