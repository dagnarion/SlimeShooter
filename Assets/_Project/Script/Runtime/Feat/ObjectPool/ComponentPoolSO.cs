using UnityEngine;
using UnityEngine.Pool;

public abstract class ComponentPoolSO<T> : ScriptableObject where T : Component
{
    [SerializeField] protected T prefab;
    [SerializeField] private int Capacity;
    [SerializeField] private int MaxSize;
    private ObjectPool<T> Pools;
    protected Transform PoolRoot;

    public void InitPool(Transform parent = null)
    {
        PoolRoot = parent;
        Pools = new ObjectPool<T>
        (
            createFunc: CreateInstance,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: false,
            defaultCapacity: Capacity,
            maxSize: MaxSize
        );
    }

    protected virtual T CreateInstance()
    {
        T instance = Instantiate(prefab, PoolRoot);
        return instance;
    }
    protected virtual void OnTakeFromPool(T instance)
    {
        instance.gameObject.SetActive(true);
    }
    protected virtual void OnReturnedToPool(T instance)
    {
        instance.gameObject.SetActive(false);
    }
    protected virtual void OnDestroyPoolObject(T instance)
    {
        if (instance != null)
            Destroy(instance.gameObject);
    }

    public T Get()
    {
        if (Pools == null) InitPool();
        return Pools.Get();
    }
    
    public void Release(T instance)
    {
        Pools?.Release(instance);
    }
    
    public void Clear()
    {
        Pools?.Clear();
        Pools = null;
    }
    
    private void OnDisable()
    {
        Clear();
    }
    
}
