using System.Collections;
using UnityEngine;

public class Obstacle : InteractionObject
{
    [SerializeField] private float damageAmount = 10;
    public float DamageAmount => damageAmount;
    public bool ignoreJumpedPlayer = false;
    public AudioClip hitSFX;


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
        if(collision is Player player)
        {
            // 상단 오브젝트는 점프 중에만, 하단은 지상(또는 ignoreJumpedPlayer)일 때만 판정
            bool hit = isUpper ? player.IsJump : (ignoreJumpedPlayer || !player.IsJump);
            if (!hit || player.stat.isInvincible)
                return;
            CameraManager.Instance.FadeGlitch(0.5f, 0.01f, 0.2f);
            CameraManager.Instance.CameraShake(0.3f, 5f, 0.2f, CameraShakeMode.DECREMENT);
            AudioSource.PlayClipAtPoint(hitSFX, player.transform.position, 0.5f);
            player.stat.TakeDamage(DamageAmount);
        }
    }

}
