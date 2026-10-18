using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 점박문어 화면 효과. 생성 시 먹물(반투명 검정 원) 뿌리기, 피격 시 전체 암전.
// 두 효과 모두 알파 = 커브(경과 시간)이고, 커브의 마지막 키 시간이 지속 시간이다.
// Singleton<T>을 쓰면 Canvas가 DDOL이 되어 씬 재로드 시 중복되므로 씬마다 Awake에서 교체되는 static 참조를 쓴다
public class OctoombaScreenEffect : MonoBehaviour
{
    public static OctoombaScreenEffect Instance { get; private set; }

    [Header("Ink")]
    [SerializeField] private Image[] inkBlobs;
    [Tooltip("먹물 원 지름 범위 (캔버스 기준 px)")]
    [SerializeField] private Vector2 inkSizeRange = new(300f, 600f);
    [SerializeField] private AnimationCurve inkAlphaCurve = new(new Keyframe(0f, 0.8f), new Keyframe(0.5f, 0.8f), new Keyframe(4f, 0f));

    [Header("Blackout")]
    [SerializeField] private Image blackoutImage;
    [SerializeField] private AnimationCurve blackoutAlphaCurve = new(new Keyframe(0f, 1f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0f));

    private Coroutine inkCor;
    private Coroutine blackoutCor;

    private void Awake()
    {
        Instance = this;
        SetActive(inkBlobs, false);
        blackoutImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SplashInk()
    {
        var area = ((RectTransform)transform).rect.size * 0.5f;
        foreach (var blob in inkBlobs)
        {
            var rt = blob.rectTransform;
            rt.anchoredPosition = new Vector2(Random.Range(-area.x, area.x), Random.Range(-area.y, area.y));
            rt.sizeDelta = Vector2.one * Random.Range(inkSizeRange.x, inkSizeRange.y);
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
        Restart(ref inkCor, inkBlobs, inkAlphaCurve);
    }

    public void Blackout() => Restart(ref blackoutCor, new[] { blackoutImage }, blackoutAlphaCurve);

    private void Restart(ref Coroutine cor, Image[] images, AnimationCurve curve)
    {
        if (cor != null)
            StopCoroutine(cor);
        cor = StartCoroutine(PlayCurve(images, curve));
    }

    private IEnumerator PlayCurve(Image[] images, AnimationCurve curve)
    {
        SetActive(images, true);
        float duration = curve.length > 0 ? curve[curve.length - 1].time : 0f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetAlpha(images, curve.Evaluate(t));
            yield return null;
        }
        SetActive(images, false);
    }

    private static void SetAlpha(Image[] images, float alpha)
    {
        foreach (var image in images)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    private static void SetActive(Image[] images, bool active)
    {
        foreach (var image in images)
            image.gameObject.SetActive(active);
    }
}
