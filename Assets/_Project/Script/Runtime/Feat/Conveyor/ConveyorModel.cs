using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

public enum BeltUnitState
{
    /// <summary>Đang nhảy lên cửa băng chuyền / chờ cửa thông.</summary>
    Inserting,
    Moving
}

public class BeltUnit
{
    public ShooterModel Shooter;
    public BeltUnitState State;
    /// <summary>Quãng đường đã đi trong vòng hiện tại (0..Length).</summary>
    public float Distance;
    /// <summary>Distance của tick trước (để hệ thống bắn biết đoạn vừa đi qua).</summary>
    public float PreviousDistance;
    public int Laps;
    /// <summary>Đã bắt đầu nhảy lên cửa (chỉ shooter đứng đầu hàng chờ mới nhảy).</summary>
    public bool JumpStarted;
    internal float InsertTimer;
}

/// <summary>
/// Băng chuyền N slot. Mỗi shooter chiếm 1 slot từ lúc được nhận tới khi rời băng.
/// Không có hàng chờ xếp chồng: hết slot thì từ chối.
/// </summary>
public class ConveyorModel : ITickable, IDisposable
{
    private readonly IConveyorPath _path;
    private readonly float _speed;
    private readonly float _spacing;
    private readonly float _insertDelay;

    private readonly List<BeltUnit> _units = new List<BeltUnit>();
    private readonly ReactiveProperty<int> _capacity;
    private readonly ReactiveProperty<int> _available;

    private readonly Subject<BeltUnit> _onInserted = new Subject<BeltUnit>();
    private readonly Subject<BeltUnit> _onJumpStarted = new Subject<BeltUnit>();
    private readonly Subject<BeltUnit> _onAttached = new Subject<BeltUnit>();
    private readonly Subject<ShooterModel> _onInsertRejected = new Subject<ShooterModel>();
    private readonly Subject<ShooterModel> _onLapCompleted = new Subject<ShooterModel>();
    private readonly Subject<ShooterModel> _onRemoved = new Subject<ShooterModel>();

    public ConveyorModel(IConveyorPath path, ConveyorConfigSO config, int capacity)
        : this(path, config.MoveSpeed, config.Spacing, config.JumpDuration, capacity) { }

    public ConveyorModel(IConveyorPath path, float speed, float spacing, float insertDelay, int capacity)
    {
        _path = path;
        _speed = speed;
        _spacing = spacing;
        _insertDelay = Mathf.Max(0f, insertDelay);
        _capacity = new ReactiveProperty<int>(Mathf.Max(1, capacity));
        _available = new ReactiveProperty<int>(_capacity.Value);
    }

    public IConveyorPath Path => _path;
    public IReadOnlyList<BeltUnit> Units => _units;
    public ReadOnlyReactiveProperty<int> Capacity => _capacity;
    /// <summary>Số slot còn trống (hiển thị "x/N" ở cửa băng chuyền).</summary>
    public ReadOnlyReactiveProperty<int> Available => _available;

    /// <summary>End rush: shooter chạy vòng liên tục, không rời băng.</summary>
    public bool IsLooping { get; set; }

    /// <summary>Shooter được nhận (đã chiếm slot) — có thể còn phải chờ tới lượt nhảy.</summary>
    public Observable<BeltUnit> OnInserted => _onInserted;
    /// <summary>Shooter bắt đầu nhảy lên cửa vào (view chạy animation nhảy ở đây).</summary>
    public Observable<BeltUnit> OnJumpStarted => _onJumpStarted;
    public Observable<BeltUnit> OnAttached => _onAttached;
    public Observable<ShooterModel> OnInsertRejected => _onInsertRejected;
    /// <summary>Shooter chạy hết vòng (không looping) và đã rời băng.</summary>
    public Observable<ShooterModel> OnLapCompleted => _onLapCompleted;
    /// <summary>Shooter bị gỡ khỏi băng giữa chừng (hết đạn...).</summary>
    public Observable<ShooterModel> OnRemoved => _onRemoved;

    public bool CanInsert => _units.Count < _capacity.Value;

    public void AddCapacity(int amount)
    {
        if (amount <= 0) return;
        _capacity.Value += amount;
        RefreshAvailable();
    }

    public bool Contains(ShooterModel shooter) => IndexOf(shooter) >= 0;

    public bool TryInsert(ShooterModel shooter)
    {
        if (shooter == null || Contains(shooter)) return false;
        if (!CanInsert)
        {
            _onInsertRejected.OnNext(shooter);
            return false;
        }

        var unit = new BeltUnit { Shooter = shooter, State = BeltUnitState.Inserting, InsertTimer = _insertDelay };
        _units.Add(unit);
        shooter.SetState(ShooterState.MovingToBelt);
        RefreshAvailable();
        _onInserted.OnNext(unit);
        StartHeadJump();
        return true;
    }

    public bool Remove(ShooterModel shooter)
    {
        int index = IndexOf(shooter);
        if (index < 0) return false;
        _units.RemoveAt(index);
        RefreshAvailable();
        _onRemoved.OnNext(shooter);
        return true;
    }

    public void Tick(float deltaTime)
    {
        AdvanceMovingUnits(deltaTime);
        AttachPendingUnits(deltaTime);
    }

    private void AdvanceMovingUnits(float deltaTime)
    {
        float length = _path.Length;
        for (int i = 0; i < _units.Count; i++)
        {
            var unit = _units[i];
            if (unit.State != BeltUnitState.Moving) continue;

            unit.PreviousDistance = unit.Distance;
            unit.Distance += _speed * deltaTime;
            if (unit.Distance < length) continue;

            if (IsLooping)
            {
                unit.Distance -= length;
                unit.Laps++;
                continue;
            }

            unit.Distance = length;
            _units.RemoveAt(i);
            i--;
            RefreshAvailable();
            _onLapCompleted.OnNext(unit.Shooter);
        }
    }

    private void AttachPendingUnits(float deltaTime)
    {
        // FIFO: chỉ shooter đứng đầu hàng chờ nhảy lên, vào băng khi nhảy xong và cửa thông.
        var head = PendingHead();
        if (head == null) return;

        head.InsertTimer -= deltaTime;
        if (head.InsertTimer > 0f || !IsEntranceClear()) return;

        head.State = BeltUnitState.Moving;
        head.Distance = 0f;
        head.PreviousDistance = 0f;
        head.Shooter.SetState(ShooterState.OnBelt);
        _onAttached.OnNext(head);
        StartHeadJump();
    }

    private BeltUnit PendingHead()
    {
        foreach (var unit in _units)
        {
            if (unit.State == BeltUnitState.Inserting) return unit;
        }
        return null;
    }

    private void StartHeadJump()
    {
        var head = PendingHead();
        if (head == null || head.JumpStarted) return;
        head.JumpStarted = true;
        head.InsertTimer = _insertDelay;
        _onJumpStarted.OnNext(head);
    }

    /// <summary>Không có shooter nào trong khoảng <c>spacing</c> trước hoặc sau cửa vào.</summary>
    public bool IsEntranceClear()
    {
        float length = _path.Length;
        foreach (var unit in _units)
        {
            if (unit.State != BeltUnitState.Moving) continue;
            if (unit.Distance < _spacing || unit.Distance > length - _spacing) return false;
        }
        return true;
    }

    private int IndexOf(ShooterModel shooter)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].Shooter == shooter) return i;
        }
        return -1;
    }

    private void RefreshAvailable() => _available.Value = Mathf.Max(0, _capacity.Value - _units.Count);

    public void Dispose()
    {
        _capacity.Dispose();
        _available.Dispose();
        _onInserted.Dispose();
        _onJumpStarted.Dispose();
        _onAttached.Dispose();
        _onInsertRejected.Dispose();
        _onLapCompleted.Dispose();
        _onRemoved.Dispose();
    }
}
