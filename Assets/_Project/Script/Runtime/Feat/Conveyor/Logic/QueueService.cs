using System;
using System.Collections.Generic;
using UnityEngine;

public class QueueService
{
    private readonly List<IGamePieces> _queue = new List<IGamePieces>();
    
    private readonly Vector3 entryPosition;
    private readonly float itemHeight;
    private readonly float dropDuration;

    public QueueService(Vector3 entryPosition, float itemHeight, float dropDuration)
    {
        this.entryPosition = entryPosition;
        this.itemHeight = itemHeight;
        this.dropDuration = dropDuration;
    }
    
    public void AddToQueue(IGamePieces gamePieces)
    {
        _queue.Add(gamePieces);
        Vector3 newStackPosition = entryPosition + Vector3.up * (itemHeight  * _queue.Count);
        
        gamePieces.Movement.PlayJumpTo(newStackPosition, dropDuration);
    }
    
    public int GetQueueCount()
    {
        return _queue.Count;
    }

    public IGamePieces GetFirstQueue()
    {
        var unit = _queue[0];
        _queue.RemoveAt(0);
        return unit;
    }

    public void RestackQueue()
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            Vector3 newStackPosition = entryPosition + Vector3.up * (itemHeight * (i + 1));
            var unit = _queue[i];
            unit.Movement.PlayDropTo(newStackPosition, dropDuration);
        }
    }

    public void RemoveFromQueue(IGamePieces gamePieces)
    {
        _queue.Remove(gamePieces);
        gamePieces.Movement?.DetachFromBelt();
        if(gamePieces.Movement is IDisposable disposable)
        {
            disposable.Dispose();
        }
        RestackQueue();
    }

    public bool IsQueueHasUnit(IGamePieces unit)
    {
        return _queue.Contains(unit);
    }
    
}
