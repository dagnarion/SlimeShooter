using System;
using DG.Tweening;
using UnityEngine;

/// <summary>Hiển thị một ô pixel. Không có collider — logic bắn nằm trong PixelBoard.</summary>
public class PixelView : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Transform visual;
    [Range(0.5f, 1f)] [SerializeField] private float fillRatio = 0.92f;

    private Sequence _breakSequence;
    private Vector3 _baseScale;

    public Vector2Int Cell { get; private set; }

    public void Setup(Vector2Int cell, Vector3 position, float cellSize, Material material)
    {
        Cell = cell;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        _baseScale = Vector3.one * (cellSize * fillRatio);
        Visual.localScale = _baseScale;
        if (Visual != transform) Visual.localPosition = Vector3.zero;
        meshRenderer.sharedMaterial = material;
    }

    /// <summary>Chờ đạn bay tới (<paramref name="delay"/>) rồi phát animation vỡ trong <paramref name="duration"/>.</summary>
    public void PlayBreak(float delay, float duration, Action onComplete)
    {
        _breakSequence?.Kill();
        _breakSequence = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(Visual.DOPunchScale(_baseScale * 0.25f, duration * 0.4f, 6, 0.5f))
            .Append(Visual.DOScale(Vector3.zero, duration * 0.6f).SetEase(Ease.InBack))
            .Join(Visual.DOLocalMoveY(_baseScale.y * 0.5f, duration * 0.6f))
            .SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    private Transform Visual => visual != null ? visual : transform;

    private void OnDisable()
    {
        _breakSequence?.Kill();
        _breakSequence = null;
    }
}
