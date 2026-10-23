using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// An area blast. Damages each enemy in range once.
/// </summary>
[CreateAssetMenu(fileName = "Explosion", menuName = "Projectile Behaviors/Explosion")]
public class PBExplosion : ProjectileBehavior
{
    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable || projectile.HitEnemies.Contains(enemy)) continue;

            float reach = projectile.Data.HitRadius + enemy.Data.ContactRadius;
            if ((enemy.transform.position - projectile.transform.position).sqrMagnitude > reach * reach) continue;

            projectile.HitEnemies.Add(enemy);
            enemy.TakeDamage(projectile.Damage);
        }

        return false;
    }
}