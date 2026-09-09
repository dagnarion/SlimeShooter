using UnityEngine;
using UnityEngine.Events;

public abstract class EventListener<T> : MonoBehaviour
{
    [SerializeField] private EventChanelSO<T> eventChanelSo;
    [SerializeField] private UnityEvent<T> unityEvent;

    public void Raise(T value)
    {
        unityEvent?.Invoke(value);
    }

    private void OnEnable()
    {
        if (eventChanelSo == null) return;
        eventChanelSo.AddListener(this);
    }

    private void OnDisable()
    {
        if (eventChanelSo == null) return;
        eventChanelSo.RemoveListener(this);
    }
}

