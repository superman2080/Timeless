using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    public bool Jump { get; private set; }

    private PlayerInput input;

    void Awake()
    {
        input = new PlayerInput();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        input.Player.Jump.performed += ctx => Jump = true;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void LateUpdate()
    {
        Jump = false;
    }
}
