using UnityEngine;

/// <summary>
/// Flashes the player's sprite red when hurt and blue when a hit is blocked.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerHitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Color damagedColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color blockedColor = new Color(0.3f, 0.9f, 1f);

    private PlayerHealth health;
    private float timer;

    private void Awake() => health = GetComponent<PlayerHealth>();

    // Unsubscribe so the events do not keep this object alive
    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnBlocked += HandleBlocked;
        GameManager.OnRunStarted += Clear;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnBlocked -= HandleBlocked;
        GameManager.OnRunStarted -= Clear;
    }

    private void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f) body.color = Color.white;
    }

    private void HandleDamaged(float damage) => Flash(damagedColor);
    private void HandleBlocked() => Flash(blockedColor);

    private void Flash(Color color)
    {
        timer = health.HitInvulnerability;
        body.color = color;
    }

    private void Clear()
    {
        timer = 0f;
        body.color = Color.white;
    }
}