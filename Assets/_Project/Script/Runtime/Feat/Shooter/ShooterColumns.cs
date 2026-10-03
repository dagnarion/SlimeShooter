using System;
using System.Collections.Generic;
using R3;

/// <summary>
/// Các cột shooter dựng từ LevelSO. Index 0 của mỗi cột là shooter đứng đầu,
/// chỉ shooter đứng đầu mới chọn được; lấy ra thì cả cột dồn lên.
/// </summary>
public class ShooterColumns : IShooterContainer, IDisposable
{
    private readonly List<List<ShooterModel>> _columns = new List<List<ShooterModel>>();
    private readonly List<ShooterModel> _all = new List<ShooterModel>();
    private readonly Subject<int> _onColumnChanged = new Subject<int>();

    public ShooterColumns(LevelSO level) : this(level.Columns) { }

    public ShooterColumns(IReadOnlyList<ColumnSpec> columns)
    {
        int nextId = 0;
        foreach (var spec in columns)
        {
            var column = new List<ShooterModel>();
            foreach (var shooterSpec in spec.shooters)
            {
                var shooter = new ShooterModel(nextId++, shooterSpec.colorId, shooterSpec.ammo) { Container = this };
                column.Add(shooter);
                _all.Add(shooter);
            }
            _columns.Add(column);
        }
    }

    public int ColumnCount => _columns.Count;

    /// <summary>Mọi shooter của level (kể cả đã rời cột) — để view/system tra cứu.</summary>
    public IReadOnlyList<ShooterModel> AllShooters => _all;

    /// <summary>Phát index cột vừa thay đổi (có shooter rời đi / được chèn vào).</summary>
    public Observable<int> OnColumnChanged => _onColumnChanged;

    public IReadOnlyList<ShooterModel> GetColumn(int index) => _columns[index];

    public int RemainingInColumns
    {
        get
        {
            int count = 0;
            foreach (var column in _columns) count += column.Count;
            return count;
        }
    }

    public ShooterModel GetFront(int column) => _columns[column].Count > 0 ? _columns[column][0] : null;

    public bool TryFind(ShooterModel shooter, out int column, out int row)
    {
        for (column = 0; column < _columns.Count; column++)
        {
            row = _columns[column].IndexOf(shooter);
            if (row >= 0) return true;
        }
        column = -1;
        row = -1;
        return false;
    }

    public bool CanPick(ShooterModel shooter)
    {
        return shooter != null && TryFind(shooter, out _, out int row) && row == 0;
    }

    public bool Remove(ShooterModel shooter)
    {
        if (!TryFind(shooter, out int column, out int row)) return false;

        _columns[column].RemoveAt(row);
        if (shooter.Container == this) shooter.Container = null;
        _onColumnChanged.OnNext(column);
        return true;
    }

    public void Dispose()
    {
        _onColumnChanged.Dispose();
        foreach (var shooter in _all) shooter.Dispose();
    }
}
