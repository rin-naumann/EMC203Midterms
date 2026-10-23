using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A fireball. Flies straight, damages enemies it hits and can explode or chain to another enemy.
/// </summary>
[CreateAssetMenu(fileName = "Fireball", menuName = "Projectile Behaviors/Fireball")]
public class PBFireball : ProjectileBehavior
{
    [Header("Explosion")]
    [Tooltip("The blast projectile. Its Hit Radius is the explosion size")]
    [SerializeField] private ProjectileData explosion;
    [Tooltip("Explosion damage as a fraction of the fireball's damage")]
    [SerializeField] private float explosionDamageMultiplier = 0.5f;

    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        projectile.transform.position += projectile.Direction * (projectile.Data.Speed * deltaTime);

        Enemy hit = FindHit(projectile);
        if (hit == null) return false;

        projectile.HitEnemies.Add(hit);
        hit.TakeDamage(projectile.Damage);

        bool isLast = projectile.PierceLeft <= 0;
        if (!isLast) projectile.PierceLeft--;

        WBStaff.StaffState rules = projectile.Source?.State as WBStaff.StaffState;
        if (rules != null && (rules.ExplodeOnHit || (isLast && rules.ExplodeOnLast)))
            Explode(projectile, hit, rules.ChainChance);

        return isLast;
    }

    private Enemy FindHit(Projectile projectile)
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable || projectile.HitEnemies.Contains(enemy)) continue;

            // ContactRadius is used as the enemy's body radius
            float reach = projectile.Data.HitRadius + enemy.Data.ContactRadius;
            if ((enemy.transform.position - projectile.transform.position).sqrMagnitude <= reach * reach) return enemy;
        }

        return null;
    }

    private void Explode(Projectile projectile, Enemy directHit, float chainChance)
    {
        Vector3 center = projectile.transform.position;

        if (explosion != null)
        {
            Projectile blast = ObjectPooler.Instance.ProjectilePool.Get();
            blast.Init(explosion, center, Vector3.right, projectile.Damage * explosionDamageMultiplier);
            blast.HitEnemies.Add(directHit);
        }

        TryChain(projectile, center, chainChance);
    }

    private void TryChain(Projectile parent, Vector3 center, float chance)
    {
        if (chance <= 0f || Random.value >= chance) return;

        Projectile child = ObjectPooler.Instance.ProjectilePool.Get();
        child.Init(parent.Data, center, ChainDirection(parent, center), parent.Damage);

        child.Source = parent.Source;
        child.PierceLeft = parent.PierceLeft;
        child.HitEnemies.AddRange(parent.HitEnemies);
    }

    private Vector3 ChainDirection(Projectile parent, Vector3 center)
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;
        Enemy nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable || parent.HitEnemies.Contains(enemy)) continue;

            float sqrDistance = (enemy.transform.position - center).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance) { nearest = enemy; nearestSqrDistance = sqrDistance; }
        }

        if (nearest != null) return nearest.transform.position - center;
        return Random.insideUnitCircle.normalized;
    }
}