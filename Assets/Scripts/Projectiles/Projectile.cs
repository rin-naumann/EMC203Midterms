using UnityEngine;
using System.Collections.Generic;

public enum Faction { Player, Enemy }

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class Projectile : MonoBehaviour, IPoolable
{
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private ProjectileData data;

    public ProjectileData Data => data;

    // Per-projectile running state. Behaviors are shared assets, so what changes per shot lives here.
    public float LifeTimer;
    public Vector3 Direction;
    public float Damage;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Configures a freshly pooled projectile. Call it right after Get().
    /// Damage is passed in and not read from the data, so a weapon can scale it with its level.
    /// </summary>
    public void Init(ProjectileData projectileData, Vector3 position, Vector3 direction, float damage)
    {
        data = projectileData;
        Damage = damage;
        LifeTimer = data.LifeTime;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;

        transform.position = position;
        transform.right = Direction;   // turns the sprite to face where it flies (art should point right)

        spriteRenderer.sprite = data.Sprite;

        if (data.AnimatorController != null && animator.runtimeAnimatorController != data.AnimatorController)
            animator.runtimeAnimatorController = data.AnimatorController;
    }

    /// <summary>Called once per frame by ProjectileManager.</summary>
    public void Tick(float deltaTime, Vector3 playerPosition)
    {
        data.Behavior.Tick(this, deltaTime, playerPosition);

        LifeTimer -= deltaTime;
        bool hit = data.Faction == Faction.Enemy ? HitPlayer(playerPosition) : HitEnemy();

        if (hit || LifeTimer <= 0f) ObjectPooler.Instance.ProjectilePool.Release(this);
    }

    private bool HitPlayer(Vector3 playerPosition)
    {
        // TODO: add the player's hit radius to reach (needs a player script that exposes it).
        float reach = data.HitRadius;
        if ((playerPosition - transform.position).sqrMagnitude > reach * reach) return false;

        // TODO: damage the player with Damage. Needs a player health script that implements
        // IDamageable, plus a way for projectiles to reach it.
        return true;
    }

    // Single hit for now. Pierce and explosions belong to the fireball (see PBFireball).
    private bool HitEnemy()
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable) continue;

            // ContactRadius doubles as the enemy's body radius for now. EnemyData has no separate one.
            float reach = data.HitRadius + enemy.Data.ContactRadius;
            if ((enemy.transform.position - transform.position).sqrMagnitude > reach * reach) continue;

            // Damaging can kill the enemy and remove it from the list, so stop looping right away.
            enemy.TakeDamage(Damage);
            return true;
        }

        return false;
    }

    void IPoolable.OnSpawn() => ProjectileManager.Instance?.Register(this);
    void IPoolable.OnDespawn() => ProjectileManager.Instance?.Unregister(this);
}