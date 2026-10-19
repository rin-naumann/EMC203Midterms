using UnityEngine;

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

    public Pool<Enemy> EnemyPool { get; private set; }
    public Pool<Projectile> ProjectilePool { get; private set; }

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
    }

    // UTIL: Creates a child of the ObjectPooler and returns it as the pool's parent. For scene hierarchy organization.
    private Transform CreateParent(string name) {
        Transform parent = new GameObject(name).transform;
        parent.SetParent(transform);
        return parent;
    }
}