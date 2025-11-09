using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ObjectSpawnData
{
    [Tooltip("스폰 위치의 라인 인덱스 (0: 왼쪽, 1: 중앙, 2: 오른쪽...)")]
    public int laneIndex;
    public ObjectType type;

    public ObjectSpawnData(int laneIndex = 0, ObjectType type = ObjectType.NONE)
    {
        this.laneIndex = laneIndex;
        this.type = type;
    }
}

[Serializable]
public class ObjectSpawnRow
{
    [Tooltip("이 줄에서 동시에 생성될 오브젝트들 (각 레인별)")]
    public List<ObjectSpawnData> spawnDatas = new List<ObjectSpawnData>();
}

[Serializable]
public class ObjectSpawnDataSet
{
    [Tooltip("각 행은 patternIntervalTime 간격으로 생성됨")]
    public List<ObjectSpawnRow> rows = new List<ObjectSpawnRow>();
}

[CreateAssetMenu(fileName = "CreateObjectSO", menuName = "Scriptable Objects/CreateObjectSO")]
public class CreateObjectSO : ScriptableObject
{
    public List<ObjectSpawnDataSet> objectSpawnDatas = new List<ObjectSpawnDataSet>();

    [Tooltip("패턴 내 각 줄 사이의 생성 간격 (초)")]
    public float patternIntervalTime = 0.5f;
}