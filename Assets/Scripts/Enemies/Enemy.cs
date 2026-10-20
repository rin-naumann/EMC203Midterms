using UnityEngine;
using System;

public class Enemy : MonoBehaviour, IPoolable, IDamageable
{
    public static event Action<Enemy> OnEnemyDeath;

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
    public bool IsInvulnerable;

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
        IsInvulnerable = false;

        spriteRenderer.sprite = data.Sprite;
        spriteRenderer.enabled = true;

        if (data.AnimatorController != null && animator.runtimeAnimatorController != data.AnimatorController)
            animator.runtimeAnimatorController = data.AnimatorController;

        data.Behavior.OnInit(this);
    }

    public void Tick(float deltaTime, Vector3 playerPosition) => data.Behavior.Tick(this, deltaTime, playerPosition);

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
        if (currentHealth <= 0f) Die();
    }

    public void Die()
    {
        OnEnemyDeath?.Invoke(this);
        ObjectPooler.Instance.EnemyPool.Release(this);
    }

    void IPoolable.OnSpawn() => EnemyManager.Instance?.Register(this);
    void IPoolable.OnDespawn() => EnemyManager.Instance?.Unregister(this);
}