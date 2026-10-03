using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

/// <summary>
/// Model lưới pixel. Mỗi ô: Empty / Alive -> Breaking -> Dead.
/// Chỉ ô Alive đầu tiên trên đường bắn (tính từ mép, bỏ qua ô Dead) mới là mục tiêu;
/// ô Breaking vẫn chặn đường nên ô phía sau chỉ lộ ra khi ô trước đã Dead.
/// </summary>
public class PixelBoard : ITickable, IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly int[] _colors;
    private readonly CellState[] _states;
    private readonly float _breakTime;

    private readonly List<int> _breaking = new List<int>();
    private readonly Dictionary<int, float> _breakTimers = new Dictionary<int, float>();

    private readonly ReactiveProperty<int> _remaining;
    private readonly Subject<Vector2Int> _onCellBreaking = new Subject<Vector2Int>();
    private readonly Subject<Vector2Int> _onCellDead = new Subject<Vector2Int>();

    public PixelBoard(LevelSO level, BoardConfigSO config)
        : this(level.Width, level.Height, level.Cells, config.BreakTime) { }

    /// <param name="cells">Row-major, y = 0 là hàng dưới cùng, <see cref="LevelSO.EmptyCell"/> = ô trống.</param>
    public PixelBoard(int width, int height, IReadOnlyList<int> cells, float breakTime)
    {
        if (cells.Count != width * height) throw new ArgumentException($"cells.Count {cells.Count} != {width}x{height}");

        _width = width;
        _height = height;
        _breakTime = Mathf.Max(0f, breakTime);
        _colors = new int[cells.Count];
        _states = new CellState[cells.Count];

        int alive = 0;
        for (int i = 0; i < cells.Count; i++)
        {
            _colors[i] = cells[i];
            _states[i] = cells[i] == LevelSO.EmptyCell ? CellState.Empty : CellState.Alive;
            if (_states[i] == CellState.Alive) alive++;
        }

        TotalCount = alive;
        AliveCount = alive;
        _remaining = new ReactiveProperty<int>(alive);
    }

    public int Width => _width;
    public int Height => _height;
    public int TotalCount { get; }

    /// <summary>Số ô chưa Dead (Alive + Breaking). Về 0 = thắng.</summary>
    public ReadOnlyReactiveProperty<int> Remaining => _remaining;

    /// <summary>Số ô còn bắn được (chưa bị bắn).</summary>
    public int AliveCount { get; private set; }

    public Observable<Vector2Int> OnCellBreaking => _onCellBreaking;
    public Observable<Vector2Int> OnCellDead => _onCellDead;

    public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < _width && cell.y < _height;
    public int ColorAt(Vector2Int cell) => InBounds(cell) ? _colors[Index(cell)] : LevelSO.EmptyCell;
    public CellState StateAt(Vector2Int cell) => InBounds(cell) ? _states[Index(cell)] : CellState.Empty;

    /// <summary>Số đường bắn của một cạnh (số cột với Bottom/Top, số hàng với Left/Right).</summary>
    public int LineCount(BoardSide side) => side == BoardSide.Bottom || side == BoardSide.Top ? _width : _height;

    /// <summary>Ô đầu tiên chưa Dead trên đường bắn (Alive hoặc Breaking).</summary>
    public bool TryGetFirstBlocking(BoardSide side, int line, out Vector2Int cell)
    {
        cell = default;
        if (line < 0 || line >= LineCount(side)) return false;

        int length = side == BoardSide.Bottom || side == BoardSide.Top ? _height : _width;
        for (int step = 0; step < length; step++)
        {
            var current = CellOnLine(side, line, step);
            var state = _states[Index(current)];
            if (state == CellState.Alive || state == CellState.Breaking)
            {
                cell = current;
                return true;
            }
        }
        return false;
    }

    /// <summary>Mục tiêu hợp lệ: ô chặn đầu tiên phải Alive và cùng màu.</summary>
    public bool TryGetTarget(BoardSide side, int line, int colorId, out Vector2Int cell)
    {
        if (!TryGetFirstBlocking(side, line, out cell)) return false;
        int index = Index(cell);
        return _states[index] == CellState.Alive && _colors[index] == colorId;
    }

    /// <summary>Ô Alive cùng màu gần <paramref name="from"/> nhất (theo toạ độ lưới, liên tục). Dùng cho end rush.</summary>
    public bool TryFindNearestAlive(int colorId, Vector2 from, out Vector2Int cell)
    {
        cell = default;
        float best = float.MaxValue;
        for (int i = 0; i < _states.Length; i++)
        {
            if (_states[i] != CellState.Alive || _colors[i] != colorId) continue;
            var candidate = new Vector2Int(i % _width, i / _width);
            float d = (candidate - from).sqrMagnitude;
            if (d >= best) continue;
            best = d;
            cell = candidate;
        }
        return best < float.MaxValue;
    }

    /// <summary>Có ô màu này đang lộ ra (bắn được) từ bất kỳ cạnh nào không.</summary>
    public bool HasExposed(int colorId)
    {
        for (int s = 0; s < 4; s++)
        {
            var side = (BoardSide)s;
            int lines = LineCount(side);
            for (int line = 0; line < lines; line++)
            {
                if (TryGetTarget(side, line, colorId, out _)) return true;
            }
        }
        return false;
    }

    public bool HasAlive(int colorId)
    {
        for (int i = 0; i < _states.Length; i++)
        {
            if (_states[i] == CellState.Alive && _colors[i] == colorId) return true;
        }
        return false;
    }

    public bool BeginBreak(Vector2Int cell)
    {
        if (!InBounds(cell)) return false;
        int index = Index(cell);
        if (_states[index] != CellState.Alive) return false;

        _states[index] = CellState.Breaking;
        AliveCount--;
        _onCellBreaking.OnNext(cell);

        if (_breakTime <= 0f)
        {
            MarkDead(index);
        }
        else
        {
            _breaking.Add(index);
            _breakTimers[index] = _breakTime;
        }
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (_breaking.Count == 0) return;

        for (int i = 0; i < _breaking.Count; i++)
        {
            int index = _breaking[i];
            _breakTimers[index] -= deltaTime;
        }

        for (int i = 0; i < _breaking.Count; i++)
        {
            int index = _breaking[i];
            if (_breakTimers[index] > 0f) continue;

            _breaking.RemoveAt(i);
            _breakTimers.Remove(index);
            i--;
            MarkDead(index);
        }
    }

    private void MarkDead(int index)
    {
        _states[index] = CellState.Dead;
        _remaining.Value--;
        _onCellDead.OnNext(new Vector2Int(index % _width, index / _width));
    }

    private Vector2Int CellOnLine(BoardSide side, int line, int step)
    {
        switch (side)
        {
            case BoardSide.Bottom: return new Vector2Int(line, step);
            case BoardSide.Top: return new Vector2Int(line, _height - 1 - step);
            case BoardSide.Left: return new Vector2Int(step, line);
            default: return new Vector2Int(_width - 1 - step, line);
        }
    }

    private int Index(Vector2Int cell) => cell.y * _width + cell.x;

    public void Dispose()
    {
        _remaining.Dispose();
        _onCellBreaking.Dispose();
        _onCellDead.Dispose();
    }
}
