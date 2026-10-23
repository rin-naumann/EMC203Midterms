using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// The Sword: swings in front of the player, hitting every enemy inside a cone.
///   Level 2, 6   damage up              Level 5   also swings behind the player
///   Level 3, 7   attack speed up        Level 8   swings twice (Count 2)
///   Level 4, 9   attack range up        Level 10  swings three times (Count 3), and every swing shoots a piercing wave
/// A swing is an instant hit (a distance and angle check, no colliders). The slash you see is a separate
/// pooled projectile that only plays an animation.
/// </summary>
[CreateAssetMenu(fileName = "Sword", menuName = "Weapon Behaviors/Sword")]
public class WBSword : WeaponBehavior
{
    // Weapon-specific state. It lives on the Weapon, never on this asset, which every Sword shares.
    private class SwordState : WeaponState
    {
        public bool SwingBehind;
        public bool FiresWave;

        // Progress of the attack that is happening right now.
        public int SwingsLeft;
        public float SwingTimer;
    }

    [Header("Swing")]
    [Tooltip("Width of the swing in degrees. 120 means 60 to each side of the facing direction.")]
    [SerializeField] private float arcAngle = 120f;
    [Tooltip("Seconds between the swings of one attack. How many swings there are is the weapon's Count.")]
    [SerializeField] private float swingInterval = 0.15f;

    [Header("Visuals")]
    [Tooltip("The slash animation: a projectile using the Effect behavior. Its lifetime should match the animation.")]
    [SerializeField] private ProjectileData slash;
    [Tooltip("How far in front of the player the slash appears.")]
    [SerializeField] private float slashOffset = 0.5f;

    [Header("Wave (level 10)")]
    [Tooltip("A projectile using the Wave behavior.")]
    [SerializeField] private ProjectileData wave;
    [Tooltip("Wave damage as a fraction of the swing's damage.")]
    [SerializeField] private float waveDamageMultiplier = 0.5f;

    [Header("Upgrades")]
    [Tooltip("Damage up levels multiply damage by 1 plus this. 0.5 is +50%.")]
    [SerializeField] private float damageUp = 0.5f;
    [Tooltip("Attack speed up levels divide the cooldown by 1 plus this. 0.25 is 25% faster.")]
    [SerializeField] private float attackSpeedUp = 0.25f;
    [Tooltip("Range up levels multiply the range by 1 plus this.")]
    [SerializeField] private float rangeUp = 0.25f;

    public override WeaponState CreateState() => new SwordState();

    public override void Tick(Weapon weapon, float deltaTime)
    {
        weapon.TickCooldown(deltaTime);
        SwordState state = weapon.GetState<SwordState>();

        // Not in the middle of an attack: wait for the cooldown, then start one.
        // The Sword swings whether or not an enemy is near, because the swing is a fixed area in front of the player.
        if (state.SwingsLeft == 0)
        {
            if (!weapon.IsReady) return;

            state.SwingsLeft = weapon.Count;
            state.SwingTimer = 0f;
            weapon.StartCooldown();
        }

        // Swings of one attack come one after another, a short gap apart.
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

        // Read each swing, so the sword follows the player if they turn between swings.
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

    // Damages every enemy within reach AND inside the arc. No colliders: a distance check, then an angle check.
    private void HitCone(Weapon weapon, Vector3 origin, Vector3 direction)
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;
        float cosHalfArc = Mathf.Cos(arcAngle * 0.5f * Mathf.Deg2Rad);

        // Backwards: a kill removes that enemy from the list, and going backwards keeps the rest in place.
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            if (enemy.IsInvulnerable) continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            float sqrDistance = toEnemy.sqrMagnitude;
            float reach = weapon.Range + enemy.Data.ContactRadius;
            if (sqrDistance > reach * reach) continue;

            // Inside the arc if the angle to the enemy is small enough: dot(toEnemy, direction) >= |toEnemy| * cos(half arc).
            // Comparing like this avoids normalizing. An enemy standing on the player counts as hit.
            if (sqrDistance > 0.0001f && Vector3.Dot(toEnemy, direction) < Mathf.Sqrt(sqrDistance) * cosHalfArc) continue;

            enemy.TakeDamage(weapon.Damage);
        }
    }

    private void SpawnSlash(Weapon weapon, Vector3 origin, Vector3 direction)
    {
        if (slash == null) return;

        Projectile effect = ObjectPooler.Instance.ProjectilePool.Get();
        effect.Init(slash, origin + direction * slashOffset, direction, 0f);

        // Remember where it sits relative to the player. If the slash's behavior follows the player, this is the spot it keeps.
        effect.PlayerOffset = direction * slashOffset;

        // The slash grows with the range upgrades. (Projectile.Init already keeps it upright when it faces left.)
        effect.transform.localScale = Vector3.one * (weapon.Range / weapon.Data.Range);
    }

    // Debug only. The number of swings is the weapon's Count, which the debugger already shows.
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
                weapon.Count++;   // one more swing per attack
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