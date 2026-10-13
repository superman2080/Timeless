using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Animator animator { get; private set; }
    private static int jumpId = Animator.StringToHash("Jump");
    private static int jumpSpeedId = Animator.StringToHash("JumpSpeed");
    [SerializeField] private AnimationClip jumpClip;

    private void Start()
    {
        animator = gameObject.GetComponent<Animator>();
    }

    public void Jump(float airTime)
    {
        // 점프 클립이 체공 시간 동안 재생되도록 속도 맞춤
        animator.SetFloat(jumpSpeedId, jumpClip.length / airTime);
        animator.SetTrigger(jumpId);
    }
}
