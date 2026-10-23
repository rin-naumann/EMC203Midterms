using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Wave", menuName = "Projectile Behaviors/Wave")]
public class PBWave : ProjectileBehavior
{
    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        projectile.transform.position += projectile.Direction * (projectile.Data.Speed * deltaTime);

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