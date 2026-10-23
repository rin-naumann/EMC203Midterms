using UnityEngine;

/// <summary>
/// Everything the camera does: it follows the player smoothly, and it shakes. Put it on the camera, which must NOT
/// be a child of the player, because this script moves it in the world itself.
/// The follow and the shake are kept apart on purpose. The follow position is its own value, and the shake is
/// added on top when the camera is placed. If the shake were applied to the camera and then read back by the
/// follow, the follow would try to "catch up" to the shake and the camera would drift.
/// </summary>
public class CameraHandler : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("How tightly the camera follows. Higher is tighter and lower is lazier. About 8 is a gentle trail; 20 is almost rigid.")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Shake")]
    [Tooltip("The furthest the camera moves from its normal spot at full trauma, in world units.")]
    [SerializeField] private float maxOffset = 0.35f;
    [Tooltip("How fast the shake wobbles. Higher is more frantic.")]
    [SerializeField] private float frequency = 25f;
    [Tooltip("Trauma lost per second. 2 means a full shake lasts half a second.")]
    [SerializeField] private float decayPerSecond = 2f;

    [Header("Slime slam")]
    [SerializeField, Range(0f, 1f)] private float slamTrauma = 0.9f;

    [Header("Player hurt")]
    [Tooltip("0 turns the shake off when the player is hit.")]
    [SerializeField, Range(0f, 1f)] private float hurtTrauma = 0.3f;

    private Vector3 followPosition;   // where the camera would be with no shake
    private bool snap = true;         // jump straight to the player instead of gliding (first frame, new run)
    private float trauma;
    private float seed;
    private PlayerHealth health;

    private void Awake()
    {
        followPosition = transform.position;
        seed = Random.value * 100f;   // so two runs don't shake in exactly the same pattern
    }

    // Static events outlive this object, so always unsubscribe in OnDisable.
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

    // The player's own events are on the player object, which exists by now, so they are hooked here.
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

    // The player jumps back to the start on a new run, and the camera should be there with it, not fly across the map.
    private void HandleRunStarted()
    {
        snap = true;
        trauma = 0f;
    }

    private void HandlePlayerDamaged(float damage) => AddTrauma(hurtTrauma);

    private void HandleSlam(Vector3 position, float radius)
    {
        if (Player.Instance == null) return;

        // Full strength at the player's feet, fading to nothing at the maximum distance.
        float distance = Vector3.Distance(Player.Instance.Position, position);
        AddTrauma(slamTrauma);
    }

    // After everything else has moved this frame, so the camera sees the player's final position.
    private void LateUpdate()
    {
        Follow();
        transform.position = followPosition + ShakeOffset();
    }

    private void Follow()
    {
        if (Player.Instance == null) return;

        // Keep the camera's own depth: only X and Y follow.
        Vector3 target = Player.Instance.Position;
        target.z = followPosition.z;

        if (snap)
        {
            snap = false;
            followPosition = target;
            return;
        }

        // Exponential smoothing: each frame it closes a fixed share of the remaining distance. Written with
        // Exp so it moves the same at 30 FPS as at 144, which a plain Lerp(a, b, speed * dt) would not.
        // At timeScale 0 the delta time is 0, so a paused camera simply stays where it is.
        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        followPosition = Vector3.Lerp(followPosition, target, blend);
    }

    private Vector3 ShakeOffset()
    {
        // A frozen game (pause, level-up, win, lose) shouldn't leave the camera stuck in the middle of a shake.
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            trauma = 0f;
            return Vector3.zero;
        }

        if (trauma <= 0f) return Vector3.zero;

        trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.deltaTime);
        float strength = trauma * trauma * maxOffset;

        // Perlin noise gives a smooth wobble instead of random jumps. Each axis reads a different part of it.
        float time = Time.time * frequency;
        float x = (Mathf.PerlinNoise(seed, time) * 2f - 1f) * strength;
        float y = (Mathf.PerlinNoise(seed + 50f, time) * 2f - 1f) * strength;
        return new Vector3(x, y, 0f);
    }
}