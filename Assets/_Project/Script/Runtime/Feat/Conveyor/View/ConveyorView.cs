using DG.Tweening;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

/// <summary>
/// Dựng máng nước theo đường băng chuyền, đặt shooter theo vị trí trên băng,
/// bộ đếm slot "x/N" ở cửa vào, và nháy đỏ khi hết slot.
/// </summary>
public class ConveyorView : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("Máng nước (placeholder)")]
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private float channelWidth = 1.3f;
    [SerializeField] private float floorY = -0.25f;
    [SerializeField] private float wallHeight = 0.45f;
    [SerializeField] private float wallThickness = 0.18f;
    [SerializeField] private Color rejectColor = new Color(0.95f, 0.25f, 0.25f);

    [Header("Entrance")]
    [SerializeField] private TMP_Text slotCounter;
    [SerializeField] private Vector3 counterOffset = new Vector3(-1.2f, 0.1f, -1.2f);

    [Inject] private ConveyorModel _conveyor;
    [Inject] private ConveyorConfigSO _config;
    [Inject] private ShooterViewRegistry _registry;

    private Material _waterInstance;
    private Color _waterBaseColor;
    private Tween _rejectTween;

    private void Start()
    {
        BuildChannel();
        PlaceCounter();

        _conveyor.Available
            .CombineLatest(_conveyor.Capacity, (available, capacity) => $"{available}/{capacity}")
            .Subscribe(text => { if (slotCounter != null) slotCounter.text = text; })
            .AddTo(this);

        _conveyor.OnJumpStarted.Subscribe(OnJumpStarted).AddTo(this);
        _conveyor.OnAttached.Subscribe(unit => { if (_registry.TryGet(unit.Shooter, out var v)) v.StopMotion(); }).AddTo(this);
        _conveyor.OnInsertRejected.Subscribe(_ => PlayRejectFeedback()).AddTo(this);
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

    private void BuildChannel()
    {
        var channel = new GameObject("Channel");
        channel.transform.SetParent(transform, false);
        channel.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        channel.AddComponent<MeshFilter>().sharedMesh =
            ProceduralMeshes.Channel(_conveyor.Path, channelWidth, floorY, wallHeight, wallThickness);

        var renderer = channel.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _waterInstance = waterMaterial != null ? new Material(waterMaterial) : null;
        if (_waterInstance != null && _waterInstance.HasProperty(BaseColorId)) _waterBaseColor = _waterInstance.GetColor(BaseColorId);
        renderer.sharedMaterials = new[] { _waterInstance, wallMaterial };
    }

    private void PlaceCounter()
    {
        if (slotCounter == null) return;
        Vector3 entrance = _conveyor.Path.Evaluate(0f, out _);
        slotCounter.transform.position = entrance + counterOffset;
    }

    private void PlayRejectFeedback()
    {
        if (slotCounter != null) slotCounter.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 8).SetLink(slotCounter.gameObject);
        if (_waterInstance == null || !_waterInstance.HasProperty(BaseColorId)) return;

        _rejectTween?.Kill();
        _rejectTween = DOVirtual.Float(0f, 1f, 0.35f, t => _waterInstance.SetColor(BaseColorId, Color.Lerp(rejectColor, _waterBaseColor, t)))
            .SetLink(gameObject);
    }

    private void OnDestroy()
    {
        _rejectTween?.Kill();
        if (_waterInstance != null) Destroy(_waterInstance);
    }
}
