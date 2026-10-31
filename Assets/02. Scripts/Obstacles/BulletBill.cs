using System.Collections;
using UnityEngine;

// 킬러. 상단 레인 3개를 한 번에 덮으며 월드보다 빠르게 직진한다 (점프 중에만 피격).
// 생성되면 warningDuration 동안 모델/콜라이더를 숨긴 채 제자리에서 UI 경고를 띄운 뒤 발사한다.
// 살아 있는 동안(경고 포함) LaneManager가 하단 오브젝트를 새로 생성하지 않는다
public class BulletBill : Obstacle
{
    // 동시 1개 제한 + 하단 생성 억제용 (LaneManager에서 확인)
    public static int ActiveCount { get; private set; }

    [Header("Fly")]
    [Tooltip("월드 속도에 더해지는 자체 비행 속도 (유닛/s)")]
    [Min(0f)]
    [SerializeField] private float flySpeed = 10f;
    [Tooltip("발사 전 경고 표시 시간(초). 0이면 즉시 발사")]
    [Min(0f)]
    [SerializeField] private float warningDuration = 1f;

    [Header("Model")]
    [Tooltip("레인별 모델 (왼쪽부터). laneInterval 간격으로 가운데 정렬된다")]
    [SerializeField] private Transform[] models;

    private BoxCollider boxCollider;
    private bool launched;
    private Coroutine warningCor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveCount = 0; // 도메인 리로드 꺼져 있음

    // InteractionObject.Col은 Start에서 할당되어 첫 OnEnable 시점엔 null이므로 직접 캐싱
    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ActiveCount++;
        LayoutModels();
        SetVisible(false);
        launched = false;
        warningCor = StartCoroutine(WarningCoroutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ActiveCount--;
        if (warningCor != null)
        {
            StopCoroutine(warningCor);
            warningCor = null;
            if (BulletBillWarningUI.Instance != null)
                BulletBillWarningUI.Instance.Hide();
        }
    }

    private IEnumerator WarningCoroutine()
    {
        if (warningDuration > 0f)
        {
            if (BulletBillWarningUI.Instance != null)
                BulletBillWarningUI.Instance.Show();
            yield return new WaitForSeconds(warningDuration);
            if (BulletBillWarningUI.Instance != null)
                BulletBillWarningUI.Instance.Hide();
        }

        SetVisible(true);
        launched = true;
        warningCor = null;
    }

    // 경고 중에는 생성 위치에 멈춰 있다
    protected override void ObstacleMovement()
    {
        if (!launched)
            return;
        Rb.MovePosition(transform.position + Vector3.back * (GameManager.Instance.mapSpeed + flySpeed) * Time.fixedDeltaTime);
    }

    // 레인 간격을 런타임에 읽어 모델을 레인마다 하나씩 놓고, 콜라이더 폭을 전체 레인에 맞춘다
    private void LayoutModels()
    {
        var laneManager = GameManager.Instance.laneManager;
        if (laneManager == null || models == null || models.Length == 0)
            return;

        float interval = laneManager.laneInterval;
        float centerOffset = (models.Length - 1) * 0.5f;
        for (int i = 0; i < models.Length; i++)
        {
            if (models[i] == null)
                continue;
            var pos = models[i].localPosition;
            pos.x = (i - centerOffset) * interval;
            models[i].localPosition = pos;
        }

        if (boxCollider != null)
        {
            var size = boxCollider.size;
            size.x = models.Length * interval;
            boxCollider.size = size;
        }
    }

    private void SetVisible(bool visible)
    {
        if (models != null)
        {
            foreach (var model in models)
                if (model != null)
                    model.gameObject.SetActive(visible);
        }
        if (boxCollider != null)
            boxCollider.enabled = visible;
    }
}
