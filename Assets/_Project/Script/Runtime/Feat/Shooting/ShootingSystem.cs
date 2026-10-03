using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

public readonly struct ShotEvent
{
    public readonly ShooterModel Shooter;
    public readonly Vector2Int Cell;
    public readonly bool IsRush;

    public ShotEvent(ShooterModel shooter, Vector2Int cell, bool isRush)
    {
        Shooter = shooter;
        Cell = cell;
        IsRush = isRush;
    }
}

/// <summary>
/// Bắn hoàn toàn trên model (không physics):
/// - Thường: mỗi đường bắn (cột/hàng) shooter đi qua được bắn tối đa 1 phát, vào ô chặn đầu tiên nếu Alive và cùng màu.
///   Lấy mẫu theo quãng đường đã đi nên kết quả không phụ thuộc FPS.
/// - End rush: bắn ô Alive cùng màu gần nhất ở bất kỳ đâu, theo nhịp thời gian rất nhanh.
/// Model bị trừ ngay khi bắn (ô -> Breaking, ammo--); đạn chỉ là hiệu ứng.
/// </summary>
public class ShootingSystem : ITickable, IDisposable
{
    private class UnitTrack
    {
        public bool HasLine;
        public BoardSide Side;
        public int Line;
        public float RushTimer;
    }

    private readonly ConveyorModel _conveyor;
    private readonly PixelBoard _board;
    private readonly BoardLayout _layout;
    private readonly EndRushSystem _endRush;
    private readonly ShootingConfigSO _config;

    private readonly Dictionary<ShooterModel, UnitTrack> _tracks = new Dictionary<ShooterModel, UnitTrack>();
    private readonly List<BeltUnit> _snapshot = new List<BeltUnit>();
    private readonly Subject<ShotEvent> _onShot = new Subject<ShotEvent>();
    private readonly Subject<ShooterModel> _onShooterDepleted = new Subject<ShooterModel>();
    private readonly IDisposable _lapSubscription;
    private float _rushTime;
    private bool _rushTimedOut;

    public ShootingSystem(ConveyorModel conveyor, PixelBoard board, BoardLayout layout, EndRushSystem endRush, ShootingConfigSO config)
    {
        _conveyor = conveyor;
        _board = board;
        _layout = layout;
        _endRush = endRush;
        _config = config;
        // Shooter rời băng (về khay) thì quên đường bắn cũ; lần sau lên băng là một lượt mới.
        _lapSubscription = _conveyor.OnLapCompleted.Subscribe(shooter => _tracks.Remove(shooter));
    }

    public Observable<ShotEvent> OnShot => _onShot;
    public Observable<ShooterModel> OnShooterDepleted => _onShooterDepleted;

    public void Tick(float deltaTime)
    {
        bool rush = _endRush != null && _endRush.IsActive.CurrentValue;
        if (rush) TickRushTimeout(deltaTime);

        _snapshot.Clear();
        _snapshot.AddRange(_conveyor.Units);
        foreach (var unit in _snapshot)
        {
            if (unit.State != BeltUnitState.Moving) continue;
            var track = GetTrack(unit.Shooter);
            if (rush) FireRush(unit, track, deltaTime);
            else FireAlongPath(unit, track);
        }
    }

    #region Normal
    private void FireAlongPath(BeltUnit unit, UnitTrack track)
    {
        var path = _conveyor.Path;
        float from = unit.PreviousDistance;
        float to = unit.Distance;
        if (to < from) to += path.Length; // vừa quấn vòng (looping)

        float step = Mathf.Max(0.01f, _layout.CellSize * _config.SampleStepRatio);
        for (float d = from; ; d += step)
        {
            if (d > to) d = to;
            Visit(unit.Shooter, track, d);
            if (d >= to || !unit.Shooter.HasAmmo) break;
        }
    }

    private void Visit(ShooterModel shooter, UnitTrack track, float distance)
    {
        var path = _conveyor.Path;
        Vector3 position = path.Evaluate(distance, out _);
        if (!path.TryGetSide(distance, out var side) || !_layout.TryWorldToLine(side, position, out int line))
        {
            track.HasLine = false;
            return;
        }

        if (track.HasLine && track.Side == side && track.Line == line) return;
        track.HasLine = true;
        track.Side = side;
        track.Line = line;

        if (shooter.HasAmmo && _board.TryGetTarget(side, line, shooter.ColorId, out var cell))
            Fire(shooter, cell, false);
    }
    #endregion

    #region Rush
    private void FireRush(BeltUnit unit, UnitTrack track, float deltaTime)
    {
        track.RushTimer += deltaTime;
        var shooter = unit.Shooter;
        while (track.RushTimer >= _config.RushFireInterval && shooter.HasAmmo)
        {
            track.RushTimer -= _config.RushFireInterval;
            Vector3 position = _conveyor.Path.Evaluate(unit.Distance, out _);
            var from = new Vector2(_layout.WorldToColumn(position.x), _layout.WorldToRow(position.z));
            if (!_board.TryFindNearestAlive(shooter.ColorId, from, out var cell)) break;
            Fire(shooter, cell, true);
        }
    }

    private void TickRushTimeout(float deltaTime)
    {
        if (_rushTimedOut) return;
        _rushTime += deltaTime;
        if (_rushTime < _config.RushTimeout || _board.AliveCount == 0) return;

        // Chốt an toàn: không để end rush kẹt mãi.
        _rushTimedOut = true;
        Debug.LogWarning($"[Shooting] End rush quá {_config.RushTimeout}s, cho vỡ {_board.AliveCount} ô còn lại.");
        for (int y = 0; y < _board.Height; y++)
        for (int x = 0; x < _board.Width; x++)
            _board.BeginBreak(new Vector2Int(x, y));
    }
    #endregion

    private void Fire(ShooterModel shooter, Vector2Int cell, bool isRush)
    {
        if (!_board.BeginBreak(cell)) return;
        shooter.ConsumeAmmo();
        _onShot.OnNext(new ShotEvent(shooter, cell, isRush));
        if (!shooter.HasAmmo) Deplete(shooter);
    }

    private void Deplete(ShooterModel shooter)
    {
        _tracks.Remove(shooter);
        shooter.SetState(ShooterState.Dead);
        _conveyor.Remove(shooter);
        _onShooterDepleted.OnNext(shooter);
    }

    private UnitTrack GetTrack(ShooterModel shooter)
    {
        if (!_tracks.TryGetValue(shooter, out var track))
        {
            track = new UnitTrack();
            _tracks[shooter] = track;
        }
        return track;
    }

    public void Dispose()
    {
        _lapSubscription.Dispose();
        _onShot.Dispose();
        _onShooterDepleted.Dispose();
    }
}
