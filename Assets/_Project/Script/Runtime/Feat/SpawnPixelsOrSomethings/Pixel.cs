using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Pool;

public class Pixel : MonoBehaviour
{
    [SerializeField] MeshRenderer meshRenderer;
    Color32 Color;
    ObjectPool<Pixel> PixelPool;
    public void Init(ObjectPool<Pixel> pool) => PixelPool = pool;
    public void Setup(Vector3 pos, Quaternion rotation,float scale, Color32 color)
    {
        transform.position = pos;
        transform.rotation = rotation;
        transform.localScale = Vector3.one * scale;
        Color = color;
        meshRenderer.material.color = color;
    }
    public Color32 GetColor()
    {
        return Color;
    }
    [Button]
    public void Break()
    {
        PixelPool.Release(this);
    }
}
