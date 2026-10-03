using System;
using DG.Tweening;
using UnityEngine;

/// <summary>Đạn hiệu ứng: bay từ nòng tới tâm ô trong đúng thời gian cho trước. Không có physics.</summary>
public class BulletFx : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float normalScale = 0.25f;
    [SerializeField] private float rushScale = 0.4f;

    private Tween _tween;

    public void Launch(Vector3 from, Vector3 to, float duration, Material material, bool isRush, Action onArrived)
    {
        _tween?.Kill();
        transform.position = from;
        transform.localScale = Vector3.one * (isRush ? rushScale : normalScale);
        meshRenderer.sharedMaterial = material;

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = isRush;
            trail.sharedMaterial = material;
        }

        _tween = transform.DOMove(to, Mathf.Max(0.01f, duration))
            .SetEase(isRush ? Ease.InQuad : Ease.Linear)
            .SetLink(gameObject)
            .OnComplete(() => onArrived?.Invoke());
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _tween = null;
    }
}
