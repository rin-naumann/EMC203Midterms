using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private SpawnSchedule schedule;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float spawnMargin = 2f;

    // Running state. It lives here and not on the schedule, which stays read-only.
    private float[] groupTimers;
    private bool[] eventFired;
    private Enemy winEnemy;

    private void Awake()
    {
        if (schedule == null)
        {
            Debug.LogError("EnemySpawner has no SpawnSchedule assigned.", this);
            enabled = false;
            return;
        }

        if (viewCamera == null) viewCamera = Camera.main;

        groupTimers = new float[schedule.Groups.Length];
        eventFired = new bool[schedule.Events.Length];
    }

    private void OnEnable()
    {
        GameManager.OnRunStarted += ResetRun;
        Enemy.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        GameManager.OnRunStarted -= ResetRun;
        Enemy.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void ResetRun()
    {
        if (groupTimers == null) return;

        System.Array.Clear(groupTimers, 0, groupTimers.Length);
        System.Array.Clear(eventFired, 0, eventFired.Length);
        winEnemy = null;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        float time = GameManager.Instance.ElapsedTime;
        UpdateGroups(time, Time.deltaTime);
        UpdateEvents(time);
    }

    private void UpdateGroups(float time, float deltaTime)
    {
        SpawnGroup[] groups = schedule.Groups;

        for (int i = 0; i < groups.Length; i++)
        {
            SpawnGroup group = groups[i];
            if (!group.IsActive(time)) continue;

            groupTimers[i] -= deltaTime;
            if (groupTimers[i] > 0f) continue;

            // Add the interval instead of setting it, so any overshoot carries over and the rhythm doesn't drift.
            groupTimers[i] += group.Interval;
            SpawnBatch(group, time);
        }
    }

    private void SpawnBatch(SpawnGroup group, float time)
    {
        EnemyData[] types = group.Enemies;
        if (types.Length == 0) return;

        int count = group.GetCount(time);
        float radius = GetSpawnRadius();

        for (int i = 0; i < count; i++)
            SpawnEnemy(types[Random.Range(0, types.Length)], radius);
    }

    private void UpdateEvents(float time)
    {
        SpawnEvent[] events = schedule.Events;

        for (int i = 0; i < events.Length; i++)
        {
            if (eventFired[i] || time < events[i].Time) continue;
            eventFired[i] = true;

            float radius = GetSpawnRadius();
            for (int n = 0; n < events[i].Count; n++)
            {
                Enemy enemy = SpawnEnemy(events[i].Data, radius);
                if (events[i].WinOnDeath) winEnemy = enemy;
            }
        }
    }

    private Enemy SpawnEnemy(EnemyData data, float radius)
    {
        Enemy enemy = ObjectPooler.Instance.EnemyPool.Get();
        enemy.Init(data, GetSpawnPosition(radius));
        return enemy;
    }

    private float GetSpawnRadius()
    {
        float halfHeight = viewCamera.orthographicSize;
        float halfWidth = halfHeight * viewCamera.aspect;
        return Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + spawnMargin;
    }

    private Vector3 GetSpawnPosition(float radius)
    {
        float angle = Random.value * Mathf.PI * 2f;
        Vector3 center = viewCamera.transform.position;
        return new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, 0f);
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        if (enemy != winEnemy) return;

        winEnemy = null;
        GameManager.Instance.ChangeState(GameState.Win);
    }
}