using System;
using R3;

/// <summary>Nối ShooterPickService với băng chuyền: được nhận thì lấy shooter ra khỏi container.</summary>
public class ConveyorController : IDisposable
{
    private readonly ConveyorModel _conveyor;
    private readonly IDisposable _subscription;

    public ConveyorController(ShooterPickService pickService, ConveyorModel conveyor)
    {
        _conveyor = conveyor;
        _subscription = pickService.OnPicked.Subscribe(OnPicked);
    }

    private void OnPicked(ShooterModel shooter) => SendToBelt(shooter);

    /// <summary>Đưa shooter lên băng (bỏ qua luật chọn — dùng cho end rush, booster Pickup...).</summary>
    public bool SendToBelt(ShooterModel shooter)
    {
        var container = shooter.Container;
        if (!_conveyor.TryInsert(shooter)) return false;
        container?.Remove(shooter);
        shooter.Container = null;
        return true;
    }

    public void Dispose() => _subscription.Dispose();
}
