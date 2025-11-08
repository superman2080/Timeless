using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : InteractionObject
{
    public Rigidbody rb { get; private set; }
    private InputHandler handler;
    private PlayerAnimator playerAnimator;

    #region Relate to Lane
    public int currentLane;
    private Coroutine changeLaneCor;
    #endregion

    public bool IsJump { get; private set; }
    private Coroutine jumpCoroutine;

    private void Start()
    {
        handler = gameObject.GetComponent<InputHandler>();
        playerAnimator = gameObject.GetComponent<PlayerAnimator>();
        rb = gameObject.GetComponent<Rigidbody>();
        currentLane = Mathf.RoundToInt(GameManager.Instance.laneManager.laneLength / 2);
    }

    private void Update()
    {
        Move();
    }

    void Move()
    {
        if (handler.MoveInput != 0)
        {
            ChangeLane(currentLane + Convert.ToInt32(handler.MoveInput));
        }
        if (handler.Jump)
        {
            Jump();
        }
    }

    public void Jump()
    {
        if (jumpCoroutine != null)
            return;

        jumpCoroutine = StartCoroutine(JumpCoroutine());
        playerAnimator.Jump();
    }
    public void ChangeLane(int laneIndex)
    {
        if (GameManager.Instance.nowViewMode == ViewMode.View2D ||
            laneIndex < 0 ||
            laneIndex > GameManager.Instance.laneLength - 1 ||
            changeLaneCor != null)
            return;

        changeLaneCor = StartCoroutine(ChangeLaneCoroutine(laneIndex, 0.1f));
    }

    private IEnumerator ChangeLaneCoroutine(int laneIndex, float changeTime = 0.2f)
    {
        var origin = rb.position;
        var target = new Vector3(GameManager.Instance.laneManager.lanes[laneIndex].LaneX, rb.position.y, rb.position.z);

        for (float elapsedTime = 0f; elapsedTime < changeTime; elapsedTime += Time.fixedDeltaTime)
        {
            float t = elapsedTime / changeTime;
            var step = Vector3.Lerp(origin, target, t);
            rb.MovePosition(step);
            yield return new WaitForFixedUpdate();
        }

        rb.position = target;
        currentLane = laneIndex;
        changeLaneCor = null;
    }

    private IEnumerator JumpCoroutine()
    {
        IsJump = true;
        yield return new WaitUntil(() => Utils.IsAnimationTerminated(playerAnimator.animator, 0, "Jump"));
        IsJump = false;
        jumpCoroutine = null;
    }

}