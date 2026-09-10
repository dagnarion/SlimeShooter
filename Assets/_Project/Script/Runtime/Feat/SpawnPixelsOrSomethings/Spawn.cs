using UnityEngine;
using UnityEngine.Pool;

public class Spawn : MonoBehaviour
{
    [SerializeField] SpawnDataSO spawnDataSO;
    [SerializeField] float cellSize = 1f;
    Color32[] pixels;
    [SerializeField] Pixel PixelPrefab;
    private ObjectPool<Pixel> PixelPool;

    private void Awake()
    {
        PixelPool = new ObjectPool<Pixel>
        (
            createFunc: () =>
            {
                Pixel bullet = Instantiate(PixelPrefab, transform);
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
    }


    public void SpawnPixels()
    {
        int count = 0;
        pixels = spawnDataSO.SpawnTexture.GetPixels32();
        int height = spawnDataSO.SpawnTexture.height;
        int width = spawnDataSO.SpawnTexture.width;

        float offsetX = (height - 1) * cellSize * 0.5f;
        float offsetZ = (width - 1) * cellSize * 0.5f;

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (pixels[count].a != 0)
                {
                    Pixel pixel = PixelPool.Get();
                    Vector3 pos = new Vector3(
                        transform.position.z + j * cellSize - offsetZ,
                        0,
                        transform.position.x + i * cellSize - offsetX
                    );
                    pixel.Setup(pos, Quaternion.identity,cellSize, pixels[count]);
                }
                count++;
            }
        }
    }
}