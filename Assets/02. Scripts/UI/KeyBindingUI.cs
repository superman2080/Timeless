using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 키 매핑 창. 게임 씬에서는 InputHandler의 액션에 바로 적용, Title에서는 자체 인스턴스에 적용 후 저장
// 이미 다른 칸이 쓰던 키를 지정하면 새 칸에 할당하고 기존 칸은 바인딩 해제(None)
public class KeyBindingUI : MonoBehaviour
{
    [SerializeField] private KeyRebindButton[] buttons;
    [SerializeField] private Button resetButton;

    // UIStack이 리바인딩 취소용 ESC를 메뉴 닫기로 처리하지 않도록
    public static bool IsRebinding { get; private set; }
    public static int LastRebindEndFrame { get; private set; } = -1;

    private PlayerInput actions;
    private bool ownsActions;
    private InputActionRebindingExtensions.RebindingOperation operation;

    private void EnsureInit()
    {
        if (actions != null)
            return;

        var handler = FindAnyObjectByType<InputHandler>();
        if (handler != null)
        {
            actions = handler.Actions;
        }
        else
        {
            actions = new PlayerInput();
            KeyBindingStore.Load(actions.asset);
            ownsActions = true;
        }

        foreach (var button in buttons)
            button.Init(actions, this);
        resetButton.onClick.AddListener(ResetToDefault);
    }

    public void Refresh()
    {
        EnsureInit();
        foreach (var button in buttons)
            button.Refresh();
    }

    public void StartRebind(KeyRebindButton target)
    {
        CancelRebind();
        if (target.BindingIndex < 0)
            return;

        var action = target.Action;
        bool wasEnabled = action.enabled;
        action.Disable();

        // 선택된 키 버튼이 남아 있으면 Space/Enter가 UI Submit으로 버튼을 다시 눌러 리바인딩이 재시작됨
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        IsRebinding = true;
        target.SetWaiting();
        operation = action.PerformInteractiveRebinding(target.BindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsExcluding("<Keyboard>/escape")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(_ =>
            {
                UnbindDuplicates(target);
                KeyBindingStore.Save(actions.asset);
                EndRebind(action, wasEnabled);
            })
            .OnCancel(_ => EndRebind(action, wasEnabled))
            .Start();
    }

    // 새 키를 이미 쓰고 있던 다른 칸은 빈 경로로 오버라이드해 해제
    private void UnbindDuplicates(KeyRebindButton target)
    {
        string path = target.EffectivePath;
        foreach (var button in buttons)
        {
            if (button == target || button.BindingIndex < 0)
                continue;
            if (button.EffectivePath == path)
                button.Action.ApplyBindingOverride(button.BindingIndex, string.Empty);
        }
    }

    private void EndRebind(InputAction action, bool wasEnabled)
    {
        operation?.Dispose();
        operation = null;
        IsRebinding = false;
        LastRebindEndFrame = Time.frameCount;
        if (wasEnabled)
            action.Enable();
        Refresh();
    }

    public void CancelRebind()
    {
        operation?.Cancel();
    }

    private void ResetToDefault()
    {
        CancelRebind();
        KeyBindingStore.ResetAll(actions.asset);
        Refresh();
    }

    private void OnDisable()
    {
        CancelRebind();
    }

    private void OnDestroy()
    {
        operation?.Dispose();
        IsRebinding = false;
        if (ownsActions)
            actions.Dispose();
    }
}
