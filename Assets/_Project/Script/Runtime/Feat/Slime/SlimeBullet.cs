using System;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody))]
public class SlimeBullet : MonoBehaviour
{
    ObjectPool<SlimeBullet> SlimeBulletPool;
    Rigidbody rb;
    private bool isReleased;
    public Color32 Color;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        isReleased = false;
    }

    public void Init(ObjectPool<SlimeBullet> pool) => SlimeBulletPool = pool;
    public void Setup(Vector3 pos, Quaternion rotation,float speed,Color32 color)
    {
        transform.position = pos;
        transform.rotation = rotation;
        Physics.SyncTransforms();
        rb.linearVelocity = -transform.forward*speed;
        Color = color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pixel"))
        {
            if(other.gameObject.TryGetComponent(out Pixel pixel))
            {
                if (Color.Equals(pixel.GetColor()))
                {
                    pixel.Break();
                    Release();
                }
            }
        }
    }

    private void Release()
    {
        if(isReleased) return;
        isReleased = true;
        SlimeBulletPool.Release(this);
    }
}
