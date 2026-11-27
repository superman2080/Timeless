using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float MoveInput { get; private set; }

    public bool Jump { get; private set; }

    // 키 매핑 UI가 이 인스턴스에 리바인딩해야 실제 조작에 반영된다
    public PlayerInput Actions => input;

    private PlayerInput input;

    void Awake()
    {
        input = new PlayerInput();
        KeyBindingStore.Load(input.asset);
    }

    private void OnEnable()
    {
        input.Enable();
        input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<float>();
        input.Player.Move.canceled += ctx => MoveInput = 0;

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

    // 메뉴가 열린 동안 게임 입력 차단 (Player.Move는 Update라 timeScale 0에서도 입력을 처리함)
    public void SetGameplayEnabled(bool on)
    {
        if (on)
        {
            input.Player.Enable();
        }
        else
        {
            input.Player.Disable();
            MoveInput = 0;
            Jump = false;
        }
    }
}
