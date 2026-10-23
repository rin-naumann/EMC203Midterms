using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Owns every drop: creates them where an enemy dies (from that enemy's drop table), updates all of
/// them in one loop (the same pattern as ProjectileManager), and clears them when a new run starts.
/// </summary>
public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; private set; }

    [Tooltip("Drops from one enemy are scattered up to this far from where it died, so they don't sit on top of each other.")]
    [SerializeField] private float scatter = 0.3f;

    private readonly List<DropPickup> drops = new List<DropPickup>();
    public IReadOnlyList<DropPickup> Drops => drops;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;
    }

    // Static events outlive this object, so always unsubscribe in OnDisable.
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

        // The collect radius is the player's body radius. The magnet radius can never be smaller than it,
        // or a drop inside the body would never start flying.
        float collectRadius = player.HitRadius;
        float magnetRadius = Mathf.Max(player.PickupRadius, collectRadius);

        for (int i = drops.Count - 1; i >= 0; i--)
        {
            if (i >= drops.Count) continue;
            drops[i].Tick(deltaTime, playerPosition, magnetRadius, collectRadius);
        }
    }

    // Enemy only announces its death and knows nothing about drops; this is the one place that connects the two.
    // Each entry in the enemy's drop table rolls its own chance. An empty table (the slime) drops nothing.
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

    // New run: clear the old run's drops. Releasing unregisters each one, so go backwards.
    private void ClearAll()
    {
        for (int i = drops.Count - 1; i >= 0; i--)
            ObjectPooler.Instance.DropPool.Release(drops[i]);
    }
}