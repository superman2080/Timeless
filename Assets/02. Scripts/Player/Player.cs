using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : InteractionObject
{
    public Rigidbody rb { get; private set; }

    #region Relate to Lane
    [HideInInspector] public int currentLane;
    private InputHandler handler;
    private Coroutine changeLaneCor;
    #endregion

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Move();
    }

    void Move()
    {
        if (handler.MoveInput != Vector2.zero)
        {
            ChangeLane(currentLane + Convert.ToInt32(handler.MoveInput.x));
        }
    }

    public void ChangeLane(int laneIndex)
    {
        if (GameManager.Instance.nowViewMode == ViewMode.View2D || 
            laneIndex < 0 || 
            laneIndex > GameManager.Instance.laneLength ||
            Mathf.Abs(laneIndex - currentLane) >= 2 ||
            changeLaneCor != null)
            return;

        changeLaneCor = StartCoroutine(ChangeLaneCoroutine(laneIndex, 0.1f));
    }


    private IEnumerator ChangeLaneCoroutine(int laneIndex, float changeTime = 0.2f)
    {
        var origin = rb.position;
        var target = GameManager.Instance.laneManager.lanes[laneIndex].transform.position;

        for (float elapsedTime = 0f; elapsedTime < changeTime; elapsedTime += Time.fixedDeltaTime)
        {
            var step = Vector3.Lerp(target, origin, elapsedTime);
            rb.MovePosition(step);
            yield return new WaitForFixedUpdate();
        }
        rb.position = target;

        changeLaneCor = null;
    }
}
