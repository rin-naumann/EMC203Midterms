using UnityEngine;

/// <summary>
/// Moves the camera to follow the player smoothly and adds screen shake.
/// The camera must not be a child of the player.
/// </summary>
public class CameraHandler : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("Higher is tighter, lower is lazier")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Shake")]
    [Tooltip("Furthest the shake moves the camera, in world units")]
    [SerializeField] private float maxOffset = 0.35f;
    [SerializeField] private float frequency = 25f;
    [SerializeField] private float decayPerSecond = 2f;

    [Header("Slime slam")]
    [SerializeField, Range(0f, 1f)] private float slamTrauma = 0.9f;

    [Header("Player hurt")]
    [SerializeField, Range(0f, 1f)] private float hurtTrauma = 0.3f;

    private Vector3 followPosition;  // where the camera would be without shake
    private bool snap = true;  // jump to the player instead of gliding
    private float trauma;
    private float seed;
    private PlayerHealth health;

    private void Awake()
    {
        followPosition = transform.position;
        seed = Random.value * 100f;  // so each run shakes differently
    }

    // Unsubscribe so the static event does not keep this object alive
    private void OnEnable()
    {
        EBSlime.OnSlam += HandleSlam;
        GameManager.OnRunStarted += HandleRunStarted;
    }

    private void OnDisable()
    {
        EBSlime.OnSlam -= HandleSlam;
        GameManager.OnRunStarted -= HandleRunStarted;
    }

    // Hooked here because the player exists by now
    private void Start()
    {
        if (Player.Instance == null) return;
        health = Player.Instance.Health;
        health.OnDamaged += HandlePlayerDamaged;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDamaged -= HandlePlayerDamaged;
    }

    public void AddTrauma(float amount) => trauma = Mathf.Min(1f, trauma + amount);

    // Jump with the player on a new run
    private void HandleRunStarted()
    {
        snap = true;
        trauma = 0f;
    }

    private void HandlePlayerDamaged(float damage) => AddTrauma(hurtTrauma);

    private void HandleSlam(Vector3 position, float radius)
    {
        if (Player.Instance == null) return;

        float distance = Vector3.Distance(Player.Instance.Position, position);
        AddTrauma(slamTrauma);
    }

    // After everything else has moved
    private void LateUpdate()
    {
        Follow();
        transform.position = followPosition + ShakeOffset();
    }

    private void Follow()
    {
        if (Player.Instance == null) return;

        // Only X and Y follow
        Vector3 target = Player.Instance.Position;
        target.z = followPosition.z;

        if (snap)
        {
            snap = false;
            followPosition = target;
            return;
        }

        // Smooth follow that moves the same at any frame rate
        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        followPosition = Vector3.Lerp(followPosition, target, blend);
    }

    private Vector3 ShakeOffset()
    {
        // Stop shaking when the game is not playing
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            trauma = 0f;
            return Vector3.zero;
        }

        if (trauma <= 0f) return Vector3.zero;

        trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.deltaTime);
        float strength = trauma * trauma * maxOffset;

        // Perlin noise gives a smooth wobble
        float time = Time.time * frequency;
        float x = (Mathf.PerlinNoise(seed, time) * 2f - 1f) * strength;
        float y = (Mathf.PerlinNoise(seed + 50f, time) * 2f - 1f) * strength;
        return new Vector3(x, y, 0f);
    }
}