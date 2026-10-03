using UnityEngine;
using UnityEngine.SceneManagement;

public interface ISceneLoader
{
    void ReloadActive();
}

/// <summary>
/// Tải lại scene gameplay để chơi lại / sang level mới. Reflex tự dispose container của scene cũ
/// nên mọi model, subscription R3 được dọn sạch.
/// </summary>
public class SceneLoader : ISceneLoader
{
    public void ReloadActive()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
