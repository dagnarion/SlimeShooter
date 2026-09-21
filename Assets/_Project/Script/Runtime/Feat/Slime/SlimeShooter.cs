using UnityEngine;
using UnityEngine.Pool;
#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

public class SlimeShooter : MonoBehaviour
{
    [SerializeField] private ComponentPoolSO<SlimeBullet> bulletPool;
    [SerializeField] LayerMask PixelLayer;
    [SerializeField] float RaycastLength;
    [SerializeField] float BulletSpeed;
    private SlimeLocalEvent slimeLocalEvent;
    int Ammo;
    private bool isDead;
    RaycastHit hit;
    Transform previousHit;
    Color32 Color;

    public void Setup(Color32 color, int ammo, SlimeLocalEvent slimeLocalEvent)
    {
        Ammo = ammo;
        Color = color;
        this.slimeLocalEvent = slimeLocalEvent;
        isDead = false;
    }

    private void Update()
    {
        if (Ammo <= 0 && !isDead)
        {
            isDead = true;
            slimeLocalEvent.RaiseDead();
            return;
        }
        if (isDead) return;
        if(transform.rotation == Quaternion.Euler(0,0,0) || transform.rotation == Quaternion.Euler(0,90,0) || transform.rotation == Quaternion.Euler(0,180,0) || transform.rotation == Quaternion.Euler(0, -90, 0))
        {    
            // No rotation checks needed anymore: the direction is always axis-aligned by design.
            if (Physics.Raycast(transform.position, transform.forward, out hit, RaycastLength, PixelLayer) && previousHit != hit.transform)
            {
                previousHit = hit.transform;
                var pixel = hit.transform.GetComponent<Pixel>();

                if (Color.Equals(pixel.GetColor()) && !pixel.HasBeenShot)
                {
                    pixel.SetBeenShot();
                    Shoot(hit.collider.bounds.center);
                }
            }
        }

    }
    void Shoot(Vector3 targetCenter)
    {
        Vector3 dir = transform.forward.normalized;

        // Slide the spawn point sideways into the pixel's lane, keeping the shooter's rotation untouched.
        Vector3 toTarget = targetCenter - transform.position;
        Vector3 lateralOffset = toTarget - Vector3.Project(toTarget, dir);
        Vector3 spawnPos = transform.position + lateralOffset;

        SlimeBullet bullet = bulletPool.Get();
        bullet.Setup(spawnPos, Quaternion.LookRotation(dir), BulletSpeed, Color);
        Ammo--;
        slimeLocalEvent.Raise(Ammo);
    }

}
