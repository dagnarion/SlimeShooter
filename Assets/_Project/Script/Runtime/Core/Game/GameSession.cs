using System;
using R3;
using UnityEngine;

public class GameSession : IDisposable
{
    private readonly ReactiveProperty<GameState> _state = new ReactiveProperty<GameState>(GameState.Loading);

    public ReadOnlyReactiveProperty<GameState> State => _state;
    public GameState CurrentState => _state.Value;
    public bool IsPlaying => _state.Value == GameState.Playing;
    public bool IsFinished => _state.Value == GameState.Won || _state.Value == GameState.Lost;

    public bool StartPlaying() => TryTransition(GameState.Loading, GameState.Playing);
    public bool Pause() => TryTransition(GameState.Playing, GameState.Paused);
    public bool Resume() => TryTransition(GameState.Paused, GameState.Playing);
    public bool Win() => TryTransition(GameState.Playing, GameState.Won);
    public bool Lose() => TryTransition(GameState.Playing, GameState.Lost);

    /// <summary>Retry / next level: mọi trạng thái đều có thể quay về Loading.</summary>
    public void Reload() => _state.Value = GameState.Loading;

    private bool TryTransition(GameState from, GameState to)
    {
        if (_state.Value != from)
        {
            Debug.LogWarning($"[GameSession] Invalid transition {_state.Value} -> {to} (expected from {from})");
            return false;
        }

        _state.Value = to;
        return true;
    }

    public void Dispose()
    {
        _state.Dispose();
    }
}
