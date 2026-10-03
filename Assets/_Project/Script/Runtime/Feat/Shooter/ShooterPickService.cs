using System;
using R3;

/// <summary>
/// Cổng duy nhất để "chọn" một shooter: chỉ khi đang Playing và container cho phép.
/// Băng chuyền (P5) subscribe <see cref="OnPicked"/> để nhận shooter.
/// </summary>
public class ShooterPickService : IDisposable
{
    private readonly GameSession _session;
    private readonly Subject<ShooterModel> _onPicked = new Subject<ShooterModel>();
    private readonly Subject<ShooterModel> _onPickRejected = new Subject<ShooterModel>();

    public ShooterPickService(GameSession session)
    {
        _session = session;
    }

    public Observable<ShooterModel> OnPicked => _onPicked;

    /// <summary>Bấm vào shooter nhưng không được chọn (không đứng đầu cột...). Dùng cho feedback.</summary>
    public Observable<ShooterModel> OnPickRejected => _onPickRejected;

    public bool TryPick(ShooterModel shooter)
    {
        if (shooter == null || !_session.IsPlaying) return false;

        var container = shooter.Container;
        if (container == null || !container.CanPick(shooter))
        {
            _onPickRejected.OnNext(shooter);
            return false;
        }

        _onPicked.OnNext(shooter);
        return true;
    }

    public void Dispose()
    {
        _onPicked.Dispose();
        _onPickRejected.Dispose();
    }
}
