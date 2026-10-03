using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;
using static LevelTestUtils;

public class RectConveyorPathTests
{
    [Test]
    public void Length_IsPerimeterOfRoundedRect()
    {
        var path = new RectConveyorPath(Vector3.zero, 5f, 4f, 1f);
        float expected = 2f * (8f + 6f) + 2f * Mathf.PI * 1f; // 4 cạnh thẳng + 4 cung 1/4
        Assert.AreEqual(expected, path.Length, 1e-4f);
    }

    [Test]
    public void StartsAtBottomLeft_MovingRight_InwardPointsToBoard()
    {
        var path = new RectConveyorPath(Vector3.zero, 5f, 4f, 1f);
        Vector3 start = path.Evaluate(0f, out Vector3 forward);

        Assert.AreEqual(-4f, start.x, 1e-4f);
        Assert.AreEqual(-4f, start.z, 1e-4f);
        Assert.AreEqual(Vector3.right, forward);
        Assert.AreEqual(Vector3.forward, RectConveyorPath.Inward(forward), "Phía trong của cạnh dưới là +Z");
    }

    [Test]
    public void Sides_FollowCounterClockwiseOrder_CornersHaveNoSide()
    {
        var path = new RectConveyorPath(Vector3.zero, 5f, 4f, 1f);
        float bottom = 8f, arc = Mathf.PI * 0.5f, right = 6f, top = 8f;

        AssertSide(path, bottom * 0.5f, BoardSide.Bottom);
        Assert.IsFalse(path.TryGetSide(bottom + arc * 0.5f, out _), "Góc cua không có cạnh");
        AssertSide(path, bottom + arc + right * 0.5f, BoardSide.Right);
        AssertSide(path, bottom + arc + right + arc + top * 0.5f, BoardSide.Top);
        AssertSide(path, path.Length - arc - 1f, BoardSide.Left);
    }

    [Test]
    public void Evaluate_IsContinuous_AndWraps()
    {
        var path = new RectConveyorPath(new Vector3(2f, 0f, 3f), 4f, 6f, 1.5f);
        Vector3 previous = path.Evaluate(0f, out _);
        for (int i = 1; i <= 400; i++)
        {
            Vector3 current = path.Evaluate(path.Length * i / 400f, out _);
            Assert.Less(Vector3.Distance(previous, current), path.Length / 400f + 1e-3f);
            previous = current;
        }
        Assert.Less(Vector3.Distance(path.Evaluate(0f, out _), path.Evaluate(path.Length, out _)), 1e-4f);
    }

    private static void AssertSide(IConveyorPath path, float distance, BoardSide expected)
    {
        Assert.IsTrue(path.TryGetSide(distance, out var side));
        Assert.AreEqual(expected, side);
    }
}

public class ConveyorModelTests
{
    private const float Speed = 5f;
    private const float Spacing = 1f;
    private const float InsertDelay = 0.2f;

    private RectConveyorPath _path;

    [SetUp]
    public void SetUp() => _path = new RectConveyorPath(Vector3.zero, 5f, 5f, 0f); // chu vi 40

    private ConveyorModel Create(int capacity) => new ConveyorModel(_path, Speed, Spacing, InsertDelay, capacity);
    private static ShooterModel Shooter(int id) => new ShooterModel(id, Red, 10);

    [Test]
    public void RejectsWhenFull_AndPublishesAvailable()
    {
        var conveyor = Create(2);
        var rejected = new List<ShooterModel>();
        conveyor.OnInsertRejected.Subscribe(rejected.Add);

        Assert.IsTrue(conveyor.TryInsert(Shooter(0)));
        Assert.IsTrue(conveyor.TryInsert(Shooter(1)));
        Assert.AreEqual(0, conveyor.Available.CurrentValue);

        var third = Shooter(2);
        Assert.IsFalse(conveyor.TryInsert(third));
        CollectionAssert.AreEqual(new[] { third }, rejected);

        conveyor.AddCapacity(1);
        Assert.AreEqual(1, conveyor.Available.CurrentValue);
        Assert.IsTrue(conveyor.TryInsert(third));
    }

    [Test]
    public void Insert_WaitsForJumpDelay_ThenAttachesAtStart()
    {
        var conveyor = Create(3);
        var shooter = Shooter(0);
        conveyor.TryInsert(shooter);
        Assert.AreEqual(ShooterState.MovingToBelt, shooter.State.CurrentValue);

        conveyor.Tick(InsertDelay * 0.5f);
        Assert.AreEqual(BeltUnitState.Inserting, conveyor.Units[0].State);

        conveyor.Tick(InsertDelay * 0.5f + 1e-4f);
        Assert.AreEqual(BeltUnitState.Moving, conveyor.Units[0].State);
        Assert.AreEqual(ShooterState.OnBelt, shooter.State.CurrentValue);
        Assert.AreEqual(0f, conveyor.Units[0].Distance, 1e-4f);
    }

    [Test]
    public void SecondShooter_KeepsSpacing()
    {
        var conveyor = Create(3);
        conveyor.TryInsert(Shooter(0));
        conveyor.TryInsert(Shooter(1));

        // Cả hai hết delay cùng lúc, nhưng con thứ 2 phải chờ con đầu đi đủ xa.
        for (int i = 0; i < 200; i++)
        {
            conveyor.Tick(0.01f);
            var a = conveyor.Units[0];
            var b = conveyor.Units[1];
            if (a.State == BeltUnitState.Moving && b.State == BeltUnitState.Moving)
                Assert.GreaterOrEqual(a.Distance - b.Distance, Spacing - 1e-3f);
        }
        Assert.AreEqual(BeltUnitState.Moving, conveyor.Units[1].State);
    }

