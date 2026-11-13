using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 맵(배경)별 라이팅 on/off.
/// Baked 라이트는 런타임에 끌 수 없으므로 씬 라이트 프로브의 SH를
/// 베이크값(켜짐) ↔ 하늘 앰비언트(꺼짐) 사이에서 교체한다.
/// </summary>
public class MapLighting : MonoBehaviour
{
    [SerializeField] private MapManager mapManager;
    [Tooltip("라이팅이 켜지는 배경 타입")]
    [SerializeField] private BackgroundType[] litBackgrounds = { BackgroundType.LavaCorridor };
    [SerializeField] private float fadeTime = 1f;

    private SphericalHarmonicsL2[] lit;
    private SphericalHarmonicsL2[] off;
    private SphericalHarmonicsL2[] current;
    private Coroutine fadeCor;

    private void Start()
    {
        if (mapManager == null)
            mapManager = FindAnyObjectByType<MapManager>();

        if (LightmapSettings.lightProbes == null || mapManager == null)
        {
            Debug.LogWarning("MapLighting: 베이크된 라이트 프로브 또는 MapManager가 없습니다.");
            enabled = false;
            return;
        }

        // 런타임 사본으로 교체해 LightingData 에셋이 플레이 중 수정되지 않게 한다
        LightmapSettings.lightProbes = Instantiate(LightmapSettings.lightProbes);

        lit = LightmapSettings.lightProbes.bakedProbes;
        off = new SphericalHarmonicsL2[lit.Length];
        System.Array.Fill(off, RenderSettings.ambientProbe);
        current = new SphericalHarmonicsL2[lit.Length];

        Blend(lit, IsLit(mapManager.nowBackground) ? lit : off, 1f);
        mapManager.onBackgroundChanged += OnBackgroundChanged;
    }

    private void OnDestroy()
    {
        if (mapManager != null)
            mapManager.onBackgroundChanged -= OnBackgroundChanged;
    }

    private bool IsLit(BackgroundType type) => System.Array.IndexOf(litBackgrounds, type) >= 0;

    private void OnBackgroundChanged(BackgroundType type)
    {
        if (fadeCor != null)
            StopCoroutine(fadeCor);
        fadeCor = StartCoroutine(FadeCoroutine(IsLit(type) ? lit : off));
    }

    private IEnumerator FadeCoroutine(SphericalHarmonicsL2[] target)
    {
        var from = (SphericalHarmonicsL2[])current.Clone();
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            Blend(from, target, t / fadeTime);
            yield return null;
        }
        Blend(from, target, 1f);
        fadeCor = null;
    }

    private void Blend(SphericalHarmonicsL2[] a, SphericalHarmonicsL2[] b, float t)
    {
        for (int i = 0; i < current.Length; i++)
            current[i] = a[i] * (1f - t) + b[i] * t;
        LightmapSettings.lightProbes.bakedProbes = current;
    }
}
