/// <summary>Nơi giữ shooter khi chưa lên băng chuyền: cột shooter, khay chờ (P7)...</summary>
public interface IShooterContainer
{
    bool CanPick(ShooterModel shooter);

    /// <summary>Lấy shooter ra khỏi container (sau khi đã được băng chuyền nhận).</summary>
    bool Remove(ShooterModel shooter);
}
