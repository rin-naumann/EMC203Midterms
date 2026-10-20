using UnityEngine;
using System.Collections.Generic;

/// <summary>The Staff's fireball.</summary>
[CreateAssetMenu(fileName = "Fireball", menuName = "Projectile Behaviors/Fireball")]
public class PBFireball : ProjectileBehavior
{
    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        projectile.transform.position += projectile.Direction * (projectile.Data.Speed * deltaTime);

        // First pass: the fireball hits one enemy and is used up. The pierce, explosion and chain
        // rules replace this once the Staff exists (see the TODO below).
        return HitEnemy(projectile);

        // TODO (needs the Staff weapon script, which owns the level):
        //  - pierce (levels 4 and 9): how many enemies a shot passes through.
        //  - explode on the last enemy (level 5) and on every enemy hit (level 8).
        //  - 25% chance to spawn another fireball on explosion (level 10). The child can chain again and
        //    inherits the parent's REMAINING pierce.
        // Each needs per-shot state on Projectile (pierce left, hit list) set from the weapon at Init.
    }

    private bool HitEnemy(Projectile projectile)
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable) continue;

            // ContactRadius doubles as the enemy's body radius for now. EnemyData has no separate one.
            float reach = projectile.Data.HitRadius + enemy.Data.ContactRadius;
            if ((enemy.transform.position - projectile.transform.position).sqrMagnitude > reach * reach) continue;

            // Damaging can kill the enemy and remove it from the list, so stop looping right away.
            enemy.TakeDamage(projectile.Damage);
            return true;
        }

        return false;
    }
}