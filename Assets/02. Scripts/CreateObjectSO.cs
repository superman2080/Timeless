using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType
{
    NONE,

    LASER,      // ·¹ÀÌÀú
    BARREL,     // ¹è·²
    BLOCKADE,   // ºí·Ï
    FAN,        // ÆÒ
    ROBOT_ARM,  // ·Îº¿ÆÈ

    HP,
    KEY_CARD,
}

[Serializable]
public struct ObjectSpawnData
{
    [Tooltip("½ºÆù À§Ä¡ÀÇ ¶óÀÎ ÀÎµ¦½º (0: ¿ÞÂÊ, 1: Áß¾Ó, 2: ¿À¸¥ÂÊ...)")]
    public int laneIndex;
    public ObjectType type;
}

[CreateAssetMenu(fileName = "CreateObjectSO", menuName = "Scriptable Objects/CreateObjectSO")]
public class CreateObjectSO : ScriptableObject
{
    public ObjectSpawnData[] objectSpawnDatas;
}
