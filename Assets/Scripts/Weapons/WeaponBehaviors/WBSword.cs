using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// The Sword weapon. Swings in front of the player and hits every enemy inside a cone.
/// The hit is a distance and angle check. The visible slash is a separate animation.
/// </summary>
[CreateAssetMenu(fileName = "Sword", menuName = "Weapon Behaviors/Sword")]
public class WBSword : WeaponBehavior
{
    // Weapon-specific state, stored on the Weapon and not on this shared asset
    private class SwordState : WeaponState
    {
        public bool SwingBehind;
        public bool FiresWave;

        // Progress of the current attack
        public int SwingsLeft;
        public float SwingTimer;
    }

    [Header("Swing")]
    [Tooltip("Width of the swing in degrees")]
    [SerializeField] private float arcAngle = 120f;
    [Tooltip("Seconds between the swings of one attack")]
    [SerializeField] private float swingInterval = 0.15f;

    [Header("Visuals")]
    [SerializeField] private ProjectileData slash;
    [SerializeField] private float slashOffset = 0.5f;

    [Header("Wave (level 10)")]
    [SerializeField] private ProjectileData wave;
    [Tooltip("Wave damage as a fraction of the swing's damage")]
    [SerializeField] private float waveDamageMultiplier = 0.5f;

    [Header("Upgrades")]
    [Tooltip("Damage multiplier per damage up level is 1 plus this (0.5 = +50%)")]
    [SerializeField] private float damageUp = 0.5f;
    [Tooltip("Cooldown is divided by 1 plus this per attack speed up (0.25 = 25% faster)")]
    [SerializeField] private float attackSpeedUp = 0.25f;
    [SerializeField] private float rangeUp = 0.25f;

    public override WeaponState CreateState() => new SwordState();

    public override void Tick(Weapon weapon, float deltaTime)
    {
        weapon.TickCooldown(deltaTime);
        SwordState state = weapon.GetState<SwordState>();

        // Not attacking: wait for the cooldown, then start an attack
        if (state.SwingsLeft == 0)
        {
            if (!weapon.IsReady) return;

            state.SwingsLeft = weapon.Count;
            state.SwingTimer = 0f;
            weapon.StartCooldown();
        }

        // Swings of one attack come one after another
        state.SwingTimer -= deltaTime;
        if (state.SwingTimer > 0f) return;

        Swing(weapon, state);
        state.SwingsLeft--;
        state.SwingTimer = swingInterval;
    }

    private void Swing(Weapon weapon, SwordState state)
    {
        Player player = Player.Instance;
        if (player == null) return;

        weapon.NotifyAttack();

        // Read every swing so the sword follows the player's turns
        Vector3 facing = player.Movement.Facing;

        SwingToward(weapon, state, player.Position, facing);
        if (state.SwingBehind) SwingToward(weapon, state, player.Position, -facing);
    }

    private void SwingToward(Weapon weapon, SwordState state, Vector3 origin, Vector3 direction)
    {
        HitCone(weapon, origin, direction);
        SpawnSlash(weapon, origin, direction);

        if (state.FiresWave && wave != null)
        {
            Projectile shot = ObjectPooler.Instance.ProjectilePool.Get();
            shot.Init(wave, origin, direction, weapon.Damage * waveDamageMultiplier);
            shot.Source = weapon;
        }
    }

    // Damages every enemy within reach and inside the arc
    private void HitCone(Weapon weapon, Vector3 origin, Vector3 direction)
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;
        float cosHalfArc = Mathf.Cos(arcAngle * 0.5f * Mathf.Deg2Rad);

        // Backwards, because a kill removes the enemy from the list
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable) continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            float sqrDistance = toEnemy.sqrMagnitude;
            float reach = weapon.Range + enemy.Data.ContactRadius;
            if (sqrDistance > reach * reach) continue;

            // Inside the arc if the angle is small enough, without normalizing
            if (sqrDistance > 0.0001f && Vector3.Dot(toEnemy, direction) < Mathf.Sqrt(sqrDistance) * cosHalfArc) continue;

            enemy.TakeDamage(weapon.Damage);
        }
    }

    private void SpawnSlash(Weapon weapon, Vector3 origin, Vector3 direction)
    {
        if (slash == null) return;

        Projectile effect = ObjectPooler.Instance.ProjectilePool.Get();
        effect.Init(slash, origin + direction * slashOffset, direction, 0f);

        // Remember where it sits relative to the player
        effect.PlayerOffset = direction * slashOffset;

        // The slash grows with range upgrades
        effect.transform.localScale = Vector3.one * (weapon.Range / weapon.Data.Range);
    }

    // Debug only
    public override void DescribeState(Weapon weapon, StringBuilder builder)
    {
        SwordState state = weapon.GetState<SwordState>();
        builder.Append("behind ").Append(state.SwingBehind ? "yes" : "no");
        builder.Append("  wave ").Append(state.FiresWave ? "yes" : "no");
    }

    public override void StatUpgrade(Weapon weapon, int level)
    {
        switch (level)
        {
            case 2:
            case 6:
                weapon.Damage *= 1f + damageUp;
                break;
            case 3:
            case 7:
                weapon.Cooldown /= 1f + attackSpeedUp;
                break;
            case 4:
            case 9:
                weapon.Range *= 1f + rangeUp;
                break;
            case 8:
            case 10:
                weapon.Count++;  // one more swing per attack
                break;
        }
    }

    public override void UniqueUpgrade(Weapon weapon, int level)
    {
        SwordState state = weapon.GetState<SwordState>();

        switch (level)
        {
            case 5:
                state.SwingBehind = true;
                break;
            case 10:
                state.FiresWave = true;
                break;
        }
    }
}