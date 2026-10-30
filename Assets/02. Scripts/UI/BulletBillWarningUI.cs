using System.Collections;
using UnityEngine;

// 킬러 발사 전 경고. 화면 상단의 ! 블록(+ 둥근 반투명 빨간 배경판)을 깜빡인다.
// Singleton<T>을 쓰면 Canvas가 DDOL이 되어 씬 재로드 시 중복되므로 씬마다 Awake에서 교체되는 static 참조를 쓴다
public class BulletBillWarningUI : MonoBehaviour
{
    public static BulletBillWarningUI Instance { get; private set; }

    [Tooltip("깜빡일 대상 (아이콘 + 배경판의 부모). 자기 자신을 끄면 코루틴이 멈추므로 자식으로 분리")]
    [SerializeField] private GameObject root;
    [Tooltip("켜짐/꺼짐이 바뀌는 간격(초)")]
    [Min(0.01f)]
    [SerializeField] private float blinkInterval = 0.15f;

    private Coroutine blinkCor;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show()
    {
        if (blinkCor != null)
            StopCoroutine(blinkCor);
        blinkCor = StartCoroutine(BlinkCoroutine());
    }

    public void Hide()
    {
        if (blinkCor != null)
        {
            StopCoroutine(blinkCor);
            blinkCor = null;
        }
        root.SetActive(false);
    }

    private IEnumerator BlinkCoroutine()
    {
        var wait = new WaitForSeconds(blinkInterval);
        while (true)
        {
            root.SetActive(!root.activeSelf);
            yield return wait;
        }
    }
}
