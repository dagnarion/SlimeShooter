using DG.Tweening;
using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>
/// Shooter nào chuyển sang Dead (hết đạn trên băng, bị ColorBomb ở cột/khay...) thì thu nhỏ rồi trả về pool.
/// Gom một chỗ để mọi nơi loại shooter đều có cùng hiệu ứng.
/// </summary>
public class ShooterDespawnView : MonoBehaviour
{
    [SerializeField] private ShooterViewPoolSO shooterPool;
    [SerializeField] private float despawnDuration = 0.25f;

    [Inject] private ShooterColumns _columns;
    [Inject] private ShooterViewRegistry _registry;

    private void Start()
    {
        foreach (var shooter in _columns.AllShooters)
        {
            var captured = shooter;
            shooter.State
                .Where(state => state == ShooterState.Dead)
                .Take(1)
                .Subscribe(_ => Despawn(captured))
                .AddTo(this);
        }
    }

    private void Despawn(ShooterModel shooter)
    {
        if (!_registry.TryGet(shooter, out var view)) return;
        _registry.Unregister(shooter);
        view.StopMotion();
        view.transform.DOScale(Vector3.zero, despawnDuration).SetEase(Ease.InBack).SetLink(view.gameObject)
            .OnComplete(() =>
            {
                view.transform.localScale = Vector3.one;
                if (shooterPool != null) shooterPool.Release(view);
                else view.gameObject.SetActive(false);
            });
    }
}
