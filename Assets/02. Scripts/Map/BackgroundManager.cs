using UnityEngine;

// 이 Enum은 MapManager.cs 파일에만 두는 것이 좋습니다.
// 만약 이 파일에 추가했다면 MapManager.cs에서는 빼주세요.

public class BackgroundManager : MonoBehaviour
{
    [Header("배경 설정")]
    // 1. 이 프리팹의 실제 Z축 길이 (예: 30)
    // 인스펙터에서 직접 정확한 값을 입력해야 합니다. (간격 문제 해결용)
    public float backgroundLengthZ = 30f;

    // 2. 이 배경의 타입 (MapManager가 참조)
    public BackgroundType backgroundType;

    // 3. 원근감(시차) 효과를 위한 속도 배율
    // (1 = 트랙과 동일 속도, 0.5 = 절반 속도)
    public float parallaxSpeedMultiplier = 1.0f;

    [Header("참조 (선택 사항)")]
    // NRE 오류 방지를 위해 Awake()에서 자동으로 찾도록 설정
    public Renderer meshRenderer;

    private void Awake()
    {
        // 인스펙터에서 연결 안 해도 스스로 찾도록 방어 코드 추가
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<Renderer>();

        if (meshRenderer == null)
            Debug.LogError(gameObject.name + "에서 Renderer를 찾을 수 없습니다!");
    }

    /// <summary>
    /// 이 배경 오브젝트의 3D 크기를 반환합니다. (Z축은 수동 입력을 따름)
    /// </summary>
    public Vector3 BackgroundSize
    {
        get
        {
            if (meshRenderer == null)
            {
                // 렌더러가 없어도 Z길이는 반환해야 무한 스폰이 안 됩니다.
                return new Vector3(0, 0, backgroundLengthZ);
            }

            Vector3 size = meshRenderer.bounds.size;
            // Z축 크기는 bounds 대신 수동으로 입력한 값(backgroundLengthZ)을 사용
            size.z = backgroundLengthZ;
            return size;
        }
    }

    // OnEnable은 Track.cs와 동일하게 유지 (GameManager 종속성 확인)
    private void OnEnable()
    {
        // Track.cs와 동일한 GameManager 변수명(GeneratePos) 사용
        if (GameManager.Instance.GeneratePos == null)
        {
            Debug.LogError("Error: Unexpect generate position"); // 오류 메시지 수정
            Destroy(gameObject); // 안전하게 파괴
        }
    }

    // 매 프레임 호출
    private void Update()
    {
        // 1. 설정된 속도(원근감 포함)로 뒤로 이동
        float moveSpeed = GameManager.Instance.mapSpeed * parallaxSpeedMultiplier;
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // 2. 폐기 지점에 도달하면 스스로 비활성화 (Track.cs와 동일한 로직)
        // Track.cs와 동일한 GameManager 변수명(DisposePos) 사용
        if (transform.position.z <= GameManager.Instance.DisposePos.z)
        {
            gameObject.SetActive(false);
        }
    }
}