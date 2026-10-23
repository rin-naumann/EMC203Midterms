using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Tracks the player's HP, damage, healing and regeneration.
/// Raises events for other scripts to react to.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    /// <summary>Raised when a hit is completely blocked.</summary>
    public event Action OnBlocked;
    public event Action OnDied;

    private float maxHealth = 100f;
    private float hitInvulnerability = 0.5f;

    private float regenDelay = 5f;
    private float regenPerSecond = 2f;

    private float timeSinceHit;
    private float currentHealth;
    private float invulnerabilityTimer;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => !isDead;
    public bool IsInvulnerable => invulnerabilityTimer > 0f;
    public float HitInvulnerability => hitInvulnerability;
    public float SecondsUntilRegen => Mathf.Max(0f, regenDelay - timeSinceHit);
    /// <summary>Seconds after a hit before regeneration starts.</summary>
    public float RegenDelay => regenDelay;

    /// <summary>Optional hook that returns the damage that should actually be taken (0 blocks it).</summary>
    public Func<float, float> DamageFilter;

    public void Init(float max, float invulnerabilityTime, float regenAfter, float regenRate)
    {
        maxHealth = max;
        hitInvulnerability = invulnerabilityTime;
        regenDelay = regenAfter;
        regenPerSecond = regenRate;
        ResetHealth();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        invulnerabilityTimer = 0f;
        timeSinceHit = regenDelay;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f) invulnerabilityTimer -= Time.deltaTime;
        if (isDead) return;

        // Regenerates after a quiet spell. Every hit restarts the wait
        timeSinceHit += Time.deltaTime;
        if (timeSinceHit >= regenDelay && currentHealth < maxHealth) Heal(regenPerSecond * Time.deltaTime);
    }

    /// <summary>Restores health up to the maximum.</summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead || IsInvulnerable || damage <= 0f) return;

        if (DamageFilter != null) damage = DamageFilter(damage);

        // A blocked hit still starts the invulnerability
        invulnerabilityTimer = hitInvulnerability;
        timeSinceHit = 0f;
        if (damage <= 0f)
        {
            OnBlocked?.Invoke();
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnDamaged?.Invoke(damage);

        if (currentHealth <= 0f) Die();
        else OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDied?.Invoke();

        GameManager.Instance.ChangeState(GameState.Lose);
    }
}