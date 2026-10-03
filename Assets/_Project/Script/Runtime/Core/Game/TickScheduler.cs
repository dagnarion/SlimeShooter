using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gọi Tick cho các system gameplay. Model/system đăng ký vào đây thay vì tự có Update,
/// nhờ vậy test có thể tick thủ công với deltaTime bất kỳ.
/// </summary>
public class TickScheduler
{
    private readonly List<ITickable> _tickables = new List<ITickable>();
    private readonly List<ITickable> _pendingAdd = new List<ITickable>();
    private readonly List<ITickable> _pendingRemove = new List<ITickable>();
    private bool _isTicking;
    private float _timeScale = 1f;

    public float TimeScale
    {
        get => _timeScale;
        set => _timeScale = Mathf.Max(0f, value);
    }

    public int Count => _tickables.Count;

    public void Register(ITickable tickable)
    {
        if (tickable == null) return;
        if (_isTicking)
        {
            _pendingRemove.Remove(tickable);
            if (!_tickables.Contains(tickable) && !_pendingAdd.Contains(tickable)) _pendingAdd.Add(tickable);
            return;
        }
        if (!_tickables.Contains(tickable)) _tickables.Add(tickable);
    }

    public void Unregister(ITickable tickable)
    {
        if (tickable == null) return;
        if (_isTicking)
        {
            _pendingAdd.Remove(tickable);
            if (_tickables.Contains(tickable) && !_pendingRemove.Contains(tickable)) _pendingRemove.Add(tickable);
            return;
        }
        _tickables.Remove(tickable);
    }

    public void Tick(float deltaTime)
    {
        float scaled = deltaTime * _timeScale;
        if (scaled <= 0f) return;

        _isTicking = true;
        try
        {
            for (int i = 0; i < _tickables.Count; i++)
            {
                if (_pendingRemove.Contains(_tickables[i])) continue;
                _tickables[i].Tick(scaled);
            }
        }
        finally
        {
            _isTicking = false;
            FlushPending();
        }
    }

    private void FlushPending()
    {
        foreach (var tickable in _pendingRemove) _tickables.Remove(tickable);
        foreach (var tickable in _pendingAdd)
        {
            if (!_tickables.Contains(tickable)) _tickables.Add(tickable);
        }
        _pendingRemove.Clear();
        _pendingAdd.Clear();
    }
}
