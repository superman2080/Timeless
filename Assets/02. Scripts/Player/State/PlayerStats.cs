using System;
using UnityEngine;

// 형태/효과가 바꿀 수 있는 플레이어 스탯. PlayerStateMachine이 합성하고 Player.ApplyStats가 반영
[Serializable]
public struct PlayerStats
{
    [Tooltip("플레이어 루트 스케일")]
    public float scale;
    [Tooltip("최대 HP. 늘어나면 늘어난 만큼 HP도 회복, 줄어들면 최대치로 자름")]
    public float maxHP;
    [Tooltip("월드 속도 배율 (GameManager.mapSpeed = 시작 속도 × 배율)")]
    public float mapSpeedMultiplier;
    [Tooltip("점프 높이 배율")]
    public float jumpHeightMultiplier;
    [Tooltip("체공 시간 배율")]
    public float airTimeMultiplier;
    [Tooltip("시간당 HP 감소 배율")]
    public float hpDecrementMultiplier;
    [Tooltip("무적: 피격과 시간당 HP 감소 무시")]
    public bool invincible;

    public static PlayerStats Default => new PlayerStats
    {
        scale = 0.6f,
        maxHP = 100f,
        mapSpeedMultiplier = 1f,
        jumpHeightMultiplier = 1f,
        airTimeMultiplier = 1f,
        hpDecrementMultiplier = 1f,
        invincible = false,
    };
}
