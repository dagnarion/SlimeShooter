using System;
using R3;

/// <summary>Thắng thì lưu tiến trình ngay (tắt game ở màn Win vẫn sang level mới).</summary>
public class LevelProgressSystem : IDisposable
{
    private readonly IDisposable _subscription;

    public LevelProgressSystem(GameSession session, LevelService levels)
    {
        PlayedLevelNumber = levels.DisplayNumber;
        _subscription = session.State
            .Where(state => state == GameState.Won)
            .Take(1)
            .Subscribe(_ => levels.Advance());
    }

    /// <summary>Số level của ván đang chơi (giữ nguyên kể cả sau khi đã Advance).</summary>
    public int PlayedLevelNumber { get; }

    public void Dispose() => _subscription.Dispose();
}
