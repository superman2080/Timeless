using System;
using System.Collections;
using UnityEngine;

public class Stat<T> where T : MonoBehaviour
{
    public T owner { get; private set; }
    private float hp;
    public float HP
    {
        get => hp;
        private set
        {
            onHPChanged?.Invoke(owner, HP);
            hp = Mathf.Clamp(value, 0, MaxHP);
        }
    }

    public float MaxHP { get; private set; }

    public Action<T, float> onHPChanged;
    public Action<T, float> onTakeDamaged;
    public Action<T, float> onHealed;
    public Action<T> onDied;

    public Stat(T owner, float maxHP)
    {
        this.owner = owner; 
        MaxHP = maxHP;
        hp = MaxHP;
    }

    public void TakeDamage(float damage)
    {
        if(damage < 0)
            throw new ArgumentOutOfRangeException("Damage cannot be negative.");

        onTakeDamaged?.Invoke(owner, damage);
        HP -= Mathf.Abs(damage);

        if (HP <= 0)
        {
            OnDied();
        }
    }

    public void Heal(float healAmount)
    {
        if(healAmount < 0)
            throw new ArgumentOutOfRangeException("Heal amount cannot be negative.");

        onHealed?.Invoke(owner, healAmount);
        HP += Mathf.Abs(healAmount);
    }

    public void OnDied() => onDied?.Invoke(owner);

    public void SetMaxHP(float newMaxHP)
    {
        if (newMaxHP <= 0)
            throw new ArgumentOutOfRangeException(nameof(newMaxHP), "Max HP must be greater than 0.");

        MaxHP = newMaxHP;
        HP = Mathf.Min(hp, MaxHP); // 현재 HP가 새로운 MaxHP를 초과하지 않도록
    }
}

public class Player : InteractionObject
{
    private InputHandler handler;
    private PlayerAnimator playerAnimator;

    #region Relate to Lane
    private Coroutine changeLaneCor;
    #endregion

    #region Player Variable

    [SerializeField] float maxHP = 100f;
    public Stat<Player> stat { get; private set; }
    [SerializeField] private float hpDecrement;
    public bool IsJump { get; private set; }

    public float threshold2DView = 0.5f;

    private Coroutine jumpCoroutine;
    #endregion

    #region EventChannels

    public EventChannelSO onPlayerStatChanged;
    public EventChannelSO onPlayerDied;

    #endregion

    protected override void OnEnable()
    {
        return;
    }
    protected override void OnDisable()
    {
        return;
    }

    protected override void Start()
    {
        base.Start();
        #region Stat Initialization 
        stat = new Stat<Player>(this, maxHP);
        stat.onHPChanged += (player, hp) => onPlayerStatChanged.RaiseEvent();
        stat.onHPChanged += (player, hp) => CheckChangeView();
        stat.onDied += (player) => onPlayerDied.RaiseEvent();
        #endregion

        #region Component Initialization
        handler = gameObject.GetComponent<InputHandler>();
        playerAnimator = gameObject.GetComponent<PlayerAnimator>();
        #endregion

        #region Position Initialization 
        currentLane = Mathf.RoundToInt(GameManager.Instance.laneManager.laneLength / 2);

        #endregion
    }

    private void Update()
    {
        Move();
        HPDecrement();
    }

    void Move()
    {
        if (handler.MoveInput != 0 && GameManager.Instance.currentViewMode == ViewMode.View3D)
        {
            ChangeLane(currentLane + Convert.ToInt32(handler.MoveInput));
        }
        if (handler.Jump)
        {
            Jump();
        }
    }

    void Jump()
    {
        if (jumpCoroutine != null)
            return;

        jumpCoroutine = StartCoroutine(JumpCoroutine());
        playerAnimator.Jump();
    }
    public void ChangeLane(int laneIndex)
    {
        if (GameManager.Instance.currentViewMode == ViewMode.View2D ||
            laneIndex < 0 ||
            laneIndex > GameManager.Instance.laneLength - 1 ||
            changeLaneCor != null)
            return;

        changeLaneCor = StartCoroutine(ChangeLaneCoroutine(laneIndex, 0.1f));
    }

    private IEnumerator ChangeLaneCoroutine(int laneIndex, float changeTime = 0.2f)
    {
        var origin = Rb.position;
        var target = new Vector3(GameManager.Instance.laneManager.lanes[laneIndex].LaneX, Rb.position.y, Rb.position.z);

        for (float elapsedTime = 0f; elapsedTime < changeTime; elapsedTime += Time.fixedDeltaTime)
        {
            float t = elapsedTime / changeTime;
            var step = Vector3.Lerp(origin, target, t);
            Rb.MovePosition(step);
            yield return new WaitForFixedUpdate();
        }

        Rb.position = target;
        currentLane = laneIndex;
        changeLaneCor = null;
    }

    private IEnumerator JumpCoroutine()
    {
        IsJump = true;
        yield return new WaitUntil(() => Utils.IsAnimationTerminated(playerAnimator.animator, 0, "Jump"));
        IsJump = false;
        jumpCoroutine = null;
    }

    private void CheckChangeView()
    {
        // 수정: InverseLerp(min, max, value) 순서
        float hpRatio = Mathf.InverseLerp(0, stat.MaxHP, stat.HP);

        if (hpRatio <= GameManager.Instance.threshold2DView && GameManager.Instance.currentViewMode == ViewMode.View3D)
            GameManager.Instance.ChangeViewMode(ViewMode.View2D, 1.5f);
        else if (hpRatio >= GameManager.Instance.threshold3DView && GameManager.Instance.currentViewMode == ViewMode.View2D)
            GameManager.Instance.ChangeViewMode(ViewMode.View3D, 1.5f);
    }

    private void HPDecrement()
    {
        stat.TakeDamage(hpDecrement * Time.deltaTime);
    }
}