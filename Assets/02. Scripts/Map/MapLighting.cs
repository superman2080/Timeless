using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 맵(배경)별 라이팅 on/off. 프로브별로 처리한다.
/// Baked 라이트는 런타임에 끌 수 없으므로 씬 라이트 프로브의 SH를
/// 베이크값(켜짐) ↔ 하늘 앰비언트(꺼짐) 사이에서 교체한다.
/// 라이팅 대상 타일이 덮고 있는 프로브만 켜지고, 경계 밖은 blendDistance에 걸쳐 꺼진다.
/// </summary>
public class MapLighting : MonoBehaviour
{
    [SerializeField] private MapManager mapManager;
    [Tooltip("라이팅이 켜지는 배경 타입")]
    [SerializeField] private BackgroundType[] litBackgrounds = { BackgroundType.LavaCorridor };
    [Tooltip("타일 경계 밖으로 조명이 0이 될 때까지의 거리(유닛)")]
    [SerializeField, Min(0f)] private float blendDistance = 4f;
    [Tooltip("타일 pivot 기준 메시 시작 z. Corridor 프리팹은 -18 ~ +6")]
    [SerializeField] private float tileStartOffset = -18f;

    private SphericalHarmonicsL2[] lit;
    private SphericalHarmonicsL2[] off;
    private SphericalHarmonicsL2[] current;
    private float[] probeZ;
    private float[] weights;
    private readonly List<Vector2> litSpans = new List<Vector2>();

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

        var positions = LightmapSettings.lightProbes.positions;
        probeZ = new float[positions.Length];
        for (int i = 0; i < positions.Length; i++)
            probeZ[i] = positions[i].z;

        // 첫 LateUpdate에서 전부 반영되도록 유효 범위 밖 값으로 초기화
        weights = new float[lit.Length];
        System.Array.Fill(weights, -1f);
    }

    private bool IsLit(BackgroundType type) => System.Array.IndexOf(litBackgrounds, type) >= 0;

    private void LateUpdate()
    {
        // 활성화된 라이팅 대상 타일의 z 구간 수집
        litSpans.Clear();
        foreach (var bg in mapManager.Backgrounds)
        {
            if (!bg.gameObject.activeInHierarchy || !IsLit(bg.backgroundType))
                continue;
            float start = bg.transform.position.z + tileStartOffset;
            litSpans.Add(new Vector2(start, start + bg.backgroundLengthZ));
        }

        bool changed = false;
        for (int i = 0; i < current.Length; i++)
        {
            float w = Weight(probeZ[i]);
            if (w == weights[i])
                continue;
            weights[i] = w;
            current[i] = off[i] * (1f - w) + lit[i] * w;
            changed = true;
        }

        if (changed)
            LightmapSettings.lightProbes.bakedProbes = current;
    }

    // 프로브 z에서 가장 가까운 라이팅 구간까지의 거리로 가중치 계산
    private float Weight(float z)
    {
        float w = 0f;
        foreach (var span in litSpans)
        {
            float d = Mathf.Max(span.x - z, z - span.y, 0f);
            float spanW = d <= 0f ? 1f : blendDistance > 0f ? 1f - Mathf.Clamp01(d / blendDistance) : 0f;
            w = Mathf.Max(w, spanW);
        }
        return w;
    }
}
