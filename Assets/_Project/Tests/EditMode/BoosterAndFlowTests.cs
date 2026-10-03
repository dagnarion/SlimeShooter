using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static LevelTestUtils;

public class BoosterTests
{
    private GameplaySim _sim;
    private BoosterConfigSO _config;
    private FakeSave _save;
    private BoosterService _boosters;

    // 4x2: hàng dưới đỏ, hàng trên xanh -> 4 đỏ, 4 xanh
    private static readonly int[] Cells = { Red, Red, Red, Red, Blue, Blue, Blue, Blue };

    [SetUp]
    public void SetUp()
    {
        // 4 shooter > 1 slot -> không vào rush; Σammo mỗi màu = 4 = số ô mỗi màu.
        _sim = new GameplaySim(4, 2, Cells, new List<ColumnSpec>
        {
            Column((Red, 2), (Blue, 2)),
            Column((Blue, 2), (Red, 2)),
        }, conveyorSlots: 1, cacheSlots: 3);
        _config = ScriptableObject.CreateInstance<BoosterConfigSO>();
        _save = new FakeSave();
        _boosters = new BoosterService(_sim.Session, _save, _config, _sim.Conveyor, _sim.Controller, _sim.Columns,
            _sim.Tray, _sim.Board, _sim.Pick, _sim.EndRush, levelNumber: 1, seed: 3);
    }

    [TearDown]
    public void TearDown()
    {
        _boosters.Dispose();
        _sim.Dispose();
        Object.DestroyImmediate(_config);
    }

    [Test]
    public void AddSlot_IncreasesCapacity_AndSavesCount()
    {
        int before = _boosters.Count(BoosterType.AddSlot).CurrentValue;
        Assert.IsTrue(_boosters.Use(BoosterType.AddSlot));
        Assert.AreEqual(2, _sim.Conveyor.Capacity.CurrentValue);
        Assert.AreEqual(before - 1, _boosters.Count(BoosterType.AddSlot).CurrentValue);
        Assert.AreEqual(before - 1, _save.GetInt(BoosterService.SaveKeyPrefix + BoosterType.AddSlot));
    }

    [Test]
    public void Pickup_TakesShooterBehindFront()
    {
        var behind = _sim.Columns.GetColumn(0)[1];
        Assert.IsFalse(_sim.Columns.CanPick(behind));

        Assert.IsTrue(_boosters.Use(BoosterType.Pickup));
        Assert.AreEqual(BoosterType.Pickup, _boosters.ActiveMode.CurrentValue);

        Assert.IsTrue(_sim.Pick.TryPick(behind));
        Assert.IsTrue(_sim.Conveyor.Contains(behind));
        Assert.IsNull(_boosters.ActiveMode.CurrentValue, "Thoát chế độ chọn sau khi dùng");
        Assert.IsNull(_sim.Pick.Interceptor);
    }

    [Test]
    public void ColorBomb_RemovesColorEverywhere_AndKeepsBalance()
    {
        // Đưa 1 shooter đỏ lên băng trước để kiểm tra bỏ cả trên băng.
        var redFront = _sim.Columns.GetFront(0);
        _sim.Pick.TryPick(redFront);

        Assert.IsTrue(_boosters.Use(BoosterType.ColorBomb));
        _sim.Pick.TryPick(_sim.Columns.GetFront(1)); // chạm shooter xanh -> bom màu xanh

        Assert.IsFalse(_sim.Board.HasAlive(Blue));
        foreach (var shooter in _sim.Columns.AllShooters.Where(s => s.ColorId == Blue))
            Assert.AreEqual(ShooterState.Dead, shooter.State.CurrentValue);

        int redAmmo = _sim.Columns.AllShooters.Where(s => s.ColorId == Red && s.State.CurrentValue != ShooterState.Dead).Sum(s => s.Ammo.CurrentValue);
        int redCells = 0;
        for (int x = 0; x < 4; x++) if (_sim.Board.StateAt(new Vector2Int(x, 0)) == CellState.Alive) redCells++;
        Assert.AreEqual(redCells, redAmmo, "Σammo(đỏ) = Σô Alive(đỏ)");
    }

