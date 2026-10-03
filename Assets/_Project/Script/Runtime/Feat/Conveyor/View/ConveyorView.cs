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

    [Header("Tạm thời tới P7 (khay chờ)")]
    [Tooltip("Chỗ đặt shooter chạy hết vòng khi chưa có khay chờ.")]
    [SerializeField] private Vector3 parkingOffset = new Vector3(0f, 0f, -1.8f);

    [Inject] private ConveyorModel _conveyor;
    [Inject] private ConveyorConfigSO _config;
    [Inject] private ShooterViewRegistry _registry;

    private Tween _rejectTween;
    private int _parkedCount;

    private void Start()
    {
        DrawBelt();
        PlaceCounter();

        _conveyor.Available
            .CombineLatest(_conveyor.Capacity, (available, capacity) => $"{available}/{capacity}")
            .Subscribe(text => { if (slotCounter != null) slotCounter.text = text; })
            .AddTo(this);

        _conveyor.OnInserted.Subscribe(OnInserted).AddTo(this);
        _conveyor.OnAttached.Subscribe(unit => { if (_registry.TryGet(unit.Shooter, out var v)) v.StopMotion(); }).AddTo(this);
        _conveyor.OnInsertRejected.Subscribe(_ => PlayRejectFeedback()).AddTo(this);
        _conveyor.OnLapCompleted.Subscribe(OnLapCompleted).AddTo(this);
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

    private void OnInserted(BeltUnit unit)
    {
        if (!_registry.TryGet(unit.Shooter, out var view)) return;
        Vector3 entrance = _conveyor.Path.Evaluate(0f, out Vector3 forward);
        view.JumpTo(entrance, _config.JumpDuration);
        view.Face(RectConveyorPath.Inward(forward), _config.JumpDuration);
    }

    // TODO(P7): thay bằng CacheTray — hiện chỉ xếp shooter cạnh cửa vào để không bị kẹt trên băng.
    private void OnLapCompleted(ShooterModel shooter)
    {
        if (!_registry.TryGet(shooter, out var view)) return;
        Vector3 entrance = _conveyor.Path.Evaluate(0f, out _);
        Vector3 target = entrance + parkingOffset + Vector3.right * (_parkedCount++ * 1.2f);
        view.JumpTo(target, _config.JumpDuration);
        view.Face(Vector3.forward, _config.JumpDuration);
        Debug.Log($"[Conveyor] Lap completed: {shooter} (P7 sẽ đưa vào khay chờ)");
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
