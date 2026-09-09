using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EventChanelSO<T> : ScriptableObject
{
    private readonly List<EventListener<T>> _eventRaised = new List<EventListener<T>>();
    public event Action<T> OnEventRaised;
    
    private bool _isRunning;


    public void EventRaise(T value)
    {
        if (_isRunning)
        {
            Debug.LogWarning("RaiseEvent called while event is already running (reentrancy blocked).");
            return;
        }
        try
        {
            _isRunning = true;
            if (_eventRaised.Count == 0 && OnEventRaised == null)
            {
                Debug.Log("EventRaise called but no listeners or subscribers.");
                return;
            }
            if (_eventRaised != null)
            {
                List<EventListener<T>> snapshot = new List<EventListener<T>>(_eventRaised);
                foreach (var listener in snapshot)
                {
                    try
                    {
                        listener?.Raise(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("RaiseEvent called but exception " + ex.Message);
                    }
                }
            }
            var handlers = OnEventRaised;
            if (handlers != null)
            {
                Delegate[] handlerList = handlers.GetInvocationList();
                foreach (var handler in handlerList)
                {
                    try
                    {
                        ((Action<T>)handler)?.Invoke(value);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("RaiseEvent called but exception: " + e.Message);
                    }
                }
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void AddListener(EventListener<T> listener)
    {
        if (listener == null)
        {
            Debug.LogWarning("AddListener called but no listeners are registered.");
            return;
        }
        if (!_eventRaised.Contains(listener))
        {
            _eventRaised.Add(listener);
            Debug.Log("Listener added: " + listener);
        }
    }

    public void RemoveListener(EventListener<T> listener)
    {
        if (listener == null)
        {
            Debug.LogWarning("RemoveListener called but no listeners are registered.");
            return;
        }
        if (_eventRaised.Contains(listener))
        {
            Debug.Log("Listener removed: " + listener);
            _eventRaised.Remove(listener);
        }
    }
}