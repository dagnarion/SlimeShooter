using UnityEngine;
using UnityEngine.Pool;

public class PixelSpawn : MonoBehaviour
{
    [SerializeField] SpawnDataSO spawnDataSO;
    Color32[] pixels;


    private void Start()
    {
        SpawnPixels();
    }

    public void SpawnPixels()
    {
        int count = 0;
        pixels = spawnDataSO.SpawnTexture.GetPixels32();
        int height = spawnDataSO.SpawnTexture.height;
        int width = spawnDataSO.SpawnTexture.width;

        float offsetX = (width - 1) * spawnDataSO.cellSize * 0.5f;
        float offsetZ = (height - 1) * spawnDataSO.cellSize * 0.5f;

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (pixels[count].a != 0)
                {
                    Pixel pixel = PoolManager.Instance.GetPixel();
                    Vector3 pos = new Vector3(
                        transform.position.x + j * spawnDataSO.cellSize - offsetX,
                        transform.position.y,
                        transform.position.z + i * spawnDataSO.cellSize - offsetZ
                    );
                    pixel.Setup(pos, Quaternion.identity, spawnDataSO.cellSize, pixels[count]);
                }
                count++;
            }
        }
    }
}