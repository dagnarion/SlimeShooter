using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class ConveyorManager : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private ConveyorDataSO data;
    
    private readonly List<IMovable> _active = new List<IMovable>();
    private readonly List<IMovable> _queue = new List<IMovable>();

    private bool _isEndgameRush = false;
    private int _currentBeltCapacity;
    private int _currentQueueCapacity;

    private void Start()
    {
        _currentBeltCapacity = data.MaxBeltCapacity;
        _currentQueueCapacity = data.MaxQueueCapacity;
    }

    private void Update()
    {
        if (!splineContainer) return;
        
        float deltaTime = Time.deltaTime;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            IMovable movable = _active[i];
            movable.Tick(deltaTime, data.MoveSpeed, splineContainer, _isEndgameRush);
        }

        TryDispatchFromQueue();
    }

    public bool AddItemToConveyor(IMovable unit)
    {
        if (!splineContainer || !CanAcceptToConveyor()) return false;
        if (_queue.Count == 0 && IsEntranceClear())
        {
            AddToBelt(unit);
        }
        else
        {
            AddToQueue(unit);
        }

        return true;
    }

    public void HandleFollowerLap(IMovable unit)
    {
        if (unit == null) return;
        if (_isEndgameRush) return;
        
        _active.Remove(unit);
        
        //TODO : Băn sự kiện đưa ynit này xuống waitline
        
        TryDispatchFromQueue();
    }

    private void AddToQueue(IMovable unit)
    {
        _queue.Add(unit);
        Vector3 entryPosition = GetBeltEntry();
        Vector3 newStackPosition = entryPosition + Vector3.up * (data.ItemHeight * _queue.Count);
        
        unit.PlayJumpTo(newStackPosition, data.DropDuration);
    }

    private void AddToBelt(IMovable unit)
    {
        _active.Add(unit);
            
        Vector3 entryPosition = GetBeltEntry();
        unit.PlayJumpTo(entryPosition, data.JumpDuration, () =>
        {
            unit.AttachToBelt(splineContainer, data.StartPointInConveyor, data.EndPointInConveyor);
        });
    }
    
    private void TryDispatchFromQueue()
    {
        if (_queue.Count == 0 || !IsEntranceClear()) return;
        
        var unit = _queue[0];
        _queue.RemoveAt(0);
        _active.Add(unit);

        Vector3 entryPosition = GetBeltEntry();
        
        unit.PlayJumpTo(entryPosition, data.JumpDuration, () =>
        {
            unit.AttachToBelt(splineContainer, data.StartPointInConveyor, data.EndPointInConveyor);
        });
        
        RestackQueue();
    }

    private bool CanAcceptToConveyor()
    {
        if (!splineContainer) return false;
        return (_active.Count + _queue.Count) <  _currentBeltCapacity;
    }

    private bool IsEntranceClear()
    {
        float safeDistance = data.SafeDistance;
        float splineLength = splineContainer.CalculateLength();
        float entryDist = splineLength + safeDistance;
        
        foreach(var unit in _active)
        {
            float dist = unit.GetDistanceOnBelt();
            float diff = Mathf.Abs(dist - entryDist);

            if (diff < safeDistance) return false;
        }

        return true;
    }

    private Vector3 GetBeltEntry()
    {
        splineContainer.Evaluate(data.StartPointInConveyor, out var pos, out _, out _);
        return pos;
    }

    private void RestackQueue()
    {
        Vector3 entryPosition = GetBeltEntry();
        for (int i = 0; i < _queue.Count; i++)
        {
            Vector3 newStackPosition = entryPosition + Vector3.up * (data.ItemHeight * (i + 1));
            var unit = _queue[i];
            unit.PlayJumpTo(newStackPosition, data.DropDuration);
        }
    }
    
}
