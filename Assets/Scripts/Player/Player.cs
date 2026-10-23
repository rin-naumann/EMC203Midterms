using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    // [SerializeField] is what makes a private field show up in the Inspector. Without it Unity never
    // fills the field in, so it stays null and everything that reads it throws a NullReferenceException.
    [SerializeField] private PlayerData data;
    private Vector3 startPosition;

    public PlayerData Data => data;
    public PlayerMovement Movement { get; private set; }
    public PlayerHealth Health { get; private set; }
    public PlayerExperience Experience { get; private set; }
    public Vector3 Position => transform.position;
    public float HitRadius => data.HitRadius;

    // The live value. It starts from PlayerData, but upgrades change it during a run, and a
    // ScriptableObject must never be written to at runtime.
    public float PickupRadius { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }

        // Checked before becoming the Instance, so a missing asset can't leave a half-built singleton behind.
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