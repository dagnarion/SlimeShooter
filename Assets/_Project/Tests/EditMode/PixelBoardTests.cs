using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;

public class PixelBoardTests
{
    private const int R = 0;
    private const int G = 1;
    private const int E = LevelSO.EmptyCell;
    private const float BreakTime = 0.3f;

    // Lưới 3x3, viết theo hàng từ dưới lên (y = 0 trước):
    //   y=2:  G R R
    //   y=1:  R E G
    //   y=0:  R R G
    private static readonly int[] Cells = { R, R, G, R, E, G, G, R, R };

    private PixelBoard _board;

    [SetUp]
    public void SetUp() => _board = new PixelBoard(3, 3, Cells, BreakTime);

    [TearDown]
    public void TearDown() => _board.Dispose();

    [Test]
    public void TotalAndRemaining_IgnoreEmptyCells()
    {
        Assert.AreEqual(8, _board.TotalCount);
        Assert.AreEqual(8, _board.Remaining.CurrentValue);
        Assert.AreEqual(CellState.Empty, _board.StateAt(new Vector2Int(1, 1)));
    }

    [Test]
    public void TryGetTarget_FromEachSide_ReturnsFirstCellFromEdge()
    {
        Assert.IsTrue(_board.TryGetTarget(BoardSide.Bottom, 0, R, out var cell));
        Assert.AreEqual(new Vector2Int(0, 0), cell);

        Assert.IsTrue(_board.TryGetTarget(BoardSide.Top, 0, G, out cell));
        Assert.AreEqual(new Vector2Int(0, 2), cell);

        Assert.IsTrue(_board.TryGetTarget(BoardSide.Left, 2, G, out cell));
        Assert.AreEqual(new Vector2Int(0, 2), cell);

        Assert.IsTrue(_board.TryGetTarget(BoardSide.Right, 1, G, out cell));
        Assert.AreEqual(new Vector2Int(2, 1), cell);
    }

    [Test]
    public void DifferentColorInFront_BlocksTarget()
    {
        // Cột 2 từ dưới: G (0) -> chặn shooter màu R
        Assert.IsFalse(_board.TryGetTarget(BoardSide.Bottom, 2, R, out _));
    }

    [Test]
    public void EmptyCells_AreSkipped()
    {
        // Cột 1 từ dưới: R(1,0), E(1,1), R(1,2). Phá (1,0) xong thì (1,2) lộ ra qua ô trống.
        _board.BeginBreak(new Vector2Int(1, 0));
        _board.Tick(BreakTime);

        Assert.IsTrue(_board.TryGetTarget(BoardSide.Bottom, 1, R, out var cell));
        Assert.AreEqual(new Vector2Int(1, 2), cell);
    }

    [Test]
    public void BreakingCell_BlocksCellBehind_UntilDead()
    {
        // Cột 0 từ dưới: R(0,0), R(0,1), G(0,2)
        Assert.IsTrue(_board.BeginBreak(new Vector2Int(0, 0)));

        Assert.AreEqual(CellState.Breaking, _board.StateAt(new Vector2Int(0, 0)));
        Assert.IsFalse(_board.TryGetTarget(BoardSide.Bottom, 0, R, out _), "Ô đang vỡ phải chặn ô phía sau");
        Assert.AreEqual(8, _board.Remaining.CurrentValue, "Breaking vẫn tính là còn lại");

        _board.Tick(BreakTime * 0.5f);
        Assert.IsFalse(_board.TryGetTarget(BoardSide.Bottom, 0, R, out _));

        _board.Tick(BreakTime * 0.5f);
        Assert.AreEqual(CellState.Dead, _board.StateAt(new Vector2Int(0, 0)));
        Assert.IsTrue(_board.TryGetTarget(BoardSide.Bottom, 0, R, out var cell));
        Assert.AreEqual(new Vector2Int(0, 1), cell);
        Assert.AreEqual(7, _board.Remaining.CurrentValue);
    }

    [Test]
    public void BeginBreak_OnlyAliveCells()
    {
        var cell = new Vector2Int(0, 0);
        Assert.IsTrue(_board.BeginBreak(cell));
        Assert.IsFalse(_board.BeginBreak(cell), "Đang Breaking");
        _board.Tick(BreakTime);
        Assert.IsFalse(_board.BeginBreak(cell), "Đã Dead");
        Assert.IsFalse(_board.BeginBreak(new Vector2Int(1, 1)), "Ô trống");
        Assert.IsFalse(_board.BeginBreak(new Vector2Int(9, 9)), "Ngoài lưới");
    }

    [Test]
    public void Events_FireInOrder()
    {
        var breaking = new List<Vector2Int>();
        var dead = new List<Vector2Int>();
        using (_board.OnCellBreaking.Subscribe(breaking.Add))
        using (_board.OnCellDead.Subscribe(dead.Add))
        {
            _board.BeginBreak(new Vector2Int(0, 0));
            _board.BeginBreak(new Vector2Int(2, 0));
            Assert.AreEqual(2, breaking.Count);
            Assert.AreEqual(0, dead.Count);

            _board.Tick(BreakTime);
            CollectionAssert.AreEqual(new[] { new Vector2Int(0, 0), new Vector2Int(2, 0) }, dead);
        }
    }

    [Test]
    public void ManySmallTicks_EqualOneBigTick()
    {
        var a = new PixelBoard(3, 3, Cells, BreakTime);
        var b = new PixelBoard(3, 3, Cells, BreakTime);
        a.BeginBreak(new Vector2Int(0, 0));
        b.BeginBreak(new Vector2Int(0, 0));

        for (int i = 0; i < 30; i++) a.Tick(BreakTime / 30f + 1e-6f);
        b.Tick(BreakTime + 1e-6f);

        Assert.AreEqual(b.StateAt(new Vector2Int(0, 0)), a.StateAt(new Vector2Int(0, 0)));
        Assert.AreEqual(CellState.Dead, a.StateAt(new Vector2Int(0, 0)));
        a.Dispose();
        b.Dispose();
    }

    [Test]
    public void ZeroBreakTime_DiesImmediately()
    {
        using (var board = new PixelBoard(3, 3, Cells, 0f))
        {
            board.BeginBreak(new Vector2Int(0, 0));
            Assert.AreEqual(CellState.Dead, board.StateAt(new Vector2Int(0, 0)));
            Assert.AreEqual(7, board.Remaining.CurrentValue);
        }
    }

    [Test]
    public void ClearingEverything_RemainingReachesZero()
    {
        int safety = 0;
        while (_board.Remaining.CurrentValue > 0 && safety++ < 100)
        {
            foreach (BoardSide side in System.Enum.GetValues(typeof(BoardSide)))
            for (int line = 0; line < _board.LineCount(side); line++)
            {
                if (_board.TryGetFirstBlocking(side, line, out var cell) && _board.StateAt(cell) == CellState.Alive)
                    _board.BeginBreak(cell);
            }
            _board.Tick(BreakTime);
        }

        Assert.AreEqual(0, _board.Remaining.CurrentValue);
    }
}

public class BoardLayoutTests
{
    [Test]
    public void CellToWorld_CentersBoardOnAnchor()
    {
        var layout = new BoardLayout(new Vector3(10f, 2f, -5f), 3, 4, 2f);

        Assert.AreEqual(new Vector3(8f, 2f, -8f), layout.CellToWorld(new Vector2Int(0, 0)));
        Assert.AreEqual(new Vector3(12f, 2f, -2f), layout.CellToWorld(new Vector2Int(2, 3)));
    }

    [Test]
    public void WorldToLine_RoundTripsCellCenters()
    {
        var layout = new BoardLayout(Vector3.zero, 5, 7, 0.5f);
        for (int x = 0; x < 5; x++)
        for (int y = 0; y < 7; y++)
        {
            var world = layout.CellToWorld(new Vector2Int(x, y));
            Assert.IsTrue(layout.TryWorldToLine(BoardSide.Bottom, world, out int column));
            Assert.IsTrue(layout.TryWorldToLine(BoardSide.Left, world, out int row));
            Assert.AreEqual(x, column);
            Assert.AreEqual(y, row);
        }
    }

    [Test]
    public void WorldToLine_OutsideBoard_ReturnsFalse()
    {
        var layout = new BoardLayout(Vector3.zero, 3, 3, 1f);
        Assert.IsFalse(layout.TryWorldToLine(BoardSide.Bottom, new Vector3(5f, 0f, 0f), out _));
        Assert.IsFalse(layout.TryWorldToLine(BoardSide.Right, new Vector3(0f, 0f, -3f), out _));
    }
}
