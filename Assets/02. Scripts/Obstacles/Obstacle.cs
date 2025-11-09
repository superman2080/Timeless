using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Obstacle : InteractionObject
{
    public Rigidbody rb;
    private float damageAmount;
    public float DamageAmount => damageAmount;

    private Vector3 disposePos;
    private Coroutine disposeCor;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        onTargetHitEvent += OnCollded;
        disposePos = GameManager.Instance.positionLimits.disposePos;
    }

    void FixedUpdate()
    {
        ObstacleMovement();
    }

    protected virtual void ObstacleMovement()
    {
        rb.MovePosition(transform.position + Vector3.back * GameManager.Instance.mapSpeed * Time.fixedDeltaTime);
    }

    public void OnEnable()
    {
        disposeCor = StartCoroutine(DisposeCoroutine());
    }

    public void OnDisable()
    {
        if (disposeCor != null)
        {
            StopCoroutine(disposeCor);
            disposeCor = null;
        }
    }

    private void OnCollded(ICollisionable collision)
    {
        if(collision is Player player && player.IsJump == false)
        {
            player.stat.TakeDamage(DamageAmount);
        }
    }

    private IEnumerator DisposeCoroutine()
    {
        while (true)
        {
            if (transform.position.z < disposePos.z)
            {
                gameObject.SetActive(false);
                yield break;
            }
            yield return null;
        }
    }
}
