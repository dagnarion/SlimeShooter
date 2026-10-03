using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>Hiệu ứng khi vào end rush: tăng tốc toàn game. Banner UI làm ở P8.</summary>
public class EndRushPresenter : MonoBehaviour
{
    [Inject] private EndRushSystem _endRush;
    [Inject] private ConveyorConfigSO _config;

    private void Start()
    {
        _endRush.IsActive
            .Where(active => active)
            .Subscribe(_ =>
            {
                Time.timeScale = _config.EndRushTimeScale;
                Debug.Log($"[EndRush] Bắt đầu — timeScale {_config.EndRushTimeScale}");
            })
            .AddTo(this);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
