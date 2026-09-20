using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class BeltService
{
    private readonly List<IGamePieces> _active = new List<IGamePieces>();

    private readonly SplineContainer _splineContainer;
    private readonly Vector3 _entryPosition;
    private readonly float _startPoint;
    private readonly float _endPoint;
    private readonly float _moveSpeed;
    private readonly float _safeDistance;
    private readonly float _jumpDuration;

    public BeltService(
        SplineContainer splineContainer,
        Vector3 entryPosition,
        float startPoint,
        float endPoint,
        float moveSpeed,
        float safeDistance,
        float jumpDuration)
    {
        _splineContainer = splineContainer;
        _entryPosition = entryPosition;
        _startPoint = startPoint;
        _endPoint = endPoint;
        _moveSpeed = moveSpeed;
        _safeDistance = safeDistance;
        _jumpDuration = jumpDuration;
    }
    
    public void AddDirect(IGamePieces unit)
    {
        _active.Add(unit);
        unit.Movement.PlayJumpTo(_entryPosition, _jumpDuration, () =>
        {
            unit.Movement.AttachToBelt(_splineContainer, _startPoint, _endPoint);
        });
    }

    public void AddFromQueue(IGamePieces unit)
    {
        _active.Add(unit);
        unit.Movement.PlayDropTo(_entryPosition, _jumpDuration, () =>
        {
            unit.Movement.AttachToBelt(_splineContainer, _startPoint, _endPoint);
        });
    }

    public void Tick(float deltaTime, bool isEndgameRush, Action<IGamePieces> onUnitExited)
    {
        if (!_splineContainer) return;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            IGamePieces movable = _active[i];
            bool reachedExit = movable.Movement.Tick(deltaTime, _moveSpeed, _splineContainer, isEndgameRush);
            if (!reachedExit || isEndgameRush) continue;

            _active.RemoveAt(i);
            onUnitExited?.Invoke(movable);
        }
    }

    public bool IsEntranceClear(bool isEndgameRush)
    {
        if (!_splineContainer) return false;
        
        float splineLength = _splineContainer.CalculateLength();
        float entryDist = _startPoint * splineLength;
        float exitDist = _endPoint * splineLength;
        float safeDistanceBehind = _safeDistance + _moveSpeed * _jumpDuration;

        foreach (var unit in _active)
        {
            if (!unit.Movement.IsAttached) return false;

            float dist = unit.Movement.GetDistanceOnBelt();
            
            float distAhead = dist - entryDist;
            if (distAhead >= 0 && distAhead < _safeDistance) return false;
            
            if (isEndgameRush)
            {
                float distBehind = exitDist - dist;
                if (distBehind >= 0 && distBehind < safeDistanceBehind) return false;
            }
        }
        return true;
    }

    public int GetBeltCount() => _active.Count;

    public bool IsBeltHasUnit(IGamePieces unit) => _active.Contains(unit);

    public void RemoveFromBelt(IGamePieces unit)
    {
        _active.Remove(unit);
        unit.Movement.DetachFromBelt();
        if (unit.Movement is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}