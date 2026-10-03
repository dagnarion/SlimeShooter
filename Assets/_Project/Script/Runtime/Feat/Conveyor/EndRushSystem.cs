using System;
using System.Collections.Generic;
using R3;

/// <summary>
/// End rush ("Sure Win"): khi mọi shooter còn lại đều lên được băng chuyền thì không thể thua nữa.
/// Lúc đó tự đưa hết shooter còn chờ lên băng, băng chạy vòng liên tục và khoá input.
/// Cách bắn đặc biệt trong rush (bắn mọi ô cùng màu) do ShootingSystem (P6) xử lý theo <see cref="IsActive"/>.
/// </summary>
public class EndRushSystem : IDisposable
{
    private readonly GameSession _session;
    private readonly ShooterPickService _pickService;
    private readonly ConveyorModel _conveyor;
    private readonly ConveyorController _conveyorController;
    private readonly PixelBoard _board;
    private readonly ShooterColumns _columns;
    private readonly CacheTray _tray;

    private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
    private bool _hasPicked;

    public EndRushSystem(GameSession session, ShooterPickService pickService, ConveyorModel conveyor,
        ConveyorController conveyorController, PixelBoard board, ShooterColumns columns, CacheTray tray)
    {
        _session = session;
        _pickService = pickService;
        _conveyor = conveyor;
        _conveyorController = conveyorController;
        _board = board;
        _columns = columns;
        _tray = tray;

        _pickService.OnPicked.Subscribe(_ => { _hasPicked = true; Check(); }).AddTo(_subscriptions);
        _columns.OnColumnChanged.Subscribe(_ => Check()).AddTo(_subscriptions);
        _tray.OnChanged.Subscribe(_ => Check()).AddTo(_subscriptions);
        _conveyor.OnAttached.Subscribe(_ => Check()).AddTo(_subscriptions);
        _conveyor.OnLapCompleted.Subscribe(_ => Check()).AddTo(_subscriptions);
        _conveyor.OnRemoved.Subscribe(_ => Check()).AddTo(_subscriptions);
        _conveyor.Capacity.Subscribe(_ => Check()).AddTo(_subscriptions);
    }

    public ReadOnlyReactiveProperty<bool> IsActive => _isActive;

    /// <summary>Số shooter còn có thể bắn: đang chờ (cột + khay) + đang trên băng.</summary>
    public int AliveShooterCount => _columns.RemainingInColumns + _tray.Count + _conveyor.Units.Count;

    public void Check()
    {
        if (_isActive.Value || !_hasPicked || !_session.IsPlaying) return;
        if (_board.Remaining.CurrentValue <= 0) return;
        if (AliveShooterCount > _conveyor.Capacity.CurrentValue) return;

        Start();
    }

    private void Start()
    {
        _isActive.Value = true;
        _conveyor.IsLooping = true;
        _pickService.IsLocked = true;

        // Chụp danh sách trước vì SendToBelt làm cột thay đổi.
        var waiting = new List<ShooterModel>(_tray.Shooters);
        for (int c = 0; c < _columns.ColumnCount; c++) waiting.AddRange(_columns.GetColumn(c));
        foreach (var shooter in waiting) _conveyorController.SendToBelt(shooter);
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _isActive.Dispose();
    }
}
