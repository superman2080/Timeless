using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PatternEntry
{
    public PatternDataSO pattern;
    [Range(0, 100)] public float percent;
}

[CreateAssetMenu(fileName = "CreateObjectSO", menuName = "Scriptable Objects/CreateObjectSO")]
public class CreateObjectSO : ScriptableObject
{
    [Tooltip("패턴별 등장 확률(%). 합이 100이어야 함")]
    public List<PatternEntry> patterns = new List<PatternEntry>();

    [Tooltip("패턴 내 각 줄 사이 거리 (유닛)")]
    public float rowDistance = 1.8f;

    // 누적 % 기준 선택. 합이 100이 아니어도 합계 비율로 동작
    public PatternDataSO PickRandom()
    {
        float total = 0f;
        foreach (var e in patterns)
            if (e.pattern != null) total += e.percent;
        if (total <= 0f)
            return null;

        float r = UnityEngine.Random.value * total;
        PatternDataSO last = null;
        foreach (var e in patterns)
        {
            if (e.pattern == null || e.percent <= 0f)
                continue;
            last = e.pattern;
            r -= e.percent;
            if (r < 0f)
                return e.pattern;
        }
        return last;
    }
}
