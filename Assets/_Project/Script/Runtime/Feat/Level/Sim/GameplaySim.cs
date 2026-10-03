using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dựng toàn bộ model gameplay giống GameplayInstaller nhưng không cần scene/view.
/// Dùng cho test tích hợp, autoplay, và tool sinh level (chấm thử level ngay trong editor).
/// </summary>
public sealed class GameplaySim : IDisposable
{
    public readonly GameSession Session = new GameSession();
    public readonly TickScheduler Scheduler = new TickScheduler();
    public readonly PixelBoard Board;
    public readonly BoardLayout Layout;
    public readonly RectConveyorPath Path;
    public readonly ConveyorModel Conveyor;
    public readonly ShooterColumns Columns;
    public readonly CacheTray Tray;
    public readonly ShooterPickService Pick;
    public readonly ConveyorController Controller;
    public readonly EndRushSystem EndRush;
    public readonly RuleSystem Rules;
    public readonly ShootingSystem Shooting;
    public readonly ShootingConfigSO ShootingConfig;

    public float Time { get; private set; }

    public GameplaySim(int width, int height, IReadOnlyList<int> cells, IReadOnlyList<ColumnSpec> columns,
        int conveyorSlots = 5, int cacheSlots = 5,
        float breakTime = 0.35f, float speed = 5.5f, float spacing = 1.3f, float jump = 0.35f,
        float margin = 1.2f, float cornerRadius = 0.8f)
    {
        Board = new PixelBoard(width, height, cells, breakTime);
        Layout = new BoardLayout(Vector3.zero, width, height, 1f);
        var bounds = Layout.WorldBounds;
        Path = new RectConveyorPath(bounds.center, bounds.extents.x + margin, bounds.extents.z + margin, cornerRadius);
        Conveyor = new ConveyorModel(Path, speed, spacing, jump, conveyorSlots);
        Columns = new ShooterColumns(columns);
        Tray = new CacheTray(cacheSlots);
        Pick = new ShooterPickService(Session);
        Controller = new ConveyorController(Pick, Conveyor);
        EndRush = new EndRushSystem(Session, Pick, Conveyor, Controller, Board, Columns, Tray);
        Rules = new RuleSystem(Session, Board, Conveyor, Tray);
        ShootingConfig = ScriptableObject.CreateInstance<ShootingConfigSO>();
        Shooting = new ShootingSystem(Conveyor, Board, Layout, EndRush, ShootingConfig);

        Scheduler.Register(Board);
        Scheduler.Register(Conveyor);
        Scheduler.Register(Shooting);
        Session.StartPlaying();
    }

    public static GameplaySim FromLevel(LevelSO level) =>
        new GameplaySim(level.Width, level.Height, level.Cells, level.Columns, level.ConveyorSlots, level.CacheSlots);

    public void Step(float dt)
    {
        if (!Session.IsPlaying) return;
        Scheduler.Tick(dt);
        Time += dt;
    }

    public void Run(float seconds, float dt)
    {
        int steps = Mathf.CeilToInt(seconds / dt);
        for (int i = 0; i < steps && Session.IsPlaying; i++) Step(dt);
    }

    /// <summary>
    /// Bot đơn giản: ưu tiên shooter (khay trước, rồi đầu cột) có màu đang lộ ra trên board.
    /// Nếu không có, chỉ gửi bừa khi băng trống và khay còn chỗ để tránh tự lấp khay.
    /// </summary>
    public bool BotPick()
    {
        if (Pick.IsLocked || !Conveyor.CanInsert || !Conveyor.IsEntranceClear()) return false;

        var candidates = new List<ShooterModel>(Tray.Shooters);
        for (int c = 0; c < Columns.ColumnCount; c++)
        {
            var front = Columns.GetFront(c);
            if (front != null) candidates.Add(front);
        }
        if (candidates.Count == 0) return false;

        foreach (var shooter in candidates)
        {
            if (!IsColorOnBelt(shooter.ColorId) && Board.HasExposed(shooter.ColorId)) return Pick.TryPick(shooter);
        }
        foreach (var shooter in candidates)
        {
            if (Board.HasExposed(shooter.ColorId)) return Pick.TryPick(shooter);
        }
        if (Conveyor.Units.Count == 0 && Tray.FreeCount.CurrentValue > 1) return Pick.TryPick(candidates[candidates.Count - 1]);
        return false;
    }

    private bool IsColorOnBelt(int colorId)
    {
        foreach (var unit in Conveyor.Units) if (unit.Shooter.ColorId == colorId) return true;
        return false;
    }

    /// <summary>Chơi tự động tới khi thắng/thua hoặc hết giờ.</summary>
    public GameState Autoplay(float maxSeconds = 600f, float dt = 1f / 60f, float decisionInterval = 0.15f)
    {
        float nextDecision = 0f;
        while (Session.IsPlaying && Time < maxSeconds)
        {
            if (Time >= nextDecision)
            {
                BotPick();
                nextDecision = Time + decisionInterval;
            }
            Step(dt);
        }
        return Session.CurrentState;
    }

    public void Dispose()
    {
        Shooting.Dispose();
        Rules.Dispose();
        EndRush.Dispose();
        Controller.Dispose();
        Pick.Dispose();
        Tray.Dispose();
        Columns.Dispose();
        Conveyor.Dispose();
        Board.Dispose();
        Session.Dispose();
        UnityEngine.Object.DestroyImmediate(ShootingConfig);
    }
}
