using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class ConveyorManager : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private ConveyorDataSO data;
    [SerializeField] private MovableEventChanelSO onItemEntered;
    [SerializeField] private MovableEventChanelSO onItemExited;
    [SerializeField] private bool _isEndgameRush = false;
    
    private readonly List<IMovable> _active = new List<IMovable>();
    private readonly List<IMovable> _queue = new List<IMovable>();

    private int _currentBeltCapacity;

    public bool IsEndgameRush
    {
        get => _isEndgameRush;
        set => _isEndgameRush = value;
    }

    public MovableEventChanelSO OnItemEntered => onItemEntered;
    public MovableEventChanelSO OnItemExited => onItemExited;

    private void OnEnable()
    {
        if (onItemEntered != null)
        {
            onItemEntered.OnEventRaised += HandleItemEntered;
        }
    }

    private void OnDisable()
    {
        if (onItemEntered != null)
        {
            onItemEntered.OnEventRaised -= HandleItemEntered;
        }
    }

    private void HandleItemEntered(IMovable unit)
    {
        AddItemToConveyor(unit);
    }

    private void Start()
    {
        if (data != null)
        {
            _currentBeltCapacity = data.MaxBeltCapacity;
        }
    }

    private void Update()
    {
        if (!splineContainer) return;
        
        float deltaTime = Time.deltaTime;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            IMovable movable = _active[i];
            bool reachedExit = movable.Tick(deltaTime, data.MoveSpeed, splineContainer, _isEndgameRush);
            if (!reachedExit || _isEndgameRush) continue;

            _active.RemoveAt(i);
            onItemExited?.EventRaise(movable);
        }

        TryDispatchFromQueue();
    }

    public bool AddItemToConveyor(IMovable unit)
    {
        if (unit == null || !splineContainer || !CanAcceptToConveyor()) return false;
        if (_queue.Count == 0 && IsEntranceClear())
        {
            AddToBelt(unit);
        }
        else
        {
            AddToQueue(unit);
        }

        unit.OnAccepted?.Invoke();
        return true;
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
        
        unit.PlayDropTo(entryPosition, data.JumpDuration, () =>
        {
            unit.AttachToBelt(splineContainer, data.StartPointInConveyor, data.EndPointInConveyor);
        });
        
        RestackQueue();
    }

    public bool CanAcceptToConveyor()
    {
        if (!splineContainer) return false;
        int maxCapacity = _currentBeltCapacity > 0 ? _currentBeltCapacity : (data != null ? data.MaxBeltCapacity : 5);
        return (_active.Count + _queue.Count) < maxCapacity;
    }

    public bool IsEntranceClear()
    {
        if (!splineContainer || data == null) return false;
        float safeDistance = data.SafeDistance;
        float splineLength = splineContainer.CalculateLength();
        float entryDist = data.StartPointInConveyor * splineLength;
        float exitDist = data.EndPointInConveyor * splineLength;

        // Khoảng cách an toàn phía sau cần cộng thêm quãng đường unit sẽ di chuyển trong lúc unit mới đang jump/drop (0.5s)
        float safeDistanceBehind = safeDistance + data.MoveSpeed * data.JumpDuration;

        foreach (var unit in _active)
        {
            if (!unit.IsAttached) return false;
            float dist = unit.GetDistanceOnBelt();

            // 1. Kiểm tra phía trước lối vào (unit đang rời khỏi lối vào)
            float distAhead = dist - entryDist;
            if (distAhead >= 0 && distAhead < safeDistance) return false;

            // 2. Nếu đang loop, kiểm tra khoảng cách từ phía sau (unit đang tiến tới endpoint để loop về lối vào)
            if (_isEndgameRush)
            {
                float distBehind = exitDist - dist;
                if (distBehind >= 0 && distBehind < safeDistanceBehind) return false;
            }
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
            unit.PlayDropTo(newStackPosition, data.DropDuration);
        }
    }
}
