using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ESC 메뉴용 UI 스택. ESC는 비어 있으면 rootPanel을 열고, 아니면 맨 위 UI 하나만 닫는다
// 첫 Push에서 게임 정지(오디오 일시정지 포함) + 게임 입력 차단, 스택이 비면 복원
public class UIStack : MonoBehaviour
{
    [SerializeField] private UIPanel rootPanel;
    [Tooltip("열려 있는 동안 Time.timeScale = 0 (Title에서는 끔)")]
    [SerializeField] private bool pauseGame = true;

    private readonly Stack<UIPanel> stack = new();
    private float prevTimeScale = 1f;
    private InputHandler inputHandler;

    public int Count => stack.Count;

    // Keyboard.current는 마지막 사용 키보드 하나만 보므로, 모든 키보드에 바인딩되는 액션으로 ESC 처리
    private InputAction escAction;

    private void Awake()
    {
        foreach (var panel in GetComponentsInChildren<UIPanel>(true))
        {
            panel.Stack = this;
            panel.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        inputHandler = FindAnyObjectByType<InputHandler>();
    }

    private void OnEnable()
    {
        escAction = new InputAction("UIStackEscape", binding: "<Keyboard>/escape");
        escAction.performed += _ => OnEscape();
        escAction.Enable();
    }

    private void OnDisable()
    {
        escAction.Disable();
        escAction.Dispose();
    }

    private void OnEscape()
    {
        // 리바인딩 중 ESC는 리바인딩 취소 용도. 콜백 순서가 보장되지 않으므로 끝난 프레임까지 무시
        if (KeyBindingUI.IsRebinding || KeyBindingUI.LastRebindEndFrame == Time.frameCount)
            return;

        if (stack.Count == 0)
            Push(rootPanel);
        else
            Pop();
    }

    // 버튼 등 외부에서 메뉴를 열 때 (Title의 Settings 버튼)
    public void OpenRoot() => Push(rootPanel);

    public void Push(UIPanel panel)
    {
        if (panel == null || stack.Contains(panel))
            return;

        if (stack.Count == 0)
        {
            // 컷씬/크기 변경 연출 등 이미 정지 중이면 열지 않음 (timeScale 복원 충돌 방지)
            if (pauseGame && Time.timeScale == 0f)
                return;
            Pause(true);
        }

        panel.Stack = this;
        stack.Push(panel);
        panel.OnPush();
    }

    public void Pop()
    {
        if (stack.Count == 0)
            return;

        stack.Pop().OnPop();
        if (stack.Count == 0)
            Pause(false);
    }

    public void Clear()
    {
        while (stack.Count > 0)
            Pop();
    }

    private void Pause(bool on)
    {
        if (pauseGame)
        {
            // AudioSource는 timeScale 영향을 받지 않으므로 BGM 포함 전체 오디오를 직접 일시정지
            var audioPool = Pool<PooledAudio>.Instance as AudioPool;
            if (on)
            {
                prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                audioPool?.PauseAll();
            }
            else
            {
                Time.timeScale = prevTimeScale;
                audioPool?.ResumeAll();
            }
        }

        if (inputHandler != null)
            inputHandler.SetGameplayEnabled(!on);

        if (!on)
            PlayerPrefs.Save();
    }
}
