using System;
using UnityEngine;

public enum EffectType
{
    NONE,

    DUST_EXPLOSION,
    TINY_EXPLOSION,
    FIRE_BUFF,
}

[Serializable]
public struct EffectData
{
    public EffectType type;
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "EffectDataSO", menuName = "Scriptable Objects/EffectDataSO")]
public class EffectDataSO : ScriptableObject
{
    public EffectData[] effectDatas;
}
