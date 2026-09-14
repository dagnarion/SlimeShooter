using System;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody))]
public class SlimeBullet : MonoBehaviour
{
    [SerializeField] private ComponentPoolSO<SlimeBullet> bulletPool;
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
    
    public void Setup(Vector3 pos, Quaternion rotation,float speed,Color32 color)
    {
        transform.position = pos;
        transform.rotation = rotation;
        Physics.SyncTransforms();
        rb.linearVelocity = transform.forward*speed;
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
        bulletPool.Release(this);
    }
}
