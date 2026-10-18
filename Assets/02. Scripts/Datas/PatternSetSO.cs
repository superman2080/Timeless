using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PatternSetTier
{
    [Tooltip("뷰 모드 안의 HP 비율(0~1)이 이 값 이하일 때 사용")]
    [Range(0, 1)] public float maxRatio = 1f;
    public CreateObjectSO dataSet;
}

[CreateAssetMenu(fileName = "PatternSet", menuName = "Scriptable Objects/PatternSet")]
public class PatternSetSO : ScriptableObject
{
    [Tooltip("3D → 2D 전환: 전체 HP 비율이 이 값 이하가 되면")]
    [Range(0, 1)] public float threshold2DView = 0.5f;
    [Tooltip("2D → 3D 전환: 전체 HP 비율이 이 값 이상이 되면")]
    [Range(0, 1)] public float threshold3DView = 0.7f;

    public List<PatternSetTier> tiers3D = new List<PatternSetTier>();
    public List<PatternSetTier> tiers2D = new List<PatternSetTier>();

    public List<PatternSetTier> GetTiers(ViewMode mode) => mode == ViewMode.View2D ? tiers2D : tiers3D;

    // 전체 HP 비율 → 해당 뷰 모드 안의 0~1 비율 (2D: 0~threshold3DView, 3D: threshold2DView~1)
    public float ToLocalRatio(ViewMode mode, float hpRatio) => mode == ViewMode.View2D
        ? Mathf.InverseLerp(0f, threshold3DView, hpRatio)
        : Mathf.InverseLerp(threshold2DView, 1f, hpRatio);

    public float ToGlobalRatio(ViewMode mode, float local) => mode == ViewMode.View2D
        ? Mathf.Lerp(0f, threshold3DView, local)
        : Mathf.Lerp(threshold2DView, 1f, local);

    // 로컬 비율 이하 조건을 만족하는 구간 중 maxRatio가 가장 작은 것 (같으면 앞의 것)
    public CreateObjectSO Select(ViewMode mode, float hpRatio)
    {
        float local = ToLocalRatio(mode, hpRatio);
        PatternSetTier best = null;
        foreach (var t in GetTiers(mode))
            if (t.dataSet != null && local <= t.maxRatio && (best == null || t.maxRatio < best.maxRatio))
                best = t;
        return best?.dataSet;
    }

    // 구간 하한 = 같은 리스트에서 상한이 더 작은 유효 구간 중 가장 큰 상한 (Select와 같은 규칙)
    public static float GetLowerBound(List<PatternSetTier> tiers, PatternSetTier tier)
    {
        float lower = 0f;
        foreach (var t in tiers)
            if (t != tier && t.dataSet != null && t.maxRatio < tier.maxRatio && t.maxRatio > lower)
                lower = t.maxRatio;
        return lower;
    }
}
