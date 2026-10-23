using UnityEngine;

/// <summary>
/// Holds the enemy spawn plan: repeating groups and one-time events.
/// </summary>
[CreateAssetMenu(fileName = "SpawnSchedule", menuName = "Spawn Schedule")]
public class SpawnSchedule : ScriptableObject
{
    [SerializeField] private SpawnGroup[] groups;
    [SerializeField] private SpawnEvent[] events;

    public SpawnGroup[] Groups => groups;
    public SpawnEvent[] Events => events;
}

/// <summary>A group of enemies that spawns again and again.</summary>
[System.Serializable]
public class SpawnGroup
{
    [SerializeField] private string label;
    [SerializeField] private EnemyData[] enemies;
    [SerializeField] private float startTime;
    [SerializeField] private float endTime;
    [SerializeField] private float interval = 3f;           // time between batches
    [SerializeField] private float stepDuration = 60f;      // time between steps
    [SerializeField] private int[] countPerStep;            // number of enemies per step

    public string Label => label;
    public EnemyData[] Enemies => enemies;
    public float Interval => Mathf.Max(0.05f, interval);

    public bool IsActive(float time) => time >= startTime && (endTime <= 0f || time < endTime);

    public int GetCount(float time)
    {
        if (countPerStep.Length == 0) return 0;

        int step = stepDuration > 0f ? (int)((time - startTime) / stepDuration) : 0;
        return countPerStep[Mathf.Clamp(step, 0, countPerStep.Length - 1)];
    }
}

/// <summary>A one-time spawn at a set time, like the slime.</summary>
[System.Serializable]
public class SpawnEvent
{
    [SerializeField] private string label;
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private float time;
    [SerializeField] private int count = 1;
    [SerializeField] private bool winOnDeath;

    public string Label => label;
    public EnemyData Data => enemyData;
    public float Time => time;
    public int Count => count;
    public bool WinOnDeath => winOnDeath;
}