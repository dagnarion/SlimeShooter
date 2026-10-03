using System;
using System.Collections.Generic;
using R3;

/// <summary>
/// Khay chờ: giữ shooter chạy hết vòng mà còn đạn. Ô trống được dồn về bên trái.
/// Mọi shooter trong khay đều chọn được (gửi lại lên băng).
/// </summary>
public class CacheTray : IShooterContainer, IDisposable
{
    private readonly List<ShooterModel> _shooters = new List<ShooterModel>();
    private readonly ReactiveProperty<int> _capacity;
    private readonly ReactiveProperty<int> _freeCount;
    private readonly Subject<Unit> _onChanged = new Subject<Unit>();

    public CacheTray(int capacity)
    {
        _capacity = new ReactiveProperty<int>(Math.Max(1, capacity));
        _freeCount = new ReactiveProperty<int>(_capacity.Value);
    }

    /// <summary>Theo thứ tự ô, index 0 là ô trái nhất.</summary>
    public IReadOnlyList<ShooterModel> Shooters => _shooters;
    public int Count => _shooters.Count;
    public ReadOnlyReactiveProperty<int> Capacity => _capacity;
    public ReadOnlyReactiveProperty<int> FreeCount => _freeCount;
    public bool IsFull => _shooters.Count >= _capacity.Value;
    public Observable<Unit> OnChanged => _onChanged;

    public bool TryAdd(ShooterModel shooter)
    {
        if (shooter == null || IsFull || _shooters.Contains(shooter)) return false;
        _shooters.Add(shooter);
        shooter.Container = this;
        shooter.SetState(ShooterState.InTray);
        Refresh();
        return true;
    }

    public bool CanPick(ShooterModel shooter) => shooter != null && _shooters.Contains(shooter);

    public bool Remove(ShooterModel shooter)
    {
        if (!_shooters.Remove(shooter)) return false;
        if (shooter.Container == this) shooter.Container = null;
        Refresh();
        return true;
    }

    /// <summary>Thêm ô (revive / booster).</summary>
    public void AddCapacity(int amount)
    {
        if (amount <= 0) return;
        _capacity.Value += amount;
        Refresh();
    }

    private void Refresh()
    {
        _freeCount.Value = Math.Max(0, _capacity.Value - _shooters.Count);
        _onChanged.OnNext(Unit.Default);
    }

    public void Dispose()
    {
        _capacity.Dispose();
        _freeCount.Dispose();
        _onChanged.Dispose();
    }
}
