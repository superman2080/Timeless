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
public class ObjectSpawnDataSet
{
    public ObjectSpawnData[] objectSpawnDatas = new ObjectSpawnData[3];
}

[CreateAssetMenu(fileName = "CreateObjectSO", menuName = "Scriptable Objects/CreateObjectSO")]
public class CreateObjectSO : ScriptableObject
{
    public ObjectSpawnDataSet[] objectSpawnDatas;
}
