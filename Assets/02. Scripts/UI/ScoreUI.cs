using TMPro;
using UnityEngine;

// 시간에 따라 점수가 오르고, milestoneInterval마다 텍스트가 커졌다가 원래 크기로 돌아온다.
// 사망 시 정지는 같은 오브젝트의 EventListener(onPlayerDied)가 enabled를 끈다
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField, Min(0f)] private float scorePerSecond = 100f;

    [Header("Emphasis")]
    [Tooltip("이 점수의 배수를 넘을 때마다 강조")]
    [SerializeField, Min(1)] private int milestoneInterval = 1000;
    [Tooltip("강조 시 확대 배율 (1.3 = 1.3배)")]
    [SerializeField, Min(1f)] private float punchScale = 1.3f;
    [Tooltip("원래 크기로 돌아오는 시간 (초)")]
    [SerializeField, Min(0.01f)] private float punchDuration = 0.3f;

    private float score;
    private float punchTimer;

    public int Score => (int)score;

    // 아이템 등으로 얻는 점수. 마일스톤과 상관없이 강조
    public void AddScore(int amount)
    {
        score += amount;
        punchTimer = punchDuration;
    }

    private void Update()
    {
        int prevMilestone = Score / milestoneInterval;
        score += scorePerSecond * Time.deltaTime;
        if (Score / milestoneInterval > prevMilestone)
            punchTimer = punchDuration;

        scoreText.text = Score.ToString();

        punchTimer = Mathf.Max(0f, punchTimer - Time.deltaTime);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale,punchTimer / punchDuration);
    }
}
