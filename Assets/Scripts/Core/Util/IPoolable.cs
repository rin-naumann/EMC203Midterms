/// <summary>
/// Implemented by objects that live in a pool.
/// OnSpawn runs when taken from the pool, OnDespawn when returned.
/// </summary>
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}
