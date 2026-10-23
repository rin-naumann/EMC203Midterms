using UnityEngine;
using System.Collections.Generic;

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

    // Per-shot state, set by whoever fires the shot (a weapon) right after Init.
    /// <summary>How many more enemies the shot can pass through. 0 means it stops at the next hit.</summary>
    public int PierceLeft;
    /// <summary>Enemies this shot has already hit, so a piercing shot can't hit the same one every frame.</summary>
    public readonly List<Enemy> HitEnemies = new List<Enemy>();

    /// <summary>The weapon that fired this shot, or null for shots that don't come from a weapon (enemy arrows).
    /// Behaviors can read that weapon's current rules from it.</summary>
    public Weapon Source;

    /// <summary>Where the shot sits relative to the player. Only used by shots that follow the player (the Sword's slash).</summary>
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

        // A pooled object carries its old state, so reset the per-shot state too.
        PierceLeft = 0;
        HitEnemies.Clear();
        Source = null;
        PlayerOffset = Vector3.zero;

        transform.position = position;

        // An explicit rotation around Z, turned by how the sprite was drawn. Setting transform.right to a
        // vector exactly opposite the sprite's facing can pick a rotation around a different axis.
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg - data.SpriteFacing;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one;   // a weapon may have scaled this pooled object last time

        // Rotating a sprite past 90 degrees turns it upside down, so mirror it on Y to keep it upright.
        spriteRenderer.flipY = Mathf.Cos(angle * Mathf.Deg2Rad) < -0.01f;

        // Always assign the controller, even when there is none. A pooled object that keeps its last
        // owner's animation keeps playing it, and the Animator overwrites the sprite set above. That is
        // how a skeleton's arrow could turn into a fireball.
        if (animator.runtimeAnimatorController != data.AnimatorController)
            animator.runtimeAnimatorController = data.AnimatorController;

        // The sprite comes AFTER the controller change, so nothing from the old controller can overwrite it.
        // With a controller, its animation takes over from this frame on. Without one, this sprite stays.
        spriteRenderer.sprite = data.Sprite;
    }

    public void Tick(float deltaTime, Vector3 playerPosition)
    {
        bool done = data.Behavior.Tick(this, deltaTime, playerPosition);
        if (!done && data.EndsWithAnimation && AnimationFinished()) done = true;
        LifeTimer -= deltaTime;
        if (done || LifeTimer <= 0f) ObjectPooler.Instance.ProjectilePool.Release(this);
    }

    // True once the animation has played through once. Skipped on the first frame, because the Animator
    // hasn't run yet and would still report the previous life's state.
    private bool AnimationFinished()
    {
        if (animator.runtimeAnimatorController == null || LifeTimer >= data.LifeTime) return false;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        return state.normalizedTime >= 1f && !animator.IsInTransition(0);
    }

    void IPoolable.OnSpawn() => ProjectileManager.Instance?.Register(this);
    void IPoolable.OnDespawn() => ProjectileManager.Instance?.Unregister(this);
}