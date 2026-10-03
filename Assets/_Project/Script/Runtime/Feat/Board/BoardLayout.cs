using UnityEngine;

/// <summary>
/// Quy đổi ô lưới &lt;-&gt; world. Board nằm trên mặt phẳng XZ, tâm board tại <see cref="Center"/>:
/// cột x chạy theo trục +X, hàng y chạy theo trục +Z.
/// </summary>
public sealed class BoardLayout
{
    public readonly Vector3 Center;
    public readonly int Width;
    public readonly int Height;
    public readonly float CellSize;

    public BoardLayout(Vector3 center, int width, int height, float cellSize)
    {
        Center = center;
        Width = width;
        Height = height;
        CellSize = cellSize;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            Center.x + (cell.x - (Width - 1) * 0.5f) * CellSize,
            Center.y,
            Center.z + (cell.y - (Height - 1) * 0.5f) * CellSize);
    }

    /// <summary>Toạ độ liên tục (chưa làm tròn) của cột tại vị trí world.x.</summary>
    public float WorldToColumn(float worldX) => (worldX - Center.x) / CellSize + (Width - 1) * 0.5f;

    /// <summary>Toạ độ liên tục (chưa làm tròn) của hàng tại vị trí world.z.</summary>
    public float WorldToRow(float worldZ) => (worldZ - Center.z) / CellSize + (Height - 1) * 0.5f;

    /// <summary>Đường bắn mà một điểm world đang đối diện khi đứng ở cạnh <paramref name="side"/>.</summary>
    public bool TryWorldToLine(BoardSide side, Vector3 world, out int line)
    {
        bool isColumn = side == BoardSide.Bottom || side == BoardSide.Top;
        float value = isColumn ? WorldToColumn(world.x) : WorldToRow(world.z);
        line = Mathf.RoundToInt(value);
        return line >= 0 && line < (isColumn ? Width : Height);
    }

    public Bounds WorldBounds => new Bounds(Center, new Vector3(Width * CellSize, 0f, Height * CellSize));
}
