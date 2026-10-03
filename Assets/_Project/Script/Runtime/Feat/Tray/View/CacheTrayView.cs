using System.Collections.Generic;
using DG.Tweening;
using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>Hàng ô khay chờ ngay dưới băng chuyền. Cảnh báo đỏ khi còn ≤ 1 ô trống.</summary>
public class CacheTrayView : MonoBehaviour
{
    [SerializeField] private Material slotMaterial;
    [SerializeField] private float slotSpacing = 1.5f;
    [SerializeField] private Vector3 slotSize = new Vector3(1.25f, 0.06f, 1.25f);
    [Tooltip("Khoảng cách từ mép dưới băng chuyền tới hàng khay.")]
    [SerializeField] private float offsetBelowBelt = 1.6f;
    [SerializeField] private Color slotColor = new Color(0.18f, 0.2f, 0.28f);
    [SerializeField] private Color warningColor = new Color(0.75f, 0.2f, 0.2f);
    [SerializeField] private float jumpDuration = 0.35f;
    [SerializeField] private float shiftDuration = 0.2f;

    [Inject] private CacheTray _tray;
    [Inject] private IConveyorPath _path;
    [Inject] private ShooterViewRegistry _registry;

    private readonly List<Transform> _slots = new List<Transform>();
    private readonly Dictionary<ShooterModel, int> _slotOf = new Dictionary<ShooterModel, int>();
    private Material _runtimeSlotMaterial;
    private Tween _warningTween;

    private void Awake()
    {
        var bounds = _path.Bounds;
        transform.position = new Vector3(bounds.center.x, transform.position.y, bounds.min.z - offsetBelowBelt);
    }

    private void Start()
    {
        _runtimeSlotMaterial = slotMaterial != null ? new Material(slotMaterial) : null;
        _tray.Capacity.Subscribe(BuildSlots).AddTo(this);
        _tray.OnChanged.Subscribe(_ => Relayout()).AddTo(this);
        _tray.FreeCount.Subscribe(UpdateWarning).AddTo(this);
    }

    public Vector3 SlotPosition(int index)
    {
        int count = _tray.Capacity.CurrentValue;
        return transform.position + Vector3.right * ((index - (count - 1) * 0.5f) * slotSpacing);
    }

    private void BuildSlots(int capacity)
    {
        while (_slots.Count < capacity)
        {
            var slot = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // đế tròn (placeholder)
            slot.name = $"TraySlot_{_slots.Count}";
            Destroy(slot.GetComponent<Collider>());
            slot.transform.SetParent(transform, false);
            slot.transform.localScale = slotSize;
            if (_runtimeSlotMaterial != null) slot.GetComponent<MeshRenderer>().sharedMaterial = _runtimeSlotMaterial;
            _slots.Add(slot.transform);
        }
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].gameObject.SetActive(i < capacity);
            _slots[i].position = SlotPosition(i) + Vector3.down * 0.3f;
        }
        Relayout();
    }

    private void Relayout()
    {
        var shooters = _tray.Shooters;
        var stillInTray = new HashSet<ShooterModel>(shooters);
        foreach (var key in new List<ShooterModel>(_slotOf.Keys))
        {
            if (!stillInTray.Contains(key)) _slotOf.Remove(key);
        }

        for (int i = 0; i < shooters.Count; i++)
        {
            var shooter = shooters[i];
            if (!_registry.TryGet(shooter, out var view)) continue;

            bool isNew = !_slotOf.TryGetValue(shooter, out int previous);
            if (!isNew && previous == i) continue;
            _slotOf[shooter] = i;

            if (isNew)
            {
                view.JumpTo(SlotPosition(i), jumpDuration);
                view.Face(Vector3.forward, jumpDuration);
            }
            else
            {
                view.MoveTo(SlotPosition(i), shiftDuration);
            }
        }
    }

    private void UpdateWarning(int free)
    {
        if (_runtimeSlotMaterial == null) return;
        _warningTween?.Kill();
        if (free <= 1)
        {
            _runtimeSlotMaterial.color = slotColor;
            _warningTween = _runtimeSlotMaterial.DOColor(warningColor, 0.4f).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }
        else
        {
            _runtimeSlotMaterial.color = slotColor;
        }
    }

    private void OnDestroy()
    {
        _warningTween?.Kill();
        if (_runtimeSlotMaterial != null) Destroy(_runtimeSlotMaterial);
    }
}
