/// <summary>
/// Hồi sinh khi thua vì khay đầy: thêm 1 ô khay, đưa shooter bị chặn vào ô đó và chơi tiếp.
/// Số lần revive mỗi level giới hạn (sau này có thể đổi bằng xem quảng cáo / tiền).
/// </summary>
public class ReviveService
{
    private readonly GameSession _session;
    private readonly RuleSystem _rules;
    private readonly CacheTray _tray;

    public ReviveService(GameSession session, RuleSystem rules, CacheTray tray, int maxRevives = 1)
    {
        _session = session;
        _rules = rules;
        _tray = tray;
        RevivesLeft = maxRevives;
    }

    public int RevivesLeft { get; private set; }

    public bool CanRevive => _session.CurrentState == GameState.Lost
                             && _rules.LoseReason == LoseReason.TrayFull
                             && RevivesLeft > 0;

    public bool Revive()
    {
        if (!CanRevive) return false;

        var blocked = _rules.BlockedShooter;
        _tray.AddCapacity(1);
        if (blocked != null) _tray.TryAdd(blocked);

        RevivesLeft--;
        _rules.ClearLose();
        return _session.Revive();
    }
}
