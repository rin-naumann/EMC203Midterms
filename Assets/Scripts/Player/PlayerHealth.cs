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
    public event Action OnDied;

    private float maxHealth = 100f;
    private float hitInvulnerability = 0.5f;
    private float currentHealth;
    private float invulnerabilityTimer;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => !isDead;
    public bool IsInvulnerable => invulnerabilityTimer > 0f;

    public void Init(float max, float invulnerabilityTime)
    {
        maxHealth = max;
        hitInvulnerability = invulnerabilityTime;
        ResetHealth();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        invulnerabilityTimer = 0f;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f) invulnerabilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || IsInvulnerable || damage <= 0f) return;

        invulnerabilityTimer = hitInvulnerability;
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