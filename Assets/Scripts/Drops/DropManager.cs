using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Creates drops where enemies die and updates them all in one loop.
/// Clears them when a new run starts.
/// </summary>
public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; private set; }

    [SerializeField] private float scatter = 0.3f;

    private readonly List<DropPickup> drops = new List<DropPickup>();
    public IReadOnlyList<DropPickup> Drops => drops;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;
    }

    // Unsubscribe so the static events do not keep this object alive
    private void OnEnable()
    {
        GameManager.OnRunStarted += ClearAll;
        Enemy.OnEnemyDeath += DropLoot;
    }

    private void OnDisable()
    {
        GameManager.OnRunStarted -= ClearAll;
        Enemy.OnEnemyDeath -= DropLoot;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Register(DropPickup drop) => drops.Add(drop);
    public void Unregister(DropPickup drop) => drops.Remove(drop);

    private void Update()
    {
        Player player = Player.Instance;
        if (player == null) return;

        Vector3 playerPosition = player.Position;
        float deltaTime = Time.deltaTime;

        // The magnet radius is never smaller than the collect radius
        float collectRadius = player.HitRadius;
        float magnetRadius = Mathf.Max(player.PickupRadius, collectRadius);

        for (int i = drops.Count - 1; i >= 0; i--)
        {
            if (i >= drops.Count) continue;
            drops[i].Tick(deltaTime, playerPosition, magnetRadius, collectRadius);
        }
    }

    // Rolls each entry in the enemy's drop table when it dies
    private void DropLoot(Enemy enemy)
    {
        DropEntry[] table = enemy.Data.Drops;
        if (table == null) return;

        for (int i = 0; i < table.Length; i++)
        {
            DropEntry entry = table[i];
            if (entry.Drop == null) continue;
            if (entry.Chance < 1f && Random.value >= entry.Chance) continue;

            Vector3 position = enemy.transform.position + (Vector3)(Random.insideUnitCircle * scatter);
            DropPickup pickup = ObjectPooler.Instance.DropPool.Get();
            pickup.Init(entry.Drop, position);
        }
    }

    // New run: clear old drops, going backwards because releasing removes them
    private void ClearAll()
    {
        for (int i = drops.Count - 1; i >= 0; i--)
            ObjectPooler.Instance.DropPool.Release(drops[i]);
    }
}