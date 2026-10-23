using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Updates every active enemy in one loop and finds the nearest enemy.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    [SerializeField] private Transform player;
    private readonly List<Enemy> enemies = new List<Enemy>();
    public IReadOnlyList<Enemy> Enemies => enemies;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;
    }

    private void OnEnable() => GameManager.OnRunStarted += ClearAll;
    private void OnDisable() => GameManager.OnRunStarted -= ClearAll;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Register(Enemy enemy) => enemies.Add(enemy);
    public void Unregister(Enemy enemy) => enemies.Remove(enemy);

    private void Update()
    {
        Vector3 playerPosition = player.position;
        float deltaTime = Time.deltaTime;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (i >= enemies.Count) continue;
            enemies[i].FaceTowards(playerPosition.x);
            enemies[i].Tick(deltaTime, playerPosition);
        }
    }

    public Enemy NearestEnemy(Vector3 searchPoint)
    {
        Enemy nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].IsInvulnerable) continue;

            float sqrDistance = (enemies[i].transform.position - searchPoint).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance) { nearest = enemies[i]; nearestSqrDistance = sqrDistance; }
        }

        return nearest;
    }

    private void ClearAll()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
            ObjectPooler.Instance.EnemyPool.Release(enemies[i]);
    }
}