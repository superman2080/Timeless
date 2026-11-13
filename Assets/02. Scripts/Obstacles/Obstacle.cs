using System.Collections;
using UnityEngine;

public class Obstacle : InteractionObject
{
    [SerializeField] private float damageAmount = 10;
    public float DamageAmount => damageAmount;
    public bool ignoreJumpedPlayer = false;
    public AudioClip hitSFX;

    [Header("Knockback (무적 플레이어에게 부딪혔을 때)")]
    [Tooltip("옆으로 튕기는 속도 (3D: 왼쪽 레인은 왼쪽, 오른쪽 레인은 오른쪽, 가운데는 0 / 2D: 3방향 랜덤)")]
    [SerializeField] private float knockSideSpeed = 6f;
    [Tooltip("위로 튕기는 속도")]
    [SerializeField] private float knockUpSpeed = 8f;
    [SerializeField] private float knockGravity = 25f;
    [Tooltip("회전 속도(도/초)")]
    [SerializeField] private float knockSpin = 720f;
    [Tooltip("튕긴 뒤 사라질 때까지 시간(초)")]
    [SerializeField] private float knockDuration = 0.5f;
    [Tooltip("튕길 때 효과음 (비워 두면 재생 안 함)")]
    public AudioClip knockSFX;

    private bool knocked;
    private Vector3 knockVelocity;
    private float knockElapsed;


    protected override void Start()
    {
        base.Start();
        onTargetHitEvent += OnCollded;

    }

    protected override void OnEnable()
    {
        base.OnEnable();
        knocked = false;
    }

    void FixedUpdate()
    {
        if (knocked)
            KnockMovement();
        else
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
            if (!hit || knocked)
                return;
            if (player.IsInvincible)
            {
                Knock(player);
                return;
            }
            CameraManager.Instance.FadeGlitch(0.5f, 0.01f, 0.2f);
            CameraManager.Instance.CameraShake(0.3f, 5f, 0.2f, CameraShakeMode.DECREMENT);
            AudioSource.PlayClipAtPoint(hitSFX, player.transform.position, 0.5f);
            player.TakeHit(DamageAmount);
            OnHitPlayer(player);
        }
    }

    // 판정을 통과해 실제로 플레이어를 때렸을 때 (서브클래스 고유 연출용)
    protected virtual void OnHitPlayer(Player player) { }

    private void Knock(Player player)
    {
        // 3D: 레인 위치로 방향 결정 (-1 왼쪽, 0 가운데, 1 오른쪽). 2D: 3방향 중 랜덤
        int dir = GameManager.Instance.currentViewMode == ViewMode.View2D
            ? Random.Range(-1, 2)
            : System.Math.Sign(currentLane - (GameManager.Instance.laneLength - 1) / 2);
        knockVelocity = new Vector3(dir * knockSideSpeed, knockUpSpeed, 0f);
        knockElapsed = 0f;
        knocked = true;
        if (knockSFX != null)
            AudioSource.PlayClipAtPoint(knockSFX, player.transform.position, 0.5f);
    }

    // 포물선 + 월드 스크롤 + 회전. knockDuration 후 풀로 반환
    private void KnockMovement()
    {
        float dt = Time.fixedDeltaTime;
        knockVelocity.y -= knockGravity * dt;
        Rb.MovePosition(transform.position + (knockVelocity + Vector3.back * GameManager.Instance.mapSpeed) * dt);
        // 날아가는 쪽으로 구르듯 회전 (가운데는 앞으로 회전)
        Vector3 axis = knockVelocity.x == 0f ? Vector3.right : Vector3.back * Mathf.Sign(knockVelocity.x);
        Rb.MoveRotation(Quaternion.AngleAxis(knockSpin * dt, axis) * Rb.rotation);

        knockElapsed += dt;
        if (knockElapsed >= knockDuration)
            gameObject.SetActive(false);
    }

}
