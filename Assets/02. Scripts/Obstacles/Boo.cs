using UnityEngine;

// 부끄부끄. 멀리 있을 때는 몸이 투명해 아웃라인만 보이고, 플레이어가 revealDistance 안으로 들어오면 몸 알파가 1까지 차오른다.
// 아웃라인 알파는 (1 - 몸 알파)이므로 몸이 드러나는 만큼 같이 사라진다.
// 이동/피격/연출은 Obstacle을 그대로 쓴다 (하단 레인, 점프로 회피 가능)
public class Boo : Obstacle
{
    [Header("Reveal")]
    [Tooltip("플레이어와의 z 거리가 이 값 이하가 되면 몸이 드러난다 (유닛)")]
    [Min(0f)]
    [SerializeField] private float revealDistance = 12f;
    [Tooltip("알파가 목표값에 거의 도달하는 데 걸리는 시간(초). 0이면 즉시 전환")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.1f;
    [Tooltip("범위 밖에서의 몸 알파")]
    [Range(0f, 1f)]
    [SerializeField] private float hiddenAlpha = 0f;
    [Tooltip("켜면 씬 뷰에서 등장 구간을 선으로 표시")]
    [SerializeField] private bool drawRevealGizmo = true;

    [Header("Render")]
    [Tooltip("몸 + 아웃라인 머티리얼을 가진 렌더러 (Boo_Mesh)")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int bodyMaterialIndex = 0;
    [SerializeField] private int outlineMaterialIndex = 1;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");

    // 에셋이 아닌 런타임 인스턴스. 셰어드 머티리얼을 건드리면 에셋이 수정되고 모든 Boo가 같이 바뀐다
    private Material bodyMaterial;
    private Material outlineMaterial;
    private float alpha;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"{name}: targetRenderer가 연결되지 않았습니다.", this);
            return;
        }

        var materials = targetRenderer.materials;
        bodyMaterial = GetMaterial(materials, bodyMaterialIndex);
        outlineMaterial = GetMaterial(materials, outlineMaterialIndex);

        // 몸이 투명해도 깊이를 써야 아웃라인(확장된 뒷면)의 내부가 가려져 외곽선만 남는다.
        // 에셋에 저장해도 URP 머티리얼 임포터가 Transparent 머티리얼의 _ZWrite를 0으로 되돌리므로 런타임 인스턴스에 건다
        if (bodyMaterial != null && bodyMaterial.HasProperty(ZWriteID))
            bodyMaterial.SetFloat(ZWriteID, 1f);
    }

    private Material GetMaterial(Material[] materials, int index)
    {
        if (index >= 0 && index < materials.Length)
            return materials[index];

        Debug.LogWarning($"{name}: 머티리얼 인덱스 {index}가 렌더러 슬롯 범위를 벗어났습니다.", this);
        return null;
    }

    private void OnDestroy()
    {
        // materials 접근 시 복제된 인스턴스 정리
        if (bodyMaterial != null)
            Destroy(bodyMaterial);
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }

    // 풀에서 재사용될 때 이미 드러난 상태로 등장하지 않도록 초기화
    protected override void OnEnable()
    {
        base.OnEnable();
        alpha = hiddenAlpha;
        ApplyAlpha();
    }

    private void Update()
    {
        float target = IsInRange() ? 1f : hiddenAlpha;
        if (fadeDuration <= 0f)
        {
            alpha = target;
        }
        else
        {
            // 프레임레이트에 안정적인 지수 감쇠 Lerp.
            // 감쇠율 = 3 / fadeDuration 이면 fadeDuration 동안 차이의 약 95%가 줄어든다
            float rate = 3f / fadeDuration;
            alpha = Mathf.Lerp(alpha, target, 1f - Mathf.Exp(-rate * Time.deltaTime));
        }
        ApplyAlpha();
    }

    // 플레이어는 z 고정이고 월드가 -Z로 흐르므로 z 갭으로 거리를 잰다.
    // 플레이어 뒤로 지나간 경우(갭 음수)도 드러난 상태를 유지한다
    private bool IsInRange()
    {
        var player = GameManager.Instance.player;
        if (player == null)
            return false;

        return transform.position.z - player.transform.position.z <= revealDistance;
    }

    private void ApplyAlpha()
    {
        SetAlpha(bodyMaterial, BaseColorID, alpha);
        SetAlpha(outlineMaterial, OutlineColorID, 1f - alpha);
    }

    private static void SetAlpha(Material material, int propertyID, float value)
    {
        if (material == null || !material.HasProperty(propertyID))
            return;

        Color color = material.GetColor(propertyID);
        color.a = value;
        material.SetColor(propertyID, color);
    }

    // 등장 구간(플레이어 ~ revealDistance)을 씬 뷰에 표시
    private void OnDrawGizmosSelected()
    {
        if (!drawRevealGizmo || !Application.isPlaying)
            return;

        var player = GameManager.Instance.player;
        if (player == null)
            return;

        float x = transform.position.x;
        float y = transform.position.y;
        float near = player.transform.position.z;
        float far = near + revealDistance;
        Gizmos.color = Color.Lerp(Color.cyan, Color.white, alpha);
        Gizmos.DrawLine(new Vector3(x, y, near), new Vector3(x, y, far));
        Gizmos.DrawWireCube(new Vector3(x, y, far), new Vector3(0.6f, 0.1f, 0.05f));
    }
}
