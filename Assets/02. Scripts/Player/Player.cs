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
            hp = Mathf.Clamp(value, 0, MaxHP);
            onHPChanged?.Invoke(owner, hp);
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
        if (damage < 0)
            throw new ArgumentOutOfRangeException("Damage cannot be negative.");

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

        onHealed?.Invoke(owner, healAmount);
        HP += Mathf.Abs(healAmount);
    }

    // 늘어나면 늘어난 만큼 HP도 회복, 줄어들면 최대치로 자름
    public void SetMaxHP(float newMaxHP)
    {
        if (newMaxHP <= 0)
            throw new ArgumentOutOfRangeException(nameof(newMaxHP), "Max HP must be greater than 0.");

        float increase = Mathf.Max(0f, newMaxHP - MaxHP);
        MaxHP = newMaxHP;
        HP = hp + increase;
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

    public Stat<Player> stat { get; private set; }
    [SerializeField] private float hpDecrement;
    public bool IsJump { get; private set; }
    #endregion

    #region State
    [Header("State")]
    [Tooltip("시작 형태. 크기/최대 HP 등 기본 스탯")]
    [SerializeField] PlayerFormSO defaultForm;
    public PlayerStateMachine StateMachine { get; private set; }
    public bool IsInvincible => StateMachine.Stats.invincible;
    private float baseMapSpeed;
    #endregion

    #region Jump
    [Header("Jump")]
    [Tooltip("체공 시간(초)")]
    [SerializeField] float airTime = 0.5f;
    [Tooltip("체공 그래프. x: 0~1 정규화 시간, y: 높이(월드 유닛)")]
    [SerializeField] AnimationCurve jumpCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1.5f), new Keyframe(1, 0));
    [Tooltip("점프 중 레인 변경 허용")]
    [SerializeField] bool allowLaneChangeWhileJumping = true;
    private float groundY;
    private float jumpOffsetY;
    private float laneX;
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
        stat = new Stat<Player>(this, defaultForm != null ? defaultForm.stats.maxHP : PlayerStats.Default.maxHP);
        stat.onHPChanged += (player, hp) => onPlayerStatChanged.RaiseEvent();
        stat.onHPChanged += (player, hp) => CheckChangeView();

        stat.onDied += (player) => {
            CameraManager.Instance.FadeGlitch(0.01f, 0.9f, 1f);
            Col.enabled = false;
            Invoke("Restart", 1.5f);
        };
        stat.onDied += (player) => onPlayerDied.RaiseEvent();
        stat.onDied += (player) => enabled = false;

        #endregion

        #region State Initialization
        baseMapSpeed = GameManager.Instance.mapSpeed;
        StateMachine = new PlayerStateMachine(this);
        StateMachine.onStatsChanged += ApplyStats;
        StateMachine.ChangeForm(defaultForm);
        #endregion

        #region Component Initialization
        handler = gameObject.GetComponent<InputHandler>();
        playerAnimator = gameObject.GetComponent<PlayerAnimator>();
        #endregion

        #region Position Initialization 
        groundY = Rb.position.y;
        laneX = Rb.position.x;
        currentLane = Mathf.RoundToInt(GameManager.Instance.laneManager.laneLength / 2);
        ChangeLane(currentLane);
        #endregion

        CameraManager.Instance.FadeGlitch(0.9f, 0.002f, 1.5f);
    }

    private void Update()
    {
        Move();
        StateMachine.Tick(Time.deltaTime);

        // 무적 상태가 아닐 때만 HP 감소
        if (!IsInvincible)
        {
            HPDecrement();
        }

        if (currentLane != GameManager.Instance.laneManager.laneLength - 1 && GameManager.Instance.currentViewMode == ViewMode.View2D && changeLaneCor == null)
            ChangeLane(GameManager.Instance.laneManager.laneLength - 1, true);
    }

    private void FixedUpdate()
    {
        // 레인 이동(X)과 점프(Y)를 한 번의 MovePosition으로 합성
        Rb.MovePosition(new Vector3(laneX, groundY + jumpOffsetY, Rb.position.z));
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

        float duration = airTime * StateMachine.Stats.airTimeMultiplier;
        StartCoroutine(JumpCoroutine(duration));
        playerAnimator.Jump(duration);
    }

    // force: for 2D switch. cancels in-flight move, ignores view/jump guards
    public void ChangeLane(int laneIndex, bool force = false)
    {
        if (laneIndex < 0 || laneIndex > GameManager.Instance.laneLength - 1)
            return;

        if (force)
        {
            if (changeLaneCor != null)
                StopCoroutine(changeLaneCor);
        }
        else if (GameManager.Instance.currentViewMode == ViewMode.View2D ||
            changeLaneCor != null ||
            (!allowLaneChangeWhileJumping && IsJump))
            return;

        changeLaneCor = StartCoroutine(ChangeLaneCoroutine(laneIndex, 0.1f));
    }

    private IEnumerator ChangeLaneCoroutine(int laneIndex, float changeTime = 0.2f)
    {
        float originX = laneX;
        float targetX = GameManager.Instance.laneManager.lanes[laneIndex].LaneX;

        for (float elapsedTime = 0f; elapsedTime < changeTime; elapsedTime += Time.fixedDeltaTime)
        {
            laneX = Mathf.Lerp(originX, targetX, elapsedTime / changeTime);
            yield return new WaitForFixedUpdate();
        }

        laneX = targetX;
        currentLane = laneIndex;
        changeLaneCor = null;
    }

    private IEnumerator JumpCoroutine(float duration)
    {
        IsJump = true;
        for (float elapsedTime = 0f; elapsedTime < duration; elapsedTime += Time.fixedDeltaTime)
        {
            jumpOffsetY = jumpCurve.Evaluate(elapsedTime / duration) * StateMachine.Stats.jumpHeightMultiplier;
            yield return new WaitForFixedUpdate();
        }
        jumpOffsetY = 0f;
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
        stat.TakeDamage(hpDecrement * StateMachine.Stats.hpDecrementMultiplier * Time.deltaTime);
    }

    // 장애물 피격. 무적이면 무시, 형태/효과의 OnHit을 거친 남은 데미지만 적용
    public void TakeHit(float damage)
    {
        if (IsInvincible)
            return;

        damage = StateMachine.ProcessHit(damage);
        if (damage > 0f)
            stat.TakeDamage(damage);
    }

    private void ApplyStats(PlayerStats stats)
    {
        transform.localScale = Vector3.one * stats.scale;
        stat.SetMaxHP(stats.maxHP);
        GameManager.Instance.mapSpeed = baseMapSpeed * stats.mapSpeedMultiplier;
    }

    private void Restart()
    {
        CameraManager.Instance.SetGlitch(0.02f);
        CameraManager.Instance.SetPixelateIntensity(1);
        UnityEngine.AddressableAssets.Addressables.LoadSceneAsync("SampleScene");
    }
}