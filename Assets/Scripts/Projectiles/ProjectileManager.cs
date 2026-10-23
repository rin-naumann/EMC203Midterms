using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Updates every active projectile in one loop.
/// Clears them all when a new run starts.
/// </summary>
public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }
    [SerializeField] private Transform player;
    private readonly List<Projectile> projectiles = new List<Projectile>();

    public IReadOnlyList<Projectile> Projectiles => projectiles;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;
    }

    // Unsubscribe so the static event does not keep this object alive
    private void OnEnable() => GameManager.OnRunStarted += ClearAll;
    private void OnDisable() => GameManager.OnRunStarted -= ClearAll;

    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void Register(Projectile projectile) => projectiles.Add(projectile);
    public void Unregister(Projectile projectile) => projectiles.Remove(projectile);

    private void Update()
    {
        Vector3 playerPosition = player.position;
        float deltaTime = Time.deltaTime;

        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            if (i >= projectiles.Count) continue;
            projectiles[i].Tick(deltaTime, playerPosition);
        }
    }

    // New run: clear old projectiles, going backwards because releasing removes them
    private void ClearAll()
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
            ObjectPooler.Instance.ProjectilePool.Release(projectiles[i]);
    }
}