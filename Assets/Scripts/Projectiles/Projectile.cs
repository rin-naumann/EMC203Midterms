using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A pooled projectile. Its movement and hits come from its ProjectileBehavior.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class Projectile : MonoBehaviour, IPoolable
{
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private ProjectileData data;

    public ProjectileData Data => data;

    public float LifeTimer;
    public Vector3 Direction;
    public float Damage;

    // Per-shot state, set by whoever fires the shot after Init
    /// <summary>How many more enemies the shot can pass through.</summary>
    public int PierceLeft;
    /// <summary>Enemies already hit, so a piercing shot does not hit one every frame.</summary>
    public readonly List<Enemy> HitEnemies = new List<Enemy>();

    /// <summary>The weapon that fired this shot, or null if none.</summary>
    public Weapon Source;

    /// <summary>Where the shot sits relative to the player, for shots that follow the player.</summary>
    public Vector3 PlayerOffset;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Init(ProjectileData projectileData, Vector3 position, Vector3 direction, float damage)
    {
        data = projectileData;
        Damage = damage;
        LifeTimer = data.LifeTime;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;

        // A pooled object keeps old state, so reset it
        PierceLeft = 0;
        HitEnemies.Clear();
        Source = null;
        PlayerOffset = Vector3.zero;

        transform.position = position;

        // Rotate around Z, adjusted for how the sprite was drawn
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg - data.SpriteFacing;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one;  // a weapon may have scaled this object before

        // Mirror on Y past 90 degrees to keep the sprite upright
        spriteRenderer.flipY = Mathf.Cos(angle * Mathf.Deg2Rad) < -0.01f;

        // Always set the controller so a pooled object keeps no old animation
        if (animator.runtimeAnimatorController != data.AnimatorController)
            animator.runtimeAnimatorController = data.AnimatorController;

        // Set the sprite after the controller change
        spriteRenderer.sprite = data.Sprite;
    }

    public void Tick(float deltaTime, Vector3 playerPosition)
    {
        bool done = data.Behavior.Tick(this, deltaTime, playerPosition);
        if (!done && data.EndsWithAnimation && AnimationFinished()) done = true;
        LifeTimer -= deltaTime;
        if (done || LifeTimer <= 0f) ObjectPooler.Instance.ProjectilePool.Release(this);
    }

    // True once the animation has played through once
    private bool AnimationFinished()
    {
        if (animator.runtimeAnimatorController == null || LifeTimer >= data.LifeTime) return false;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        return state.normalizedTime >= 1f && !animator.IsInTransition(0);
    }

    void IPoolable.OnSpawn() => ProjectileManager.Instance?.Register(this);
    void IPoolable.OnDespawn() => ProjectileManager.Instance?.Unregister(this);
}