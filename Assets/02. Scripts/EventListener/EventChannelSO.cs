using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventChannelSO", menuName = "Scriptable Objects/EventChannelSO")]
public class EventChannelSO : ScriptableObject
{
    private List<EventListener> listeners = new();

    public void RegisterListner(EventListener listener) => listeners.Add(listener);

    public void UnregisterListner(EventListener listener) => listeners.Remove(listener);

    public void RaiseEvent()
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised();
        }
    }
}
