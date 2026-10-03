/// <summary>Các hành động của nút UI: tạm dừng, chơi lại, sang level tiếp, hồi sinh.</summary>
public class GameFlowService
{
    private readonly GameSession _session;
    private readonly ISceneLoader _sceneLoader;
    private readonly ReviveService _revive;

    public GameFlowService(GameSession session, ISceneLoader sceneLoader, ReviveService revive)
    {
        _session = session;
        _sceneLoader = sceneLoader;
        _revive = revive;
    }

    public void Pause() => _session.Pause();
    public void Resume() => _session.Resume();
    public bool Revive() => _revive.Revive();

    public void Retry() => _sceneLoader.ReloadActive();

    /// <summary>LevelProgressSystem đã Advance lúc thắng, chỉ cần tải lại scene.</summary>
    public void Next() => _sceneLoader.ReloadActive();
}