    [Test]
    public void ManyInsertsAtOnce_JumpOneByOne()
    {
        var conveyor = Create(3);
        var jumps = new List<ShooterModel>();
        conveyor.OnJumpStarted.Subscribe(unit => jumps.Add(unit.Shooter));
        var a = Shooter(0);
        var b = Shooter(1);
        var c = Shooter(2);
        conveyor.TryInsert(a);
        conveyor.TryInsert(b);
        conveyor.TryInsert(c);

        CollectionAssert.AreEqual(new[] { a }, jumps, "Chỉ con đầu hàng chờ nhảy ngay");

        conveyor.Tick(InsertDelay + 1e-4f); // a vào băng -> b bắt đầu nhảy
        CollectionAssert.AreEqual(new[] { a, b }, jumps);

        for (int i = 0; i < 200; i++) conveyor.Tick(0.01f);
        CollectionAssert.AreEqual(new[] { a, b, c }, jumps);
    }

    [Test]
    public void LapCompleted_RemovesUnit_FreesSlot()
    {
        var conveyor = Create(1);
        var shooter = Shooter(0);
        var completed = new List<ShooterModel>();
        conveyor.OnLapCompleted.Subscribe(completed.Add);
        conveyor.TryInsert(shooter);

        float lapTime = _path.Length / Speed;
        conveyor.Tick(InsertDelay + 1e-4f);
        for (int i = 0; i < 100; i++) conveyor.Tick(lapTime / 100f + 1e-5f);

        CollectionAssert.AreEqual(new[] { shooter }, completed);
        Assert.AreEqual(0, conveyor.Units.Count);
        Assert.AreEqual(1, conveyor.Available.CurrentValue);
    }

    [Test]
    public void Looping_KeepsUnitOnBelt_AndCountsLaps()
    {
        var conveyor = Create(1);
        conveyor.IsLooping = true;
        conveyor.TryInsert(Shooter(0));
        conveyor.Tick(InsertDelay + 1e-4f);

        float lapTime = _path.Length / Speed;
        for (int i = 0; i < 250; i++) conveyor.Tick(lapTime / 100f);

        Assert.AreEqual(1, conveyor.Units.Count);
        Assert.AreEqual(2, conveyor.Units[0].Laps);
        Assert.Less(conveyor.Units[0].Distance, _path.Length);
    }

    [Test]
    public void Remove_FreesSlot_AndRaisesEvent()
    {
        var conveyor = Create(1);
        var shooter = Shooter(0);
        var removed = new List<ShooterModel>();
        conveyor.OnRemoved.Subscribe(removed.Add);
        conveyor.TryInsert(shooter);

        Assert.IsTrue(conveyor.Remove(shooter));
        Assert.AreEqual(1, conveyor.Available.CurrentValue);
        CollectionAssert.AreEqual(new[] { shooter }, removed);
    }
}

public class EndRushSystemTests
{
    private GameSession _session;
    private ShooterPickService _pick;
    private ShooterColumns _columns;
    private ConveyorModel _conveyor;
    private ConveyorController _controller;
    private PixelBoard _board;
    private CacheTray _tray;
    private EndRushSystem _rush;

    private void Build(int capacity, List<ColumnSpec> columns)
    {
        _session = new GameSession();
        _pick = new ShooterPickService(_session);
        _columns = new ShooterColumns(columns);
        _conveyor = new ConveyorModel(new RectConveyorPath(Vector3.zero, 5f, 5f, 0f), 5f, 1f, 0.1f, capacity);
        _controller = new ConveyorController(_pick, _conveyor);
        _board = new PixelBoard(1, 1, new[] { Red }, 0.1f);
        _tray = new CacheTray(5);
        _rush = new EndRushSystem(_session, _pick, _conveyor, _controller, _board, _columns, _tray);
        _session.StartPlaying();
    }

    [TearDown]
    public void TearDown()
    {
        _rush.Dispose();
        _tray.Dispose();
        _controller.Dispose();
        _conveyor.Dispose();
        _columns.Dispose();
        _pick.Dispose();
        _board.Dispose();
        _session.Dispose();
    }

    [Test]
    public void DoesNotStart_BeforeFirstPick_EvenIfFewShooters()
    {
        Build(5, new List<ColumnSpec> { Column((Red, 1)) });
        _rush.Check();
        Assert.IsFalse(_rush.IsActive.CurrentValue);
    }

    [Test]
    public void Starts_WhenAllRemainingFitOnBelt_AutoPicksRest_AndLocksInput()
    {
        // 3 shooter, 2 slot: chọn 1 con -> còn 2 chờ + 1 trên băng = 3 > 2 -> chưa rush.
        Build(2, new List<ColumnSpec> { Column((Red, 1), (Red, 1), (Red, 1)) });
        _pick.TryPick(_columns.GetFront(0));
        Assert.IsFalse(_rush.IsActive.CurrentValue);

        // Con đầu chạy hết vòng và rời băng (chưa có khay) -> còn 2 chờ <= 2 slot -> rush.
        _conveyor.Remove(_conveyor.Units[0].Shooter);

        Assert.IsTrue(_rush.IsActive.CurrentValue);
        Assert.IsTrue(_conveyor.IsLooping);
        Assert.IsTrue(_pick.IsLocked);
        Assert.AreEqual(0, _columns.RemainingInColumns, "Shooter còn chờ được tự đưa lên băng");
        Assert.AreEqual(2, _conveyor.Units.Count);
    }

    [Test]
    public void LockedInput_RejectsManualPicks()
    {
        Build(5, new List<ColumnSpec> { Column((Red, 1)), Column((Red, 1)) });
        _pick.IsLocked = true;
        Assert.IsFalse(_pick.TryPick(_columns.GetFront(0)));
    }
}
