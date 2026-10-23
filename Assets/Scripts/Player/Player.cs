using UnityEngine;

/// <summary>
/// The player. Holds references to its other scripts and resets them when a new run starts.
/// </summary>
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [SerializeField] private PlayerData data;
    private Vector3 startPosition;

    public PlayerData Data => data;
    public PlayerMovement Movement { get; private set; }
    public PlayerHealth Health { get; private set; }
    public PlayerExperience Experience { get; private set; }
    public Vector3 Position => transform.position;
    public float HitRadius => data.HitRadius;

    // The live value, which upgrades can change during a run
    public float PickupRadius { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }

        // Checked before becoming the Instance so a missing asset leaves nothing half built
        if (data == null)
        {
            Debug.LogError("Player has no PlayerData assigned.", this);
            enabled = false;
            return;
        }

        Instance = this;
        startPosition = transform.position;

        Movement = GetComponent<PlayerMovement>();
        Health = GetComponent<PlayerHealth>();
        Experience = GetComponent<PlayerExperience>();

        Health.Init(data.MaxHealth, data.HitInvulnerability, data.RegenDelay, data.RegenPerSecond);
        Experience.Init(data.BaseExpToLevel, data.ExpCostIncrease);
        PickupRadius = data.PickupRadius;
    }

    private void OnEnable() => GameManager.OnRunStarted += ResetRun;
    private void OnDisable() => GameManager.OnRunStarted -= ResetRun;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void ResetRun()
    {
        transform.position = startPosition;
        Health.ResetHealth();
        Experience.ResetRun();
        PickupRadius = data.PickupRadius;
    }
}