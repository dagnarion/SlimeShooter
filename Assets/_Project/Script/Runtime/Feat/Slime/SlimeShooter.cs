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
    
    public void Setup(Color32 color, int ammo,SlimeLocalEvent slimeLocalEvent)
    {
        Ammo = ammo;
        Color = color;
        this.slimeLocalEvent = slimeLocalEvent;
        isDead = false;
    }
    
    private void Update()
    {
        if(Ammo <= 0 && !isDead)
        {
            isDead = true;
            slimeLocalEvent.RaiseDead();
            return;
        }
        if(isDead) return;
        
        if(transform.rotation == Quaternion.Euler(0,0,0) || transform.rotation == Quaternion.Euler(0,90,0) || transform.rotation == Quaternion.Euler(0,180,0) || transform.rotation == Quaternion.Euler(0, -90, 0))
        {
            Physics.Raycast(transform.position,transform.forward,out hit,RaycastLength,PixelLayer);
            if (hit.transform != null && previousHit != hit.transform)
            {
                previousHit = hit.transform;
                var pixels = hit.transform.GetComponent<Pixel>();
                var pixelColor = pixels.GetColor();
                if (Color.Equals(pixelColor) && !pixels.HasBeenShot)
                {
                    pixels.SetBeenShot();
                    Shoot();
                }
            }    
        }
    }
    void Shoot()
    {
        SlimeBullet bullet = bulletPool.Get();
        bullet.Setup(transform.position,transform.rotation,BulletSpeed,Color);
        Ammo--;
        slimeLocalEvent.Raise(Ammo);
    }
    
}
