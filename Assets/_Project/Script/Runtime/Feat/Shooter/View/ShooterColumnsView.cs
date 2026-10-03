using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>
/// Đặt shooter theo cột phía dưới băng chuyền. Hàng 0 (đứng đầu) gần băng nhất, các hàng sau lùi về -Z.
/// </summary>
public class ShooterColumnsView : MonoBehaviour
{
    [SerializeField] private ShooterViewPoolSO shooterPool;
    [SerializeField] private Transform shooterRoot;
    [Tooltip("CellSize + CellGap quyết định khoảng cách giữa các cột (x) và các hàng (y -> trục Z).")]
    [SerializeField] private GridDataSO layoutData;
    [Tooltip("Khoảng cách từ mép dưới băng chuyền tới hàng đầu (chừa chỗ cho khay chờ).")]
    [Min(0f)] [SerializeField] private float offsetBelowBelt = 3.4f;
    [Min(0f)] [SerializeField] private float shiftDuration = 0.25f;

    [Inject] private ShooterColumns _columns;
    [Inject] private ShooterPickService _pickService;
    [Inject] private PaletteMaterialCache _materials;
    [Inject] private ShooterViewRegistry _registry;
    [Inject] private IConveyorPath _path;

    public ShooterViewPoolSO Pool => shooterPool;

    private void Awake()
    {
        // Đặt anchor trước Start để camera framer đọc được bounds.
        var bounds = _path.Bounds;
        transform.position = new Vector3(bounds.center.x, transform.position.y, bounds.min.z - offsetBelowBelt);
    }

    private void Start()
    {
        shooterPool.InitPool(shooterRoot != null ? shooterRoot : transform);
        Build();

        _columns.OnColumnChanged
            .Subscribe(Relayout)
            .AddTo(this);

        _pickService.OnPickRejected
            .Subscribe(shooter =>
            {
                if (_registry.TryGet(shooter, out var view)) view.PlayRejectFeedback();
            })
            .AddTo(this);
    }

    private void Build()
    {
        for (int c = 0; c < _columns.ColumnCount; c++)
        {
            var column = _columns.GetColumn(c);
            for (int r = 0; r < column.Count; r++)
            {
                var shooter = column[r];
                ShooterView view = shooterPool.Get();
                view.Bind(shooter, _materials.Get(shooter.ColorId));
                view.transform.SetPositionAndRotation(SlotPosition(c, r), Quaternion.identity);
                _registry.Register(shooter, view);
            }
        }
    }

    public Vector3 SlotPosition(int column, int row)
    {
        Vector2 step = Step;
        float x = (column - (_columns.ColumnCount - 1) * 0.5f) * step.x;
        return transform.position + new Vector3(x, 0f, -row * step.y);
    }

    /// <summary>Vùng chiếm bởi các cột (dùng để căn camera).</summary>
    public Bounds GetBounds(int visibleRows = 3)
    {
        Vector2 step = Step;
        float width = Mathf.Max(1, _columns.ColumnCount) * step.x;
        float depth = Mathf.Max(1, visibleRows) * step.y;
        var center = transform.position + new Vector3(0f, 0f, -(depth - step.y) * 0.5f);
        return new Bounds(center, new Vector3(width, 0f, depth));
    }

    private Vector2 Step => layoutData != null ? layoutData.CellSize + layoutData.CellGap : Vector2.one * 1.2f;

    private void Relayout(int column)
    {
        var shooters = _columns.GetColumn(column);
        for (int r = 0; r < shooters.Count; r++)
        {
            if (_registry.TryGet(shooters[r], out var view)) view.MoveTo(SlotPosition(column, r), shiftDuration);
        }
    }
}
