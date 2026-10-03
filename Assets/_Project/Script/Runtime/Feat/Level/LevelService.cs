using System;
using R3;

/// <summary>Quản lý level hiện tại và lưu tiến trình.</summary>
public class LevelService : IDisposable
{
    public const string CurrentLevelKey = "level.current";

    private readonly LevelDatabaseSO _database;
    private readonly ISaveService _save;
    private readonly ReactiveProperty<int> _currentIndex;

    public LevelService(LevelDatabaseSO database, ISaveService save)
    {
        _database = database;
        _save = save;
        _currentIndex = new ReactiveProperty<int>(ClampIndex(save.GetInt(CurrentLevelKey, 0)));
    }

    public ReadOnlyReactiveProperty<int> CurrentIndex => _currentIndex;

    /// <summary>Số thứ tự hiển thị cho người chơi (bắt đầu từ 1).</summary>
    public int DisplayNumber => _currentIndex.Value + 1;

    public LevelSO Current => _database.Get(_currentIndex.Value);

    /// <summary>Sang level tiếp theo; hết danh sách thì quay lại LoopStartIndex.</summary>
    public void Advance()
    {
        int next = _currentIndex.Value + 1;
        if (next >= _database.Count) next = _database.LoopStartIndex;
        SetIndex(next);
    }

    public void SetIndex(int index)
    {
        _currentIndex.Value = ClampIndex(index);
        _save.SetInt(CurrentLevelKey, _currentIndex.Value);
        _save.Save();
    }

    private int ClampIndex(int index)
    {
        if (_database == null || _database.Count == 0) return 0;
        if (index < 0) return 0;
        return index >= _database.Count ? _database.LoopStartIndex : index;
    }

    public void Dispose()
    {
        _currentIndex.Dispose();
    }
}
