using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    private PlayerData data;
    private PlayerMovement movement;
    private PlayerHealth health;
    private Vector3 startpos;

    public PlayerData Data => data;
    public PlayerMovement Movement { get; private set; }
    public PlayerHealth Health { get; private set; }
    public Vector3 Position => transform.position;
    public float HitRadius => data.HitRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;

        Movement = GetComponent<PlayerMovement>();
        Health = GetComponent<PlayerHealth>();
        Health.Init(data.MaxHealth, data.HitInvulnerability);
    }

    private void OnEnable() => GameManager.OnRunStarted += ResetRun;
    private void OnDisable() => GameManager.OnRunStarted -= ResetRun;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void ResetRun()
    {
        transform.position = startpos;
        Health.ResetHealth();
    }
}
