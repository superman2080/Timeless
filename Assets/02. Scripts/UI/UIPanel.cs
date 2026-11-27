using UnityEngine;

// UIStack에 쌓이는 UI 단위. 기본 동작은 Push 시 활성, Pop 시 비활성
public class UIPanel : MonoBehaviour
{
    public UIStack Stack { get; set; }

    public virtual void OnPush()
    {
        gameObject.SetActive(true);
    }

    public virtual void OnPop()
    {
        gameObject.SetActive(false);
    }
}
