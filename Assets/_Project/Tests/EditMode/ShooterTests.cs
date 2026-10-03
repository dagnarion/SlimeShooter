using System.Collections.Generic;
using NUnit.Framework;
using R3;
using static LevelTestUtils;

public class ShooterModelTests
{
    [Test]
    public void ConsumeAmmo_StopsAtZero()
    {
        using (var shooter = new ShooterModel(0, Red, 2))
        {
            Assert.IsTrue(shooter.ConsumeAmmo());
            Assert.IsTrue(shooter.ConsumeAmmo());
            Assert.IsFalse(shooter.ConsumeAmmo());
            Assert.AreEqual(0, shooter.Ammo.CurrentValue);
            Assert.IsFalse(shooter.HasAmmo);
        }
    }
}

public class ShooterColumnsTests
{
    private ShooterColumns _columns;

    [SetUp]
    public void SetUp()
    {
        _columns = new ShooterColumns(new List<ColumnSpec>
        {
            Column((Red, 10), (Green, 20)),
            Column((Blue, 15)),
        });
    }

    [TearDown]
    public void TearDown() => _columns.Dispose();

    [Test]
    public void BuildsFromSpecs_WithUniqueIds_AndContainerSet()
    {
        Assert.AreEqual(2, _columns.ColumnCount);
        Assert.AreEqual(3, _columns.AllShooters.Count);

        var ids = new HashSet<int>();
        foreach (var shooter in _columns.AllShooters)
        {
            Assert.IsTrue(ids.Add(shooter.Id));
            Assert.AreSame(_columns, shooter.Container);
            Assert.AreEqual(ShooterState.InColumn, shooter.State.CurrentValue);
        }

        var front = _columns.GetFront(0);
        Assert.AreEqual(Red, front.ColorId);
        Assert.AreEqual(10, front.Ammo.CurrentValue);
    }

    [Test]
    public void OnlyFrontShooter_CanBePicked()
    {
        var column = _columns.GetColumn(0);
        Assert.IsTrue(_columns.CanPick(column[0]));
        Assert.IsFalse(_columns.CanPick(column[1]));
    }

    [Test]
    public void Remove_ShiftsColumn_AndRaisesEvent()
    {
        var changed = new List<int>();
        using (_columns.OnColumnChanged.Subscribe(changed.Add))
        {
            var front = _columns.GetFront(0);
            var second = _columns.GetColumn(0)[1];

            Assert.IsTrue(_columns.Remove(front));

            Assert.AreSame(second, _columns.GetFront(0));
            Assert.IsTrue(_columns.CanPick(second));
            Assert.IsNull(front.Container);
            CollectionAssert.AreEqual(new[] { 0 }, changed);
            Assert.AreEqual(2, _columns.RemainingInColumns);
            Assert.AreEqual(3, _columns.AllShooters.Count, "AllShooters giữ cả shooter đã rời cột");
        }
    }

    [Test]
    public void EmptyColumn_HasNoFront()
    {
        _columns.Remove(_columns.GetFront(1));
        Assert.IsNull(_columns.GetFront(1));
        Assert.IsFalse(_columns.Remove(new ShooterModel(99, Red, 1)));
    }
}

public class ShooterPickServiceTests
{
    private GameSession _session;
    private ShooterColumns _columns;
    private ShooterPickService _pick;
    private List<ShooterModel> _picked;
    private List<ShooterModel> _rejected;

    [SetUp]
    public void SetUp()
    {
        _session = new GameSession();
        _columns = new ShooterColumns(new List<ColumnSpec> { Column((Red, 10), (Green, 20)) });
        _pick = new ShooterPickService(_session);
        _picked = new List<ShooterModel>();
        _rejected = new List<ShooterModel>();
        _pick.OnPicked.Subscribe(_picked.Add);
        _pick.OnPickRejected.Subscribe(_rejected.Add);
    }

    [TearDown]
    public void TearDown()
    {
        _pick.Dispose();
        _columns.Dispose();
        _session.Dispose();
    }

    [Test]
    public void IgnoresPicks_WhenNotPlaying()
    {
        Assert.IsFalse(_pick.TryPick(_columns.GetFront(0)));
        Assert.IsEmpty(_picked);
        Assert.IsEmpty(_rejected);
    }

    [Test]
    public void FrontShooter_IsPicked_OthersRejected()
    {
        _session.StartPlaying();
        var front = _columns.GetFront(0);
        var behind = _columns.GetColumn(0)[1];

        Assert.IsTrue(_pick.TryPick(front));
        Assert.IsFalse(_pick.TryPick(behind));

        CollectionAssert.AreEqual(new[] { front }, _picked);
        CollectionAssert.AreEqual(new[] { behind }, _rejected);
    }

    [Test]
    public void ShooterWithoutContainer_IsRejected()
    {
        _session.StartPlaying();
        var front = _columns.GetFront(0);
        _columns.Remove(front); // đã rời cột (ví dụ đang trên băng chuyền)

        Assert.IsFalse(_pick.TryPick(front));
        Assert.IsEmpty(_picked);
    }
}
