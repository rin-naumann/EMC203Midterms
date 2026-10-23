using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The shield's orbiting projectile. Damages enemies it touches and slows them at higher levels.
/// </summary>
[CreateAssetMenu(fileName = "Shield", menuName = "Projectile Behaviors/Shield")]
public class PBShield : ProjectileBehavior
{
    [Header("Slow (Shield level 8)")]
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.8f;
    [SerializeField] private float slowDuration = 1.5f;

    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        Weapon source = projectile.Source;
        if (source == null) return false;

        bool slows = (source.State as WBShield.ShieldState)?.Slows ?? false;
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable) continue;

            float reach = projectile.Data.HitRadius + enemy.Data.ContactRadius;
            if ((enemy.transform.position - projectile.transform.position).sqrMagnitude > reach * reach) continue;

            if (slows) enemy.ApplySlow(slowMultiplier, slowDuration);
            enemy.TakeDamage(source.Damage);
        }

        return false;
    }
}