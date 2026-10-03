using System.Collections.Generic;
using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>Dựng pixel từ PixelBoard và phát animation vỡ khi ô chuyển sang Breaking.</summary>
public class BoardView : MonoBehaviour
{
    [SerializeField] private PixelViewPoolSO pixelPool;
    [SerializeField] private Transform pixelRoot;

    [Inject] private PixelBoard _board;
    [Inject] private BoardLayout _layout;
    [Inject] private BoardConfigSO _config;
    [Inject] private PaletteMaterialCache _materials;

    private readonly Dictionary<Vector2Int, PixelView> _views = new Dictionary<Vector2Int, PixelView>();

    private void Start()
    {
        Build();

        _board.OnCellBreaking
            .Subscribe(PlayBreak)
            .AddTo(this);
    }

    private void Build()
    {
        Transform root = pixelRoot != null ? pixelRoot : transform;
        pixelPool.InitPool(root);
        pixelPool.Prewarm(_board.TotalCount);

        for (int y = 0; y < _board.Height; y++)
        for (int x = 0; x < _board.Width; x++)
        {
            var cell = new Vector2Int(x, y);
            if (_board.StateAt(cell) != CellState.Alive) continue;

            PixelView view = pixelPool.Get();
            view.Setup(cell, _layout.CellToWorld(cell), _config.CellSize, _materials.Get(_board.ColorAt(cell)));
            _views[cell] = view;
        }
    }

    private void PlayBreak(Vector2Int cell)
    {
        if (!_views.TryGetValue(cell, out var view)) return;
        _views.Remove(cell);
        view.PlayBreak(_config.BulletTravelTime, _config.BreakAnimDuration, () => pixelPool.Release(view));
    }

    private void OnDrawGizmosSelected()
    {
        if (_layout == null) return;
        Gizmos.color = Color.cyan;
        var bounds = _layout.WorldBounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
