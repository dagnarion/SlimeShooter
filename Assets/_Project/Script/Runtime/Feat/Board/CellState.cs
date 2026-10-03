public enum CellState
{
    /// <summary>Không có pixel (ô trong suốt của ảnh).</summary>
    Empty,
    Alive,
    /// <summary>Đã bị bắn, đang chờ đạn bay tới + animation vỡ. Vẫn chặn đường bắn.</summary>
    Breaking,
    Dead
}

/// <summary>Cạnh của board mà shooter đang đứng để bắn vào.</summary>
public enum BoardSide
{
    /// <summary>Bắn lên theo cột x, từ y = 0.</summary>
    Bottom,
    /// <summary>Bắn sang trái theo hàng y, từ x = width - 1.</summary>
    Right,
    /// <summary>Bắn xuống theo cột x, từ y = height - 1.</summary>
    Top,
    /// <summary>Bắn sang phải theo hàng y, từ x = 0.</summary>
    Left
}
