using System;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;

/// <summary>Hiển thị một shooter. Collider trên layer Shooter để nhận click.</summary>
public class ShooterView : MonoBehaviour
{
    [Tooltip("Các phần được tô màu theo palette (thân, nòng...).")]
    [SerializeField] private Renderer[] colorRenderers;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Transform visual;

    private IDisposable _binding;
    private Tween _moveTween;
    private Tween _feedbackTween;
    private Tween _faceTween;
    private Tween _recoilTween;
    private Quaternion _visualBaseRotation = Quaternion.identity;
    private Vector3 _visualBaseScale = Vector3.one;

    public ShooterModel Model { get; private set; }

    private void Awake()
    {
        if (visual != null)
        {
            _visualBaseRotation = visual.localRotation;
            _visualBaseScale = visual.localScale;
        }
    }

    /// <summary>Giật nhẹ khi bắn. Hoàn tất cú giật trước để scale không bị cộng dồn khi bắn liên tục (rush).</summary>
    public void PlayRecoil()
    {
        if (visual == null) return;
        _recoilTween?.Complete();
        visual.localScale = _visualBaseScale;
        _recoilTween = visual.DOPunchScale(_visualBaseScale * 0.12f, 0.12f, 4, 0.5f).SetLink(gameObject);
    }

    /// <summary>Quay phần thân về hướng <paramref name="direction"/> (text ammo giữ nguyên để luôn đọc được).</summary>
    public void Face(Vector3 direction, float duration = 0f)
    {
        if (visual == null || direction.sqrMagnitude < 1e-6f) return;
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up) * _visualBaseRotation;
        _faceTween?.Kill();
        if (duration <= 0f) visual.rotation = target;
        else _faceTween = visual.DORotateQuaternion(target, duration).SetLink(gameObject);
    }

    public void Bind(ShooterModel model, Material material)
    {
        Unbind();
        Model = model;
        foreach (var renderer in colorRenderers)
        {
            if (renderer != null) renderer.sharedMaterial = material;
        }
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

    /// <summary>Dừng tween di chuyển (khi băng chuyền bắt đầu điều khiển vị trí).</summary>
    public void StopMotion()
    {
        _moveTween?.Kill();
        _moveTween = null;
    }

    public bool IsMoving => _moveTween != null && _moveTween.IsActive() && _moveTween.IsPlaying();

    /// <summary>Lắc nhẹ khi bấm vào shooter không chọn được.</summary>
    public void PlayRejectFeedback()
    {
        if (_feedbackTween != null && _feedbackTween.IsActive()) return;
        Transform target = visual != null ? visual : transform;
        _feedbackTween = target.DOPunchRotation(new Vector3(0f, 0f, 15f), 0.25f, 10, 0.5f).SetLink(gameObject);
    }

    private void OnDisable()
    {
        _faceTween?.Kill();
        _recoilTween?.Kill();
        if (visual != null) visual.localScale = _visualBaseScale;
        _moveTween?.Kill();
        _feedbackTween?.Kill(true);
        Unbind();
    }
}
