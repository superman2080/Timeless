using UnityEngine;

// 슈퍼스타 효과: 지속 시간 동안 무지개색으로 발광, 끝나기 직전엔 발광이 깜빡임. 무적/맵 속도는 PlayerEffectSO 필드로 설정
[CreateAssetMenu(fileName = "Effect_", menuName = "Scriptable Objects/Player/Star Effect")]
public class StarEffectSO : PlayerEffectSO
{
    [Header("Glow")]
    [Tooltip("무지개 한 바퀴 도는 시간(초)")]
    public float rainbowPeriod = 0.5f;
    [Tooltip("발광 세기 (HDR 배율)")]
    public float glowIntensity = 2f;
    [Tooltip("남은 시간이 이 값(초) 이하가 되면 깜빡임")]
    public float blinkTime = 1f;
    [Tooltip("깜빡임 켜짐/꺼짐 한 단계 시간(초)")]
    public float blinkInterval = 0.1f;

    public override void OnUpdate(Player player, float deltaTime)
    {
        // 종료 예고: 남은 시간이 blinkTime 이하면 발광을 번갈아 끔
        bool off = player.StateMachine.GetRemainingTime(this) <= blinkTime
            && Mathf.Repeat(Time.time, blinkInterval * 2f) >= blinkInterval;
        player.SetGlow(off ? Color.black
            : Color.HSVToRGB(Mathf.Repeat(Time.time / rainbowPeriod, 1f), 1f, 1f) * glowIntensity);
    }

    public override void OnExit(Player player) => player.SetGlow(Color.black);
}
