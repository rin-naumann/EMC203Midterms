using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// The player's HP. It only tracks health and raises events. The HP bar, screen shake and red flash
/// are separate scripts that listen to the events, so none of that feedback lives here.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    /// <summary>A hit was completely blocked (the Shield's immunity). No damage was taken, but the short invulnerability still starts.</summary>
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
    /// <summary>How long the no-regeneration wait lasts after a hit. The HP bar's ghost drains over exactly this long.</summary>
    public float RegenDelay => regenDelay;

    /// <summary>
    /// Optional hook: takes the incoming damage and returns what should actually be taken (0 blocks it).
    /// PlayerHealth doesn't know what sits behind it. At the moment that is the weapons (the Shield).
    /// </summary>
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

        // Regenerates only after a quiet spell. Every hit, even a blocked one, restarts the wait.
        timeSinceHit += Time.deltaTime;
        if (timeSinceHit >= regenDelay && currentHealth < maxHealth) Heal(regenPerSecond * Time.deltaTime);
    }

    /// <summary>Restores health, up to the maximum. Used by regeneration and by heal drops. Does nothing once dead.</summary>
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

        // Even a fully blocked hit counts as a hit, so the short invulnerability starts either way.
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