using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>Phát đạn hiệu ứng cho mỗi ShotEvent và giật nhẹ shooter.</summary>
public class ShotView : MonoBehaviour
{
    [SerializeField] private BulletFxPoolSO bulletPool;
    [SerializeField] private Transform bulletRoot;
    [SerializeField] private Vector3 muzzleOffset = new Vector3(0f, 0.4f, 0f);

    [Inject] private ShootingSystem _shooting;
    [Inject] private BoardLayout _layout;
    [Inject] private BoardConfigSO _boardConfig;
    [Inject] private ShooterViewRegistry _registry;
    [Inject] private PaletteMaterialCache _materials;

    private void Start()
    {
        bulletPool.InitPool(bulletRoot != null ? bulletRoot : transform);
        _shooting.OnShot.Subscribe(OnShot).AddTo(this);
    }

    private void OnShot(ShotEvent shot)
    {
        if (!_registry.TryGet(shot.Shooter, out var shooterView)) return;

        Vector3 from = shooterView.transform.position + muzzleOffset;
        Vector3 to = _layout.CellToWorld(shot.Cell);
        BulletFx bullet = bulletPool.Get();
        bullet.Launch(from, to, _boardConfig.BulletTravelTime, _materials.Get(shot.Shooter.ColorId), shot.IsRush,
            () => bulletPool.Release(bullet));

        shooterView.PlayRecoil();
    }
}
