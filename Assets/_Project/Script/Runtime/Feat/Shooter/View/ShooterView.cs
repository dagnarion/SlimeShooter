using System;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;

/// <summary>Hiển thị một shooter. Collider trên layer Shooter để nhận click.</summary>
public class ShooterView : MonoBehaviour
{
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Transform visual;

    private IDisposable _binding;
    private Tween _moveTween;
    private Tween _feedbackTween;

    public ShooterModel Model { get; private set; }

    public void Bind(ShooterModel model, Material material)
    {
        Unbind();
        Model = model;
        bodyRenderer.sharedMaterial = material;
        _binding = model.Ammo.Subscribe(ammo => ammoText.text = ammo.ToString());
    }

    public void Unbind()
    {
        _binding?.Dispose();
        _binding = null;
        Model = null;
    }

    public Tween MoveTo(Vector3 position, float duration, Ease ease = Ease.OutQuad)
    {
        _moveTween?.Kill();
        _moveTween = transform.DOMove(position, duration).SetEase(ease).SetLink(gameObject);
        return _moveTween;
    }

    public Tween JumpTo(Vector3 position, float duration, float jumpPower = 1.2f)
    {
        _moveTween?.Kill();
        _moveTween = transform.DOJump(position, jumpPower, 1, duration).SetEase(Ease.OutQuad).SetLink(gameObject);
        return _moveTween;
    }

    /// <summary>Lắc nhẹ khi bấm vào shooter không chọn được.</summary>
    public void PlayRejectFeedback()
    {
        if (_feedbackTween != null && _feedbackTween.IsActive()) return;
        Transform target = visual != null ? visual : transform;
        _feedbackTween = target.DOPunchRotation(new Vector3(0f, 0f, 15f), 0.25f, 10, 0.5f).SetLink(gameObject);
    }

    private void OnDisable()
    {
        _moveTween?.Kill();
        _feedbackTween?.Kill(true);
        Unbind();
    }
}
