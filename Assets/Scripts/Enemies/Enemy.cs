using UnityEngine;
using System;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class Enemy : MonoBehaviour, IPoolable, IDamageable
{
    public static event Action<Enemy> OnEnemyDeath;
    /// <summary>An enemy took damage and survived. Used for the hit sound.</summary>
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

    /// <summary>An object a behavior put in the world for this enemy (the slime's landing indicator). Removed when the enemy leaves.</summary>
    public GameObject Marker;

    // Two kinds of "can't be hurt", combined into IsInvulnerable so every weapon only has to check one thing:
    //  - set by a behavior, for as long as it likes (the slime while it is in the air);
    //  - the short window after taking a hit, which also stops one enemy being hit many times at once.
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

    /// <summary>Walking speed right now: the data's speed, reduced while slowed. Behaviors use this and not Data.MoveSpeed.</summary>
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

        spriteRenderer.color = Color.white;   // a pooled enemy may have been red when it was released
        spriteRenderer.enabled = true;

        // Always assign the controller, so a pooled enemy never keeps the previous type's animation.
        if (animator.runtimeAnimatorController != data.AnimatorController)
            animator.runtimeAnimatorController = data.AnimatorController;

        // The sprite comes AFTER the controller change, so nothing from the old controller can overwrite it.
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

    /// <summary>Slows the enemy to this fraction of its speed (0.8 is 20% slower) for a while. The strongest slow wins; the time is refreshed.</summary>
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

        // Survived the hit: red, and untouchable, for exactly the same amount of time.
        hitTimer = data.HitInvulnerability;
        if (hitTimer > 0f) spriteRenderer.color = HitColor;
        OnEnemyHit?.Invoke(this);
    }

    public void Die()
    {
        // Already back in the pool: don't announce a second death for the same enemy.
        if (!gameObject.activeSelf) return;

        OnEnemyDeath?.Invoke(this);
        ObjectPooler.Instance.EnemyPool.Release(this);
    }

    void IPoolable.OnSpawn() => EnemyManager.Instance?.Register(this);
    void IPoolable.OnDespawn()
    {
        RemoveMarker();   // however the enemy left (killed, run restarted), its marker goes with it
        EnemyManager.Instance?.Unregister(this);
    }
}