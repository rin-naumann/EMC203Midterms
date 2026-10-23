using UnityEngine;

/// <summary>
/// Creates and holds the pools for enemies, projectiles and drops.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [Header("Enemies")]
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private int enemyPrewarmCount = 150;
    [SerializeField] private int enemyPoolMaxSize = 300;

    [Header("Projectiles")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int projectilePrewarmCount = 150;
    [SerializeField] private int projectilePoolMaxSize = 300;

    [Header("Drops")]
    [SerializeField] private DropPickup dropPrefab;
    [SerializeField] private int dropPrewarmCount = 200;
    [SerializeField] private int dropPoolMaxSize = 600;

    public Pool<Enemy> EnemyPool { get; private set; }
    public Pool<Projectile> ProjectilePool { get; private set; }
    public Pool<DropPickup> DropPool { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;

        EnemyPool = new Pool<Enemy>(
            enemyPrefab, 
            CreateParent("Enemy_Pool"), 
            enemyPrewarmCount, 
            enemyPoolMaxSize
        );

        ProjectilePool = new Pool<Projectile>(
            projectilePrefab,
            CreateParent("Projectile_Pool"),
            projectilePrewarmCount,
            projectilePoolMaxSize
        );

        DropPool = new Pool<DropPickup>(
            dropPrefab,
            CreateParent("Drop_Pool"),
            dropPrewarmCount,
            dropPoolMaxSize
        );
    }

    // Makes a parent object to keep the hierarchy tidy
    private Transform CreateParent(string name) {
        Transform parent = new GameObject(name).transform;
        parent.SetParent(transform);
        return parent;
    }
}