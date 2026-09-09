using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventChannel", menuName = "EventChannelSO/EventChannelSO")]
public class VoidEventChanelSO : ScriptableObject
{
        
    //-----Reentrancy Safe-----
    private bool _isRunning;
    
    //-----Event-----
    public delegate void OnHandler();
    private event OnHandler EventRaised;
    public event Action OnEventRaised;
    public void EventRaise()
    {
        if (_isRunning)
        {
            Debug.LogWarning("RaiseEvent called while event is already running (reentrancy blocked).");
            return;
        }
        try
        {
            _isRunning = true;
            if (EventRaised != null)
            {
                Delegate[] listener = EventRaised.GetInvocationList();
                foreach (var lis in listener)
                {
                    try
                    {
                        ((OnHandler)lis)?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("RaiseEvent called but exception: " + e.Message);
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
                        ((Action)handler)?.Invoke();
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

    public void AddListener(OnHandler listener)
    {
        if (listener == null)
        {
            Debug.LogWarning("AddListener called with null listener argument.");
            return;
        }
        EventRaised += listener;
    }

    public void RemoveListener(OnHandler listener)
    {
        if (listener == null)
        {
            Debug.LogWarning("RemoveListener called with null listener argument.");
            return;
        }
        EventRaised -= listener;
    }
    
}