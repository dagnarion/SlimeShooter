using DG.Tweening;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

/// <summary>Bật/tắt popup theo trạng thái game và hiện banner khi vào end rush.</summary>
public class GameUIController : MonoBehaviour
{
    [SerializeField] private WinPopup winPopup;
    [SerializeField] private LosePopup losePopup;
    [SerializeField] private PausePopup pausePopup;
    [Tooltip("Chờ một chút sau khi thắng để người chơi thấy ô cuối vỡ.")]
    [SerializeField] private float winDelay = 0.6f;

    [Header("Rush banner")]
    [SerializeField] private TMP_Text rushBanner;
    [SerializeField] private string rushText = "SLIME RUSH!";

    [Inject] private GameSession _session;
    [Inject] private EndRushSystem _endRush;

    private Tween _winDelayTween;

    private void Start()
    {
        if (rushBanner != null) rushBanner.gameObject.SetActive(false);

        _session.State.Subscribe(OnStateChanged).AddTo(this);
        _endRush.IsActive.Where(active => active).Subscribe(_ => ShowRushBanner()).AddTo(this);
    }

    private void OnStateChanged(GameState state)
    {
        _winDelayTween?.Kill();
        SetVisible(pausePopup, state == GameState.Paused);
        SetVisible(losePopup, state == GameState.Lost);

        if (state == GameState.Won)
            _winDelayTween = DOVirtual.DelayedCall(winDelay, () => winPopup.Show(), ignoreTimeScale: true).SetLink(gameObject);
        else
            SetVisible(winPopup, false);
    }

    private static void SetVisible(UIPopup popup, bool visible)
    {
        if (popup == null || popup.IsVisible == visible) return;
        if (visible) popup.Show(); else popup.Hide();
    }

    private void ShowRushBanner()
    {
        if (rushBanner == null) return;
        var t = rushBanner.transform;
        rushBanner.text = rushText;
        rushBanner.gameObject.SetActive(true);
        rushBanner.alpha = 1f;
        t.localScale = Vector3.zero;
        DOTween.Sequence().SetUpdate(true).SetLink(rushBanner.gameObject)
            .Append(t.DOScale(1.15f, 0.25f).SetEase(Ease.OutBack))
            .Append(t.DOScale(1f, 0.1f))
            .AppendInterval(0.9f)
            .Append(rushBanner.DOFade(0f, 0.3f))
            .OnComplete(() => rushBanner.gameObject.SetActive(false));
    }

    private void OnDestroy() => _winDelayTween?.Kill();
}
