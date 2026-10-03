using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD: số level, thanh % pixel đã phá, nút tạm dừng.</summary>
public class HUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button pauseButton;

    [Inject] private LevelProgressSystem _progress;
    [Inject] private PixelBoard _board;
    [Inject] private GameFlowService _flow;
    [Inject] private GameSession _session;

    private void Start()
    {
        if (levelText != null) levelText.text = $"LEVEL {_progress.PlayedLevelNumber}";

        _board.Remaining
            .Subscribe(remaining =>
            {
                float progress = _board.TotalCount > 0 ? 1f - remaining / (float)_board.TotalCount : 1f;
                if (progressFill != null) progressFill.fillAmount = progress;
                if (progressText != null) progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            })
            .AddTo(this);

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(_flow.Pause);
            _session.State.Subscribe(state => pauseButton.interactable = state == GameState.Playing).AddTo(this);
        }
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(_flow.Pause);
    }
}
