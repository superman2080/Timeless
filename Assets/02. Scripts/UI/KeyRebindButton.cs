using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 키 매핑 칸 하나 = 바인딩 하나. 인덱스는 하드코딩하지 않고 composite 파트 이름 + slot 순번으로 찾는다
public class KeyRebindButton : MonoBehaviour
{
    [SerializeField] private string actionName = "Jump";
    [Tooltip("Move의 \"negative\"/\"positive\". 비우면 composite가 아닌 일반 바인딩")]
    [SerializeField] private string compositePart = "";
    [Tooltip("조건에 맞는 바인딩 중 몇 번째인지 (0 = Primary, 1 = Secondary)")]
    [SerializeField] private int slot;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public InputAction Action { get; private set; }
    public int BindingIndex { get; private set; } = -1;
    public string EffectivePath => Action.bindings[BindingIndex].effectivePath;

    public void Init(PlayerInput actions, KeyBindingUI owner)
    {
        Action = actions.asset.FindAction(actionName, true);
        BindingIndex = FindBindingIndex();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => owner.StartRebind(this));
        if (BindingIndex < 0)
            Debug.LogWarning($"{name}: {actionName}/{compositePart} slot {slot} 바인딩을 찾지 못했습니다.");
    }

    private int FindBindingIndex()
    {
        int found = 0;
        var bindings = Action.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            bool match = string.IsNullOrEmpty(compositePart)
                ? !bindings[i].isComposite && !bindings[i].isPartOfComposite
                : bindings[i].isPartOfComposite && bindings[i].name == compositePart;
            if (match && found++ == slot)
                return i;
        }
        return -1;
    }

    public void Refresh()
    {
        if (BindingIndex < 0)
            return;
        label.text = string.IsNullOrEmpty(EffectivePath) ? "없음" : Action.GetBindingDisplayString(BindingIndex);
    }

    public void SetWaiting()
    {
        label.text = "키를 누르세요...";
    }
}
