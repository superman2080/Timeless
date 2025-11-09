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

    public bool isInvincible { get; private set; } = false;
    private bool isInvincibleCooldown = false;
    private Coroutine invincibleCoroutine;

    public float MaxHP { get; private set; }
    public float invincibleDuration = 5f;
    public float invincibleCooldown = 10f;

    public Action<T, float> onHPChanged;
    public Action<T, float> onTakeDamaged;
    public Action<T, float> onHealed;
    public Action<T> onDied;
    public Action<T> onInvincibleStart;
    public Action<T> onInvincibleEnd;

    public Stat(T owner, float maxHP, float invincibleDuration = 5f, float invincibleCooldown = 10f)
    {
        this.owner = owner;
        MaxHP = maxHP;
        hp = MaxHP;
        this.invincibleDuration = invincibleDuration;
        this.invincibleCooldown = invincibleCooldown;
    }

    public void TakeDamage(float damage)
    {
        if (damage < 0)
            throw new ArgumentOutOfRangeException("Damage cannot be negative.");

        if (isInvincible)
            return;

        onTakeDamaged?.Invoke(owner, damage);
        HP -= Mathf.Abs(damage);

        if (HP <= 0)
        {
            Debug.Log("OnDied invoked");
            onDied?.Invoke(owner);
        }
    }

    public void Heal(float healAmount)
    {
        if (healAmount < 0)
            throw new ArgumentOutOfRangeException("Heal amount cannot be negative.");

        float previousHP = HP;
        onHealed?.Invoke(owner, healAmount);
        HP += Mathf.Abs(healAmount);

        // 초과 회복 체크: HP가 MaxHP를 초과했고, 무적/쿨다운 상태가 아닐 때
        if (previousHP < MaxHP && HP >= MaxHP && !isInvincible && !isInvincibleCooldown)
        {
            if (invincibleCoroutine != null)
                owner.StopCoroutine(invincibleCoroutine);
            invincibleCoroutine = owner.StartCoroutine(InvincibleCoroutine());
        }
    }

    public void SetMaxHP(float newMaxHP)
    {
        if (newMaxHP <= 0)
            throw new ArgumentOutOfRangeException(nameof(newMaxHP), "Max HP must be greater than 0.");

        MaxHP = newMaxHP;
        HP = Mathf.Min(hp, MaxHP);
    }

    private IEnumerator InvincibleCoroutine()
    {
        // 무적 시작
        isInvincible = true;
        onInvincibleStart?.Invoke(owner);

        // 무적 지속
        yield return new WaitForSeconds(invincibleDuration);

        // 무적 종료
        isInvincible = false;
        onInvincibleEnd?.Invoke(owner);

        // 쿨다운 시작
        isInvincibleCooldown = true;

        yield return new WaitForSeconds(invincibleCooldown);

        // 쿨다운 종료
        isInvincibleCooldown = false;
        invincibleCoroutine = null;
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
    [SerializeField] float invincibleDuration = 5f;
    [SerializeField] float invincibleCooldown = 10f;
    [SerializeField] float invincibleSpeed = 10f;
    private float originSpeed;
    public Stat<Player> stat { get; private set; }
    [SerializeField] private float hpDecrement;
    public bool IsJump { get; private set; }
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
        stat = new Stat<Player>(this, maxHP, invincibleDuration, invincibleCooldown);
        stat.onHPChanged += (player, hp) => onPlayerStatChanged.RaiseEvent();
        stat.onHPChanged += (player, hp) => CheckChangeView();

        stat.onDied += (player) => CameraManager.Instance.FadeGlitch(0.01f, 1f, 1.25f);
        stat.onDied += (player) => onPlayerDied.RaiseEvent();
        stat.onDied += (player) => enabled = false;

        stat.onHealed += (player, healAmount) => Debug.Log($"Healed: {healAmount}, Current HP: {stat.HP}");

        originSpeed = GameManager.Instance.mapSpeed;
        stat.onInvincibleStart += (player) => GameManager.Instance.mapSpeed = invincibleSpeed;
        stat.onInvincibleEnd += (player) => GameManager.Instance.mapSpeed = originSpeed;
        #endregion

        #region Component Initialization
        handler = gameObject.GetComponent<InputHandler>();
        playerAnimator = gameObject.GetComponent<PlayerAnimator>();
        #endregion

        #region Position Initialization 
        currentLane = Mathf.RoundToInt(GameManager.Instance.laneManager.laneLength / 2);
        ChangeLane(currentLane);
        #endregion
    }

    private void Update()
    {
        Move();

        // 무적 상태가 아닐 때만 HP 감소
        if (!stat.isInvincible)
        {
            HPDecrement();
        }

        if (currentLane != GameManager.Instance.laneManager.laneLength - 1 && GameManager.Instance.currentViewMode == ViewMode.View2D)
            ChangeLane(GameManager.Instance.laneManager.laneLength - 1);
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
        if (IsJump == true)
            return;

        StartCoroutine(JumpCoroutine());
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
    }

    private void CheckChangeView()
    {
        float hpRatio = Mathf.InverseLerp(0, stat.MaxHP, stat.HP);

        if (hpRatio <= GameManager.Instance.threshold2DView && GameManager.Instance.currentViewMode == ViewMode.View3D)
        {
            GameManager.Instance.ChangeViewMode(ViewMode.View2D, 1.5f);
            GameManager.Instance.audioPlayer.trigger = true;
            GameManager.Instance.laneManager.RemoveAllLaneObjects();
        }
        else if (hpRatio >= GameManager.Instance.threshold3DView && GameManager.Instance.currentViewMode == ViewMode.View2D)
        {
            GameManager.Instance.ChangeViewMode(ViewMode.View3D, 1.5f);
            GameManager.Instance.audioPlayer.trigger = true;
            GameManager.Instance.laneManager.RemoveAllLaneObjects();
        }
    }

    private void HPDecrement()
    {
        stat.TakeDamage(hpDecrement * Time.deltaTime);
    }
}