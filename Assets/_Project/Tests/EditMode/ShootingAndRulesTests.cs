using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using R3;
using UnityEngine;
using static LevelTestUtils;

public class ShootingSystemTests
{
    private static int[] Fill(int w, int h, int color) => Enumerable.Repeat(color, w * h).ToArray();

    /// <summary>Gửi 1 shooter đỏ chạy đúng 1 vòng quanh board 5x5 toàn đỏ, trả về số ô bị bắn.</summary>
    private static (int shots, int ammoLeft) OneLap(float dt)
    {
        // 2 shooter > 1 slot để không vào end rush (rush đổi luật bắn).
        using (var sim = new GameplaySim(5, 5, Fill(5, 5, Red), new List<ColumnSpec> { Column((Red, 25)), Column((Red, 1)) }, conveyorSlots: 1))
        {
            var shooter = sim.Columns.GetFront(0);
            int shots = 0;
            using (sim.Shooting.OnShot.Subscribe(_ => shots++))
            {
                sim.Pick.TryPick(shooter);
                float lap = sim.Path.Length / 5.5f + 0.35f;
                sim.Run(lap + 0.2f, dt);
            }
            return (shots, shooter.Ammo.CurrentValue);
        }
    }

    [Test]
    public void OneLap_ResultIsIndependentOfFrameRate()
    {
        var a = OneLap(1f / 30f);
        var b = OneLap(1f / 60f);
        var c = OneLap(1f / 120f);

        Assert.Greater(a.shots, 0);
        Assert.AreEqual(a, b, "30 vs 60 FPS");
        Assert.AreEqual(b, c, "60 vs 120 FPS");
        Assert.AreEqual(25 - a.shots, a.ammoLeft, "Mỗi phát bắn trừ đúng 1 ammo");
    }

    [Test]
    public void EachLineFiresAtMostOncePerPass()
    {
        var result = OneLap(1f / 60f);
        // 4 cạnh x 5 đường = tối đa 20 phát trong 1 vòng.
        Assert.LessOrEqual(result.shots, 20);
    }

    [Test]
    public void WrongColorShooter_DoesNotShoot()
    {
        using (var sim = new GameplaySim(3, 3, Fill(3, 3, Red), new List<ColumnSpec> { Column((Blue, 9)), Column((Red, 9)) }, conveyorSlots: 1))
        {
            var blue = sim.Columns.GetFront(0);
            sim.Pick.TryPick(blue);
            sim.Run(sim.Path.Length / 5.5f + 0.5f, 1f / 60f);
            Assert.AreEqual(9, blue.Ammo.CurrentValue);
            Assert.AreEqual(9, sim.Board.Remaining.CurrentValue);
        }
    }

    [Test]
    public void DepletedShooter_LeavesBelt()
    {
        using (var sim = new GameplaySim(5, 5, Fill(5, 5, Red), new List<ColumnSpec> { Column((Red, 3), (Red, 22)) }, conveyorSlots: 1))
        {
            var small = sim.Columns.GetFront(0);
            var depleted = new List<ShooterModel>();
            sim.Shooting.OnShooterDepleted.Subscribe(depleted.Add);

            sim.Pick.TryPick(small);
            sim.Run(5f, 1f / 60f);

            CollectionAssert.Contains(depleted, small);
            Assert.AreEqual(ShooterState.Dead, small.State.CurrentValue);
            Assert.IsFalse(sim.Conveyor.Contains(small));
        }
    }

    [Test]
    public void Rush_ShootsHiddenCells_AndClearsBoard()
    {
        // 3x3: ô giữa màu xanh bị bao quanh bởi đỏ -> bình thường không bắn được tới.
        var cells = Fill(3, 3, Red);
        cells[4] = Blue;
        using (var sim = new GameplaySim(3, 3, cells, new List<ColumnSpec> { Column((Blue, 1)), Column((Red, 8)) }))
        {
            // Chỉ có 2 shooter <= 5 slot -> chọn 1 con là vào rush ngay.
            sim.Pick.TryPick(sim.Columns.GetFront(0));
            Assert.IsTrue(sim.EndRush.IsActive.CurrentValue);

            sim.Run(5f, 1f / 60f);

            Assert.AreEqual(GameState.Won, sim.Session.CurrentState);
            Assert.AreEqual(0, sim.Board.Remaining.CurrentValue);
        }
    }
}

public class CacheTrayTests
{
    [Test]
    public void AddRemove_CompactsAndTracksFreeCount()
    {
        using (var tray = new CacheTray(2))
        {
            var a = new ShooterModel(0, Red, 5);
            var b = new ShooterModel(1, Red, 5);
            var c = new ShooterModel(2, Red, 5);

            Assert.IsTrue(tray.TryAdd(a));
            Assert.IsTrue(tray.TryAdd(b));
            Assert.IsTrue(tray.IsFull);
            Assert.IsFalse(tray.TryAdd(c));
            Assert.AreEqual(ShooterState.InTray, a.State.CurrentValue);
            Assert.AreSame(tray, a.Container);

            Assert.IsTrue(tray.Remove(a));
            Assert.AreSame(b, tray.Shooters[0], "Dồn về bên trái");
            Assert.AreEqual(1, tray.FreeCount.CurrentValue);

            tray.AddCapacity(1);
            Assert.AreEqual(2, tray.FreeCount.CurrentValue);
            Assert.IsTrue(tray.CanPick(b));
        }
    }
}

public class RuleSystemTests
{
    [Test]
    public void ShooterWithAmmoLeft_GoesToTray()
    {
        using (var sim = new GameplaySim(3, 3, Enumerable.Repeat(Red, 9).ToArray(),
                   new List<ColumnSpec> { Column((Blue, 5)), Column((Red, 9)) }, conveyorSlots: 1))
        {
            // Lưới toàn đỏ -> shooter xanh chạy 1 vòng không bắn được -> về khay.
            // 2 shooter > 1 slot nên không vào rush.
            var blue = sim.Columns.GetFront(0);
            sim.Pick.TryPick(blue);
            sim.Run(sim.Path.Length / 5.5f + 1f, 1f / 60f);

            Assert.AreSame(sim.Tray, blue.Container);
            Assert.AreEqual(GameState.Playing, sim.Session.CurrentState);
        }
    }

    [Test]
    public void TrayFull_ShooterReturns_Lose()
    {
        using (var sim = new GameplaySim(3, 3, Enumerable.Repeat(Red, 9).ToArray(),
                   new List<ColumnSpec> { Column((Blue, 1), (Blue, 1), (Red, 9)) }, conveyorSlots: 1, cacheSlots: 1))
        {
            sim.Pick.TryPick(sim.Columns.GetFront(0));
            sim.Run(sim.Path.Length / 5.5f + 1f, 1f / 60f); // xanh #1 về khay (khay đầy)
            Assert.AreEqual(GameState.Playing, sim.Session.CurrentState);

            sim.Pick.TryPick(sim.Columns.GetFront(0));
            sim.Run(sim.Path.Length / 5.5f + 1f, 1f / 60f); // xanh #2 về -> không còn chỗ

            Assert.AreEqual(GameState.Lost, sim.Session.CurrentState);
            Assert.AreEqual(LoseReason.TrayFull, sim.Rules.LoseReason);
            Assert.IsNotNull(sim.Rules.BlockedShooter);
        }
    }

    [Test]
    public void ClearingBoard_Wins_AfterLastBreakFinishes()
    {
        using (var sim = new GameplaySim(1, 1, new[] { Red }, new List<ColumnSpec> { Column((Red, 1)) }))
        {
            sim.Pick.TryPick(sim.Columns.GetFront(0));
            int guard = 0;
            while (sim.Board.AliveCount > 0 && guard++ < 2000) sim.Step(1f / 60f);
            Assert.AreEqual(GameState.Playing, sim.Session.CurrentState, "Ô còn Breaking -> chưa thắng");

            sim.Run(1f, 1f / 60f);
            Assert.AreEqual(GameState.Won, sim.Session.CurrentState);
        }
    }
}

public class EndRushWithTrayTests
{
    [Test]
    public void TrayShooters_AreCounted_AndAutoPickedOnRush()
    {
        using (var sim = new GameplaySim(3, 3, Enumerable.Repeat(Red, 9).ToArray(),
                   new List<ColumnSpec> { Column((Blue, 1), (Red, 9)), Column((Blue, 1)) }, conveyorSlots: 1, cacheSlots: 3))
        {
            // Xanh #1 về khay: còn khay 1 + cột 2 = 3 > 1 slot -> chưa rush.
            sim.Pick.TryPick(sim.Columns.GetFront(0));
            sim.Run(sim.Path.Length / 5.5f + 1f, 1f / 60f);
            Assert.AreEqual(1, sim.Tray.Count);
            Assert.IsFalse(sim.EndRush.IsActive.CurrentValue);
            Assert.AreEqual(3, sim.EndRush.AliveShooterCount);
        }
    }
}
