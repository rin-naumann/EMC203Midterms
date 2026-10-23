using UnityEngine;
using System;

/// <summary>
/// A pooled enemy. Handles health, taking damage, slow and invulnerability.
/// Its movement and attacks come from its EnemyBehavior.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class Enemy : MonoBehaviour, IPoolable, IDamageable
{
    public static event Action<Enemy> OnEnemyDeath;
    /// <summary>Raised when an enemy takes damage and survives.</summary>
    public static event Action<Enemy> OnEnemyHit;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemyData data;
    private float currentHealth;

    public EnemyData Data => data;
    public Animator Animator => animator;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0f;
    public float AttackTimer;
    public int State;
    public Vector3 TargetPosition;

    /// <summary>An object a behavior created for this enemy, like the slime's landing indicator.</summary>
    public GameObject Marker;

    // Invulnerable if a behavior says so, or briefly after a hit
    private bool behaviorInvulnerable;
    private float hitTimer;
    public bool IsInvulnerable
    {
        get => behaviorInvulnerable || hitTimer > 0f;
        set => behaviorInvulnerable = value;
    }

    private static readonly Color HitColor = new Color(1f, 0.3f, 0.3f);

    private float slowMultiplier = 1f;
    private float slowTimer;

    /// <summary>Current speed, lower while slowed.</summary>
    public float MoveSpeed => data.MoveSpeed * (slowTimer > 0f ? slowMultiplier : 1f);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Init(EnemyData enemyData, Vector3 position)
    {
        data = enemyData;
        currentHealth = data.MaxHealth;
        transform.position = position;

        AttackTimer = 0f;
        State = 0;
        TargetPosition = Vector3.zero;
        RemoveMarker();
        IsInvulnerable = false;
        hitTimer = 0f;
        slowMultiplier = 1f;
        slowTimer = 0f;

        spriteRenderer.color = Color.white;  // a pooled enemy may still be red
        spriteRenderer.enabled = true;

        // Always set the controller so a pooled enemy keeps no old animation
        if (animator.runtimeAnimatorController != data.AnimatorController)
            animator.runtimeAnimatorController = data.AnimatorController;

        // Set the sprite after the controller change
        spriteRenderer.sprite = data.Sprite;

        data.Behavior.OnInit(this);
    }

    public void Tick(float deltaTime, Vector3 playerPosition)
    {
        if (slowTimer > 0f) slowTimer -= deltaTime;

        if (hitTimer > 0f)
        {
            hitTimer -= deltaTime;
            if (hitTimer <= 0f) spriteRenderer.color = Color.white;
        }

        data.Behavior.Tick(this, deltaTime, playerPosition);
    }

    /// <summary>Slows the enemy for a while. The strongest slow wins.</summary>
    public void ApplySlow(float multiplier, float duration)
    {
        if (slowTimer <= 0f || multiplier < slowMultiplier) slowMultiplier = multiplier;
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    public void RemoveMarker()
    {
        if (Marker == null) return;
        Destroy(Marker);
        Marker = null;
    }

    public void SetVisible(bool visible) => spriteRenderer.enabled = visible;

    public void FaceTowards(float playerX)
    {
        float dx = playerX - transform.position.x;
        if (Mathf.Abs(dx) < 0.05f) return;
        spriteRenderer.flipX = dx < 0f;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive || IsInvulnerable) return;
        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // Survived: turn red and become invulnerable briefly
        hitTimer = data.HitInvulnerability;
        if (hitTimer > 0f) spriteRenderer.color = HitColor;
        OnEnemyHit?.Invoke(this);
    }

    public void Die()
    {
        // Do not announce a second death
        if (!gameObject.activeSelf) return;

        OnEnemyDeath?.Invoke(this);
        ObjectPooler.Instance.EnemyPool.Release(this);
    }

    void IPoolable.OnSpawn() => EnemyManager.Instance?.Register(this);
    void IPoolable.OnDespawn()
    {
        RemoveMarker();  // the marker goes with the enemy
        EnemyManager.Instance?.Unregister(this);
    }
}