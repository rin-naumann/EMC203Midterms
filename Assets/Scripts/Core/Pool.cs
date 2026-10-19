using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

// All pooled objects must implement this
public class Pool<T> where T : Component, IPoolable
{
    private readonly T prefab;
    private readonly ObjectPool<T> pool;
    private readonly Transform parent;

    public Pool(T prefab, Transform parent, int preWarm, int maxSize)
    {
        this.prefab = prefab;
        this.parent = parent;
        
        pool = new ObjectPool<T>(
            createFunc: Create,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroy,
            collectionCheck: false,
            defaultCapacity: preWarm,
            maxSize: maxSize
        );

        Prewarm(Mathf.Min(preWarm, maxSize));
    }

    public T Get() => pool.Get();

    public void Release (T item)
    {
        if (!item.gameObject.activeSelf) return;
        pool.Release(item);
    }

    private void Prewarm(int count)
    {
        List<T> items = new List<T>(count);
        for (int i = 0; i < count; i++) items.Add(Create());
        for (int i = 0; i < items.Count; i++) pool.Release(items[i]);
    }

    private T Create() => Object.Instantiate(prefab, parent);
    private void OnGet(T item) { item.gameObject.SetActive(true); item.OnSpawn(); }
    private void OnRelease(T item) { item.OnDespawn(); item.gameObject.SetActive(false); }
    private void OnDestroy(T item) => Object.Destroy(item.gameObject);
}