    [Test]
    public void Shuffle_KeepsColumnSizes_AndShooters()
    {
        var before = Enumerable.Range(0, _sim.Columns.ColumnCount).Select(c => _sim.Columns.GetColumn(c).Count).ToArray();
        var set = new HashSet<ShooterModel>(Enumerable.Range(0, _sim.Columns.ColumnCount).SelectMany(c => _sim.Columns.GetColumn(c)));

        Assert.IsTrue(_boosters.Use(BoosterType.Shuffle));

        var after = Enumerable.Range(0, _sim.Columns.ColumnCount).Select(c => _sim.Columns.GetColumn(c).Count).ToArray();
        CollectionAssert.AreEqual(before, after);
        CollectionAssert.AreEquivalent(set, Enumerable.Range(0, _sim.Columns.ColumnCount).SelectMany(c => _sim.Columns.GetColumn(c)));
    }

    [Test]
    public void CannotUse_WhenEmpty_OrDuringMode()
    {
        _save.SetInt(BoosterService.SaveKeyPrefix + BoosterType.AddSlot, 0);
        using (var fresh = new BoosterService(_sim.Session, _save, _config, _sim.Conveyor, _sim.Controller, _sim.Columns,
                   _sim.Tray, _sim.Board, _sim.Pick, _sim.EndRush, 1))
        {
            Assert.IsFalse(fresh.CanUse(BoosterType.AddSlot));
        }

        _boosters.Use(BoosterType.Pickup);
        Assert.IsFalse(_boosters.CanUse(BoosterType.Shuffle), "Đang chọn mục tiêu thì không dùng booster khác");
        _boosters.CancelMode();
        Assert.IsTrue(_boosters.CanUse(BoosterType.Shuffle));
    }
}

public class ReviveAndProgressTests
{
    [Test]
    public void Revive_AfterTrayFull_AddsSlot_AndContinues()
    {
        using (var sim = new GameplaySim(3, 3, Enumerable.Repeat(Red, 9).ToArray(),
                   new List<ColumnSpec> { Column((Blue, 1), (Blue, 1), (Red, 9)) }, conveyorSlots: 1, cacheSlots: 1))
        {
            var revive = new ReviveService(sim.Session, sim.Rules, sim.Tray, 1);
            sim.Pick.TryPick(sim.Columns.GetFront(0));
            sim.Run(sim.Path.Length / 5.5f + 1f, 1f / 60f);
            sim.Pick.TryPick(sim.Columns.GetFront(0));
            sim.Run(sim.Path.Length / 5.5f + 1f, 1f / 60f);
            Assert.AreEqual(GameState.Lost, sim.Session.CurrentState);

            var blocked = sim.Rules.BlockedShooter;
            Assert.IsTrue(revive.Revive());
            Assert.AreEqual(GameState.Playing, sim.Session.CurrentState);
            Assert.AreEqual(2, sim.Tray.Capacity.CurrentValue);
            Assert.AreSame(sim.Tray, blocked.Container);
            Assert.IsFalse(revive.CanRevive);
        }
    }

    [Test]
    public void Win_AdvancesLevel_Once()
    {
        var save = new FakeSave();
        var db = ScriptableObject.CreateInstance<LevelDatabaseSO>();
        db.SetLevels(Enumerable.Range(0, 3).Select(_ => ScriptableObject.CreateInstance<LevelSO>()));
        var session = new GameSession();
        using (var levels = new LevelService(db, save))
        using (var progress = new LevelProgressSystem(session, levels))
        {
            Assert.AreEqual(1, progress.PlayedLevelNumber);
            session.StartPlaying();
            session.Win();
            Assert.AreEqual(1, levels.CurrentIndex.CurrentValue);
            Assert.AreEqual(1, save.GetInt(LevelService.CurrentLevelKey));
            Assert.AreEqual(1, progress.PlayedLevelNumber, "Số level hiển thị giữ nguyên");
        }
        session.Dispose();
    }
}
