using DG.Tweening;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

/// <summary>
/// Vẽ băng chuyền, đặt shooter theo vị trí trên băng, bộ đếm slot "x/N" ở cửa vào,
/// và nháy đỏ khi hết slot.
/// </summary>
public class ConveyorView : MonoBehaviour
{
    [Header("Belt")]
    [SerializeField] private LineRenderer beltLine;
    [Min(8)] [SerializeField] private int samplesPerUnit = 4;
    [SerializeField] private Color beltColor = new Color(0.25f, 0.55f, 0.95f);
    [SerializeField] private Color rejectColor = new Color(0.95f, 0.25f, 0.25f);

    [Header("Entrance")]
    [SerializeField] private TMP_Text slotCounter;
    [SerializeField] private Vector3 counterOffset = new Vector3(-1.2f, 0.1f, -1.2f);

    [Header("Shooter hết đạn")]
    [SerializeField] private ShooterViewPoolSO shooterPool;
    [SerializeField] private float despawnDuration = 0.25f;

    [Inject] private ConveyorModel _conveyor;
    [Inject] private ConveyorConfigSO _config;
    [Inject] private ShooterViewRegistry _registry;

    private Tween _rejectTween;

    private void Start()
    {
        DrawBelt();
        PlaceCounter();

        _conveyor.Available
            .CombineLatest(_conveyor.Capacity, (available, capacity) => $"{available}/{capacity}")
            .Subscribe(text => { if (slotCounter != null) slotCounter.text = text; })
            .AddTo(this);

        _conveyor.OnJumpStarted.Subscribe(OnJumpStarted).AddTo(this);
        _conveyor.OnAttached.Subscribe(unit => { if (_registry.TryGet(unit.Shooter, out var v)) v.StopMotion(); }).AddTo(this);
        _conveyor.OnInsertRejected.Subscribe(_ => PlayRejectFeedback()).AddTo(this);
        _conveyor.OnRemoved.Subscribe(OnRemoved).AddTo(this);
    }

    private void LateUpdate()
    {
        var path = _conveyor.Path;
        foreach (var unit in _conveyor.Units)
        {
            if (unit.State != BeltUnitState.Moving) continue;
            if (!_registry.TryGet(unit.Shooter, out var view)) continue;

            Vector3 position = path.Evaluate(unit.Distance, out Vector3 forward);
            view.transform.position = position;
            view.Face(RectConveyorPath.Inward(forward));
        }
    }

    private void OnJumpStarted(BeltUnit unit)
    {
        if (!_registry.TryGet(unit.Shooter, out var view)) return;
        Vector3 entrance = _conveyor.Path.Evaluate(0f, out Vector3 forward);
        view.JumpTo(entrance, _config.JumpDuration);
        view.Face(RectConveyorPath.Inward(forward), _config.JumpDuration);
    }

    /// <summary>Shooter bị gỡ khỏi băng giữa chừng (hết đạn): thu nhỏ rồi trả về pool.</summary>
    private void OnRemoved(ShooterModel shooter)
    {
        if (shooter.State.CurrentValue != ShooterState.Dead) return;
        if (!_registry.TryGet(shooter, out var view)) return;
        _registry.Unregister(shooter);
        view.StopMotion();
        view.transform.DOScale(Vector3.zero, despawnDuration).SetEase(Ease.InBack).SetLink(view.gameObject)
            .OnComplete(() =>
            {
                view.transform.localScale = Vector3.one;
                if (shooterPool != null) shooterPool.Release(view);
                else view.gameObject.SetActive(false);
            });
    }

    private void DrawBelt()
    {
        if (beltLine == null) return;
        var path = _conveyor.Path;
        int count = Mathf.Max(16, Mathf.CeilToInt(path.Length * samplesPerUnit));
        beltLine.loop = true;
        beltLine.useWorldSpace = true;
        beltLine.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = path.Evaluate(path.Length * i / count, out _);
            beltLine.SetPosition(i, p + Vector3.down * 0.05f);
        }
        SetBeltColor(beltColor);
    }

    private void PlaceCounter()
    {
        if (slotCounter == null) return;
        Vector3 entrance = _conveyor.Path.Evaluate(0f, out _);
        slotCounter.transform.position = entrance + counterOffset;
    }

    private void PlayRejectFeedback()
    {
        if (beltLine == null) return;
        _rejectTween?.Kill();
        SetBeltColor(rejectColor);
        _rejectTween = DOVirtual.Float(0f, 1f, 0.3f, t => SetBeltColor(Color.Lerp(rejectColor, beltColor, t)))
            .SetLink(gameObject);
        if (slotCounter != null) slotCounter.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 8).SetLink(slotCounter.gameObject);
    }

    private void SetBeltColor(Color color)
    {
        beltLine.startColor = color;
        beltLine.endColor = color;
    }
}
