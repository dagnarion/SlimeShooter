using System.Collections.Generic;
using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>
/// Đặt shooter theo cột phía dưới board. Hàng 0 (đứng đầu) gần board nhất, các hàng sau lùi về -Z.
/// </summary>
public class ShooterColumnsView : MonoBehaviour
{
    [SerializeField] private ShooterViewPoolSO shooterPool;
    [SerializeField] private Transform shooterRoot;
    [Tooltip("CellSize + CellGap quyết định khoảng cách giữa các cột (x) và các hàng (y -> trục Z).")]
    [SerializeField] private GridDataSO layoutData;
    [Min(0f)] [SerializeField] private float shiftDuration = 0.25f;

    [Inject] private ShooterColumns _columns;
    [Inject] private ShooterPickService _pickService;
    [Inject] private PaletteMaterialCache _materials;

    private readonly Dictionary<ShooterModel, ShooterView> _views = new Dictionary<ShooterModel, ShooterView>();

    public ShooterViewPoolSO Pool => shooterPool;

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
                if (_views.TryGetValue(shooter, out var view)) view.PlayRejectFeedback();
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
                _views[shooter] = view;
            }
        }
    }

    /// <summary>Tách view khỏi cột để hệ thống khác (băng chuyền) điều khiển tiếp.</summary>
    public bool TryDetach(ShooterModel shooter, out ShooterView view)
    {
        if (!_views.TryGetValue(shooter, out view)) return false;
        _views.Remove(shooter);
        return true;
    }

    public Vector3 SlotPosition(int column, int row)
    {
        Vector2 step = layoutData != null ? layoutData.CellSize + layoutData.CellGap : Vector2.one * 1.2f;
        float x = (column - (_columns.ColumnCount - 1) * 0.5f) * step.x;
        return transform.position + new Vector3(x, 0f, -row * step.y);
    }

    private void Relayout(int column)
    {
        var shooters = _columns.GetColumn(column);
        for (int r = 0; r < shooters.Count; r++)
        {
            if (_views.TryGetValue(shooters[r], out var view)) view.MoveTo(SlotPosition(column, r), shiftDuration);
        }
    }
}
