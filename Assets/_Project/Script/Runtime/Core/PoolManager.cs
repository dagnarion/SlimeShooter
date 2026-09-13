using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;
    [SerializeField] Pixel PixelPrefab;
    [SerializeField] Transform PixelParent;
    private ObjectPool<Pixel> PixelPool;
    [SerializeField] SlimeShooter SlimeShooterPrefab;
    [SerializeField] Transform SlimeShooterParent;
    private ObjectPool<SlimeShooter> SlimeShooterPool;
    [SerializeField] SlimeBullet SlimeBulletPrefab;
    [SerializeField] Transform SlimeBulletParent;
    private ObjectPool<SlimeBullet> SlimeBulletPool;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        PixelPool = new ObjectPool<Pixel>
        (
            createFunc: () =>
            {
                Pixel bullet = Instantiate(PixelPrefab, PixelParent);
                bullet.Init(PixelPool);
                return bullet;
            },
            actionOnGet: (Pixel enemyBullet) =>
            {
                enemyBullet.gameObject.SetActive(true);
            },
            actionOnRelease: (Pixel enemyBullet) =>
            {
                enemyBullet.gameObject.SetActive(false);
            },
            actionOnDestroy: (Pixel enemyBullet) =>
            {
                Destroy(enemyBullet.gameObject);
            },
            defaultCapacity: 500,
            maxSize: 1000
        );
        SlimeShooterPool = new ObjectPool<SlimeShooter>
        (
            createFunc: () =>
            {
                SlimeShooter bullet = Instantiate(SlimeShooterPrefab, SlimeShooterParent);
                bullet.Init(SlimeShooterPool);
                return bullet;
            },
            actionOnGet: (SlimeShooter enemyBullet) =>
            {
                enemyBullet.gameObject.SetActive(true);
            },
            actionOnRelease: (SlimeShooter enemyBullet) =>
            {
                enemyBullet.gameObject.SetActive(false);
            },
            actionOnDestroy: (SlimeShooter enemyBullet) =>
            {
                Destroy(enemyBullet.gameObject);
            },
            defaultCapacity: 10,
            maxSize: 100
        );
        SlimeBulletPool = new ObjectPool<SlimeBullet>
        (
            createFunc: () =>
            {
                SlimeBullet bullet = Instantiate(SlimeBulletPrefab, SlimeBulletParent);
                bullet.Init(SlimeBulletPool);
                return bullet;
            },
            actionOnGet: (SlimeBullet enemyBullet) =>
            {
                enemyBullet.gameObject.SetActive(true);
            },
            actionOnRelease: (SlimeBullet enemyBullet) =>
            {
                enemyBullet.gameObject.SetActive(false);
            },
            actionOnDestroy: (SlimeBullet enemyBullet) =>
            {
                Destroy(enemyBullet.gameObject);
            },
            defaultCapacity: 500,
            maxSize: 1000
        );
    }
    public Pixel GetPixel()
    {
        return PixelPool.Get();
    }
    public SlimeShooter GetSlimeShooter()
    {
        return SlimeShooterPool.Get();
    }
    public SlimeBullet GetSlimeBullet()
    {
        return SlimeBulletPool.Get();
    }
}
