using UnityEngine;

/// <summary>Đường chạy khép kín của băng chuyền, tham số hoá theo quãng đường (world unit).</summary>
public interface IConveyorPath
{
    float Length { get; }
    Bounds Bounds { get; }

    /// <summary>Vị trí tại quãng đường <paramref name="distance"/> (tự quấn vòng), kèm hướng đi.</summary>
    Vector3 Evaluate(float distance, out Vector3 forward);

    /// <summary>Cạnh board mà điểm này đang đối diện; false nếu đang ở góc cua.</summary>
    bool TryGetSide(float distance, out BoardSide side);
}
