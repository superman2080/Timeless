using System;
using UnityEngine;

// 값은 에셋/프리팹에 int로 직렬화되므로 새 타입은 끝에 추가할 것
public enum SfxType
{
    NONE,

    HIT,
    PICKUP,
    BGM_TRANSITION,
    BOSS_ROAR,
    BOSS_CEILING_BREAK,
}

[Serializable]
public struct SfxData
{
    public SfxType type;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
}

// SoundManager가 쓰는 클립 목록. 각 시스템은 클립 대신 SfxType으로 요청한다
[CreateAssetMenu(fileName = "AudioData", menuName = "Scriptable Objects/AudioDataSO")]
public class AudioDataSO : ScriptableObject
{
    public SfxData[] sfx;

    [Header("BGM")]
    [Tooltip("게임 시작 BGM")]
    public AudioClip bgmStart;
    [Tooltip("2D/3D 전환 시 순서대로 순환하는 BGM")]
    public AudioClip[] bgmCycle;
}
