using UnityEngine;

// 플레이어 뒤에서 일정 시간마다 등장해 추격하는 방해형 보스.
// Hidden → (첫 등장: Intro 컷씬 / 이후: Enter) → Chase → Exit → Hidden
public class Boss : MonoBehaviour
{
    [Header("Model")]
    [Tooltip("보이기/숨기기 대상 (Animator 포함 모델)")]
    public GameObject model;

    [Header("Timing")]
    [Tooltip("출현 간격(초). 게임 시작부터 세고, 퇴장이 끝나면 다시 센다")]
    [Min(0f)] public float appearInterval = 30f;
    [Tooltip("추격 유지 시간(초)")]
    [Min(0f)] public float stayDuration = 10f;

    [Header("Intro Cutscene (게임당 1회, 게임 정지)")]
    [Tooltip("낙하 시작 높이 (지면 기준). 복도 아치 천장(약 6.9m) 아래로 둘 것")]
    public float dropHeight = 6f;
    [Tooltip("낙하 가속도")]
    public float fallGravity = 40f;
    [Tooltip("낙하 전 카메라를 먼저 보여 주는 시간(초)")]
    public float introLeadTime = 0.4f;
    [Tooltip("착지 후 Idle 유지 시간(초). 이후 Screaming → Walk (블렌드는 Bowser.controller 전이에서 설정)")]
    public float landIdleTime = 0.3f;
    [Tooltip("착지 후 Walk 전환을 기다리는 최대 시간(초). 애니 설정이 꼬여도 컷씬이 끝나도록 하는 안전장치")]
    public float maxLandHold = 6f;
    public EffectType landEffect = EffectType.FIRE_BUFF;
    [Tooltip("울부짖을 때(Scream 트리거 시점) 재생할 효과음")]
    public SfxType roarSfx = SfxType.BOSS_ROAR;
    public float landShakeAmplitude = 3f;
    public float landShakeFrequency = 2f;
    public float landShakeDuration = 0.6f;

    [Header("Movement")]
    public float groundY = 0f;
    [Tooltip("두 번째 등장부터 달려 들어오기 시작하는 Z (플레이어 뒤, 화면 밖)")]
    public float spawnZ = -40f;
    [Tooltip("3D 추격 위치 Z")]
    public float chaseZ3D = -14f;
    [Tooltip("2D 추격 위치 Z (2D 카메라 화면 안)")]
    public float chaseZ2D = -10.8f;
    [Tooltip("퇴장 완료 Z")]
    public float exitZ = -45f;
    public float enterSpeed = 15f;
    public float exitAccel = 20f;
    [Tooltip("플레이어 X 추적 속도 (Lerp 계수)")]
    public float laneFollowSpeed = 4f;

    [Header("Camera Blend")]
    public float chaseBlend = 1f;
    public float exitBlend = 1f;

    [HideInInspector] public bool introPlayed; // 씬 재로드 시 리셋 = 게임당 1회

    public BossStateMachine Machine { get; } = new();
    public BossHiddenState HiddenState { get; private set; }
    public BossIntroState IntroState { get; private set; }
    public BossEnterState EnterState { get; private set; }
    public BossChaseState ChaseState { get; private set; }
    public BossExitState ExitState { get; private set; }

    public Animator Animator { get; private set; }

    public float ChaseZ => GameManager.Instance.currentViewMode == ViewMode.View2D ? chaseZ2D : chaseZ3D;

    private void Start()
    {
        Animator = model.GetComponent<Animator>();
        HiddenState = new BossHiddenState(this);
        IntroState = new BossIntroState(this);
        EnterState = new BossEnterState(this);
        ChaseState = new BossChaseState(this);
        ExitState = new BossExitState(this);
        Machine.Change(HiddenState);
    }

    private void Update()
    {
        if (Machine.Current == null)
            return;
        Machine.Tick(Machine.Current.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
    }

    public void SetModelVisible(bool visible) => model.SetActive(visible);

    public void SetPosition(float y, float z)
        => transform.position = new Vector3(GameManager.Instance.player.transform.position.x, y, z);

    // 플레이어 X를 지연 추적하며 Z를 목표로 이동. 목표에 도달하면 true
    public bool MoveTowardsZ(float targetZ, float speed, float dt)
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, GameManager.Instance.player.transform.position.x, 1f - Mathf.Exp(-laneFollowSpeed * dt));
        pos.z = Mathf.MoveTowards(pos.z, targetZ, speed * dt);
        transform.position = pos;
        return Mathf.Approximately(pos.z, targetZ);
    }

    private void OnDisable()
    {
        // 컷씬 도중 파괴/씬 전환 대비
        if (Machine.Current == IntroState && IntroState != null)
            Time.timeScale = 1f;
    }
}
