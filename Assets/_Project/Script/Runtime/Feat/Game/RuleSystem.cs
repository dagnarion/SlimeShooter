using System;
using R3;

public enum LoseReason
{
    None,
    /// <summary>Shooter chạy hết vòng còn đạn nhưng khay chờ đã đầy.</summary>
    TrayFull
}

/// <summary>
/// Luật thắng/thua:
/// - Thắng: mọi ô đã Dead (đạn cuối đã tới và animation vỡ xong).
/// - Thua: shooter về mà khay không còn chỗ — trừ khi board đã hết ô Alive (đang chờ ô cuối vỡ = sắp thắng).
/// </summary>
public class RuleSystem : IDisposable
{
    private readonly GameSession _session;
    private readonly PixelBoard _board;
    private readonly CacheTray _tray;
    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

    public RuleSystem(GameSession session, PixelBoard board, ConveyorModel conveyor, CacheTray tray)
    {
        _session = session;
        _board = board;
        _tray = tray;

        conveyor.OnLapCompleted.Subscribe(OnLapCompleted).AddTo(_subscriptions);
        board.Remaining.Where(remaining => remaining <= 0).Subscribe(_ => TryWin()).AddTo(_subscriptions);
    }

    public LoseReason LoseReason { get; private set; }

    /// <summary>Shooter không vào được khay (để revive đưa lại vào khay sau khi thêm ô).</summary>
    public ShooterModel BlockedShooter { get; private set; }

    private void OnLapCompleted(ShooterModel shooter)
    {
        if (!_session.IsPlaying || !shooter.HasAmmo) return;
        if (_tray.TryAdd(shooter)) return;
        if (_board.AliveCount == 0) return; // ô cuối đang vỡ -> sẽ thắng

        BlockedShooter = shooter;
        LoseReason = LoseReason.TrayFull;
        _session.Lose();
    }

    /// <summary>Sau khi revive: xoá trạng thái thua.</summary>
    public void ClearLose()
    {
        LoseReason = LoseReason.None;
        BlockedShooter = null;
    }

    private void TryWin()
    {
        if (_session.IsPlaying) _session.Win();
    }

    public void Dispose() => _subscriptions.Dispose();
}
