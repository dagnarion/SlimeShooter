using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

/// <summary>
/// Số lượng booster (lưu qua ISaveService) và thực thi 4 booster.
/// Pickup / ColorBomb cần chọn mục tiêu: vào "chế độ chọn", cú chạm tiếp theo vào shooter được chuyển tới đây.
/// Mọi booster đều giữ cân bằng Σammo(màu) = Σô Alive(màu).
/// </summary>
public class BoosterService : IDisposable
{
    public const string SaveKeyPrefix = "booster.";

    private readonly GameSession _session;
    private readonly ISaveService _save;
    private readonly BoosterConfigSO _config;
    private readonly ConveyorModel _conveyor;
    private readonly ConveyorController _conveyorController;
    private readonly ShooterColumns _columns;
    private readonly CacheTray _tray;
    private readonly PixelBoard _board;
    private readonly ShooterPickService _pickService;
    private readonly EndRushSystem _endRush;
    private readonly int _levelNumber;
    private readonly int _seed;

    private readonly Dictionary<BoosterType, ReactiveProperty<int>> _counts = new Dictionary<BoosterType, ReactiveProperty<int>>();
    private readonly ReactiveProperty<BoosterType?> _activeMode = new ReactiveProperty<BoosterType?>(null);
    private readonly Subject<BoosterType> _onUsed = new Subject<BoosterType>();

    public BoosterService(GameSession session, ISaveService save, BoosterConfigSO config, ConveyorModel conveyor,
        ConveyorController conveyorController, ShooterColumns columns, CacheTray tray, PixelBoard board,
        ShooterPickService pickService, EndRushSystem endRush, int levelNumber, int seed = 0)
    {
        _session = session;
        _save = save;
        _config = config;
        _conveyor = conveyor;
        _conveyorController = conveyorController;
        _columns = columns;
        _tray = tray;
        _board = board;
        _pickService = pickService;
        _endRush = endRush;
        _levelNumber = levelNumber;
        _seed = seed;

        foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
        {
            int count = save.GetInt(SaveKeyPrefix + type, config.Get(type).startCount);
            _counts[type] = new ReactiveProperty<int>(count);
        }
    }

    /// <summary>Booster đang chờ chọn mục tiêu (null = không có).</summary>
    public ReadOnlyReactiveProperty<BoosterType?> ActiveMode => _activeMode;
    public Observable<BoosterType> OnUsed => _onUsed;

    public ReadOnlyReactiveProperty<int> Count(BoosterType type) => _counts[type];
    public bool IsUnlocked(BoosterType type) => _levelNumber >= _config.Get(type).unlockLevel;

    public bool CanUse(BoosterType type)
    {
        if (!_session.IsPlaying || _endRush.IsActive.CurrentValue) return false;
        if (!IsUnlocked(type) || _counts[type].Value <= 0) return false;
        if (_activeMode.Value.HasValue) return false;
        return type != BoosterType.Shuffle || _columns.RemainingInColumns > 1;
    }

    /// <summary>Dùng booster. Pickup / ColorBomb chỉ vào chế độ chọn, trừ số lượng khi đã chọn xong.</summary>
    public bool Use(BoosterType type)
    {
        if (!CanUse(type)) return false;

        switch (type)
        {
            case BoosterType.AddSlot:
                _conveyor.AddCapacity(1);
                Consume(type);
                return true;
            case BoosterType.Shuffle:
                _columns.Shuffle(new System.Random(_seed + _counts[type].Value * 7919 + _levelNumber));
                Consume(type);
                return true;
            default:
                EnterMode(type);
                return true;
        }
    }

    public void CancelMode()
    {
        _activeMode.Value = null;
        _pickService.Interceptor = null;
    }

    private void EnterMode(BoosterType type)
    {
        _activeMode.Value = type;
        _pickService.Interceptor = OnTargetSelected;
    }

    private bool OnTargetSelected(ShooterModel shooter)
    {
        var mode = _activeMode.Value;
        if (!mode.HasValue) return false;

        bool done = mode.Value == BoosterType.Pickup ? ApplyPickup(shooter) : ApplyColorBomb(shooter.ColorId);
        if (done)
        {
            Consume(mode.Value);
            CancelMode();
        }
        return true;
    }

    private bool ApplyPickup(ShooterModel shooter)
    {
        // Chỉ áp dụng cho shooter đang nằm trong cột (đầu cột thì chọn thường cũng được, vẫn cho phép).
        if (!_columns.TryFind(shooter, out _, out _)) return false;
        return _conveyorController.SendToBelt(shooter);
    }

    /// <summary>Phá mọi ô Alive của màu và bỏ mọi shooter của màu đó (cột, khay, băng).</summary>
    public bool ApplyColorBomb(int colorId)
    {
        if (!_board.HasAlive(colorId)) return false;

        for (int y = 0; y < _board.Height; y++)
        for (int x = 0; x < _board.Width; x++)
        {
            var cell = new Vector2Int(x, y);
            if (_board.StateAt(cell) == CellState.Alive && _board.ColorAt(cell) == colorId) _board.BeginBreak(cell);
        }

        foreach (var shooter in _columns.AllShooters)
        {
            if (shooter.ColorId != colorId || shooter.State.CurrentValue == ShooterState.Dead) continue;
            if (!_columns.Remove(shooter) && !_tray.Remove(shooter)) _conveyor.Remove(shooter);
            shooter.SetState(ShooterState.Dead);
        }
        return true;
    }

    private void Consume(BoosterType type)
    {
        var count = _counts[type];
        count.Value = Mathf.Max(0, count.Value - 1);
        _save.SetInt(SaveKeyPrefix + type, count.Value);
        _save.Save();
        _onUsed.OnNext(type);
    }

    public void Dispose()
    {
        CancelMode();
        foreach (var count in _counts.Values) count.Dispose();
        _activeMode.Dispose();
        _onUsed.Dispose();
    }
}
