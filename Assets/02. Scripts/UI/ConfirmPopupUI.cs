using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 예/아니오 확인 팝업. No 또는 ESC는 팝업만 닫는다
public class ConfirmPopupUI : UIPanel
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirm;

    private void Awake()
    {
        yesButton.onClick.AddListener(() => onConfirm?.Invoke());
        noButton.onClick.AddListener(() => Stack.Pop());
    }

    public void Show(string message, Action confirm)
    {
        messageText.text = message;
        onConfirm = confirm;
        Stack.Push(this);
    }

    public override void OnPop()
    {
        onConfirm = null;
        base.OnPop();
    }
}
