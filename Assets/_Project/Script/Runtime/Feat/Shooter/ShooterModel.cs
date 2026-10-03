using System;
using R3;

public enum ShooterState
{
    InColumn,
    MovingToBelt,
    OnBelt,
    ReturningToTray,
    InTray,
    Dead
}

public class ShooterModel : IDisposable
{
    private readonly ReactiveProperty<int> _ammo;
    private readonly ReactiveProperty<ShooterState> _state;

    public ShooterModel(int id, int colorId, int ammo)
    {
        Id = id;
        ColorId = colorId;
        _ammo = new ReactiveProperty<int>(ammo);
        _state = new ReactiveProperty<ShooterState>(ShooterState.InColumn);
    }

    public int Id { get; }
    public int ColorId { get; }
    public ReadOnlyReactiveProperty<int> Ammo => _ammo;
    public ReadOnlyReactiveProperty<ShooterState> State => _state;

    /// <summary>Container đang giữ shooter (cột hoặc khay); null khi đang trên băng chuyền.</summary>
    public IShooterContainer Container { get; set; }

    public bool HasAmmo => _ammo.Value > 0;

    /// <summary>Trừ 1 viên; trả về false nếu đã hết đạn.</summary>
    public bool ConsumeAmmo()
    {
        if (_ammo.Value <= 0) return false;
        _ammo.Value--;
        return true;
    }

    public void SetState(ShooterState state) => _state.Value = state;

    public override string ToString() => $"Shooter#{Id}(color {ColorId}, ammo {_ammo.Value}, {_state.Value})";

    public void Dispose()
    {
        _ammo.Dispose();
        _state.Dispose();
    }
}
