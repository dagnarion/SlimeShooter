using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Pool;

public class Pixel : MonoBehaviour
{
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] private ComponentPoolSO<Pixel> PixelPool;
    Color32 Color;
    public bool HasBeenShot = false;

    public void Setup(Vector3 pos, Quaternion rotation,float scale, Color32 color)
    {
        transform.position = pos;
        transform.rotation = rotation;
        transform.localScale = Vector3.one * scale;
        Color = color;
        meshRenderer.material.color = color;
    }
    private void OnEnable()
    {
        HasBeenShot = false;
    }
    public void SetBeenShot()
    {
        if (HasBeenShot)
        {
            return;
        }
        HasBeenShot = true;
        DOVirtual.DelayedCall(0.5f,() => HasBeenShot = false);
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
