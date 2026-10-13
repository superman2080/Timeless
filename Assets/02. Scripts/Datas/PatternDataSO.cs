using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PatternRow
{
    [Tooltip("하단 레인별 오브젝트 (index = 레인)")]
    public ObjectType[] lower = new ObjectType[PatternDataSO.LaneCount];
    [Tooltip("상단 레인별 오브젝트 (점프 중에만 판정)")]
    public ObjectType[] upper = new ObjectType[PatternDataSO.LaneCount];
}

// 스폰 패턴 1개. Tools/Timeless/Pattern Editor에서 편집
[CreateAssetMenu(fileName = "PatternData", menuName = "Scriptable Objects/PatternData")]
public class PatternDataSO : ScriptableObject
{
    public const int LaneCount = 3;

    [Tooltip("View2D면 에디터에서 레인 2만 표시")]
    public ViewMode viewMode = ViewMode.View3D;

    [Tooltip("rows[0]이 가장 먼저 생성됨")]
    public List<PatternRow> rows = new List<PatternRow> { new PatternRow() };
}
