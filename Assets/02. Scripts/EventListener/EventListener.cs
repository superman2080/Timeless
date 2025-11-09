using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.Events;

public class EventListener : MonoBehaviour
{
    // 수신받았을 때 발생할 이벤트
    public UnityEvent response;
    // 채널: ScriptableObject
    public EventChannelSO eventChannel;

    private void OnEnable()
    {
        eventChannel.RegisterListner(this);
    }

    private void OnDisable()
    {
        eventChannel.UnregisterListner(this);
    }

    public void OnEventRaised()
    {
        response?.Invoke();
    }
}
