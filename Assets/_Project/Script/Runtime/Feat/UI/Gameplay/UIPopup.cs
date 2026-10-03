using DG.Tweening;
using UnityEngine;

/// <summary>Popup đơn giản: fade nền + phóng to nội dung. Chạy theo thời gian thật (không bị timeScale ảnh hưởng).</summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPopup : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private float duration = 0.25f;

    private CanvasGroup _group;
    private Sequence _sequence;

    public bool IsVisible { get; private set; }

    protected virtual void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    public void Show()
    {
        IsVisible = true;
        gameObject.SetActive(true);
        _sequence?.Kill();
        _group.blocksRaycasts = true;
        _group.interactable = true;
        _sequence = DOTween.Sequence().SetUpdate(true).Join(_group.DOFade(1f, duration));
        if (content != null)
        {
            content.localScale = Vector3.one * 0.8f;
            _sequence.Join(content.DOScale(1f, duration).SetEase(Ease.OutBack));
        }
        OnShown();
    }

    public void Hide()
    {
        IsVisible = false;
        _sequence?.Kill();
        _group.blocksRaycasts = false;
        _group.interactable = false;
        _sequence = DOTween.Sequence().SetUpdate(true)
            .Join(_group.DOFade(0f, duration * 0.6f))
            .OnComplete(() => gameObject.SetActive(false));
    }

    public void HideImmediate()
    {
        IsVisible = false;
        _sequence?.Kill();
        if (_group == null) _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;
        gameObject.SetActive(false);
    }

    protected virtual void OnShown() { }

    private void OnDestroy() => _sequence?.Kill();
}
