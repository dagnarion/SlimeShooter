using UnityEngine;
using UnityEngine.Pool;
#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

public class SlimeShooter : MonoBehaviour
{
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] float RaycastLength;
    [SerializeField] float BulletSpeed;
    [SerializeField] LayerMask PixelLayer;
    int Ammo;
    RaycastHit hit;
    Transform previousHit;
    [SerializeField]Color32 Color;

    ObjectPool<SlimeShooter> SlimeShooterPool;
    public void Init(ObjectPool<SlimeShooter> pool) => SlimeShooterPool = pool;
    public void Setup(Color32 color, int ammo)
    {
        Ammo = ammo;
        Color = color;
    }
    private void Update()
    {
        if(transform.rotation == Quaternion.Euler(0,0,0) || transform.rotation == Quaternion.Euler(0,90,0) || transform.rotation == Quaternion.Euler(0,180,0) || transform.rotation == Quaternion.Euler(0, -90, 0))
        {
            Physics.Raycast(transform.position,-transform.forward,out hit,RaycastLength,PixelLayer);
            if (hit.transform != null && previousHit != hit.transform)
            {
                previousHit = hit.transform;
                var pixels = hit.transform.GetComponent<Pixel>();
                var pixelColor = pixels.GetColor();
                if (Color.Equals(pixelColor) && !pixels.HasBeenShot)
                {
                    pixels.HasBeenShot = true;
                    Shoot();
                }
            }    
        }
    }
    void Shoot()
    {
        SlimeBullet bullet = PoolManager.Instance.GetSlimeBullet();
        bullet.Setup(transform.position,transform.rotation,BulletSpeed,Color);
    }
    public void Dead()
    {
        SlimeShooterPool.Release(this);
    }

}
