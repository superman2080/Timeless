using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Animator animator { get; private set; }
    private static int jumpId = Animator.StringToHash("Jump");

    private void Start()
    {
        animator = gameObject.GetComponent<Animator>();
    }

    public void Jump()
    {
        animator.SetTrigger(jumpId);
    }
}
