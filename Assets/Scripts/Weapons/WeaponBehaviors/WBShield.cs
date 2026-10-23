using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// The Shield: shields orbit the player and damage enemies they touch. A new one appears every
/// Cooldown seconds until the player has Count of them.
///   Level 2, 6   damage up                Level 7   four shields
///   Level 3      two shields              Level 8   enemies hit are slowed
///   Level 4, 9   rotation speed up        Level 10  eight shields, and immunity to one hit every 30 seconds
///   Level 5      when the player is hit, the damage is reduced and one shield is lost
/// The shields are pooled projectiles that use the PBShield behavior, and they do all the hitting.
/// This weapon only spawns them, keeps them evenly spaced around the player, and protects the player
/// (damage reduction and immunity).
/// </summary>
[CreateAssetMenu(fileName = "Shield", menuName = "Weapon Behaviors/Shield")]
public class WBShield : WeaponBehavior
{
    // Weapon-specific state. It lives on the Weapon, never on this asset, which every Shield shares.
    // Public because PBShield reads Slows from it.
    public class ShieldState : WeaponState
    {
        public readonly List<Projectile> Shields = new List<Projectile>();
        public float Angle;              // how far the whole ring has turned, in degrees
        public float RotationSpeed;      // degrees per second

        public bool BlocksDamage;        // level 5
        public bool Slows;               // level 8: PBShield slows the enemies it hits
        public bool HasImmunity;         // level 10
        public float ImmunityTimer;      // time until the immunity is ready again
    }

    [Header("Shield")]
    [Tooltip("A projectile using the Shield behavior (PBShield). Its Hit Radius is how big each shield is.")]
    [SerializeField] private ProjectileData shield;
    [Tooltip("Degrees per second at level 1.")]
    [SerializeField] private float baseRotationSpeed = 120f;

    [Header("Upgrades")]
    [Tooltip("Damage up levels multiply damage by 1 plus this. 0.5 is +50%.")]
    [SerializeField] private float damageUp = 0.5f;
    [Tooltip("Rotation speed up levels multiply the speed by 1 plus this.")]
    [SerializeField] private float rotationUp = 0.5f;
    [Tooltip("Level 5: how much of the damage is removed when a hit costs the player a shield. 0.3 is 30%.")]
    [SerializeField, Range(0f, 1f)] private float damageReduction = 0.3f;
    [Tooltip("Level 10: seconds until the immunity is ready again after it blocks a hit.")]
    [SerializeField] private float immunityInterval = 10f;

    public override WeaponState CreateState() => new ShieldState { RotationSpeed = baseRotationSpeed };

    public override void Tick(Weapon weapon, float deltaTime)
    {
        Player player = Player.Instance;
        if (player == null) return;

        ShieldState state = weapon.GetState<ShieldState>();

        weapon.TickCooldown(deltaTime);
        if (state.ImmunityTimer > 0f) state.ImmunityTimer -= deltaTime;

        // Below the shield count and the timer is up: make one more. The cooldown is what makes it one every few seconds.
        if (state.Shields.Count < weapon.Count && weapon.IsReady)
        {
            SpawnShield(weapon, player.Position, state);
            weapon.StartCooldown();
            weapon.NotifyAttack();
        }

        state.Angle = Mathf.Repeat(state.Angle + state.RotationSpeed * deltaTime, 360f);
        PlaceShields(weapon, state, player.Position);
    }

    private void SpawnShield(Weapon weapon, Vector3 origin, ShieldState state)
    {
        Projectile piece = ObjectPooler.Instance.ProjectilePool.Get();
        piece.Init(shield, origin, Vector3.right, 0f);

        // PBShield reads the damage and the Slows rule from the Source, so upgrades reach shields that already exist.
        piece.Source = weapon;

        // A shield never expires on its own: it is only removed when the player loses it.
        piece.LifeTimer = float.MaxValue;
        state.Shields.Add(piece);
    }

    // Spreads the shields evenly around a circle. Using the number of shields that exist right now means
    // the rest re-space themselves when one is lost.
    private void PlaceShields(Weapon weapon, ShieldState state, Vector3 origin)
    {
        int count = state.Shields.Count;

        for (int i = 0; i < count; i++)
        {
            float angle = (state.Angle + 360f * i / count) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * weapon.Range;
            state.Shields[i].transform.position = origin + offset;
        }
    }

    // The player is about to be hurt. Immunity is checked first and blocks the hit completely,
    // without costing a shield. Otherwise a shield takes part of the hit and is lost.
    public override float FilterPlayerDamage(Weapon weapon, float damage)
    {
        ShieldState state = weapon.GetState<ShieldState>();

        if (state.HasImmunity && state.ImmunityTimer <= 0f)
        {
            state.ImmunityTimer = immunityInterval;
            return 0f;
        }

        if (state.BlocksDamage && state.Shields.Count > 0)
        {
            int last = state.Shields.Count - 1;
            ObjectPooler.Instance.ProjectilePool.Release(state.Shields[last]);
            state.Shields.RemoveAt(last);

            // The replacement takes a full cooldown to appear.
            weapon.StartCooldown();
            return damage * (1f - damageReduction);
        }

        return damage;
    }

    // Debug only.
    public override void DescribeState(Weapon weapon, StringBuilder builder)
    {
        ShieldState state = weapon.GetState<ShieldState>();
        builder.Append("shields ").Append(state.Shields.Count).Append('/').Append(weapon.Count);
        builder.Append("  spin ").Append(state.RotationSpeed.ToString("0")).Append("/s");
        if (state.BlocksDamage) builder.Append("  reduces-damage");
        if (state.Slows) builder.Append("  slows");
        if (state.HasImmunity)
            builder.Append("  immunity ").Append(state.ImmunityTimer > 0f ? state.ImmunityTimer.ToString("0") + "s" : "READY");
    }

    public override void StatUpgrade(Weapon weapon, int level)
    {
        ShieldState state = weapon.GetState<ShieldState>();

        switch (level)
        {
            case 2:
            case 6:
                weapon.Damage *= 1f + damageUp;
                break;
            case 3:
                weapon.Count = 2;
                break;
            case 4:
            case 9:
                state.RotationSpeed *= 1f + rotationUp;
                break;
            case 7:
                weapon.Count = 4;
                break;
            case 10:
                weapon.Count = 8;
                break;
        }
    }

    public override void UniqueUpgrade(Weapon weapon, int level)
    {
        ShieldState state = weapon.GetState<ShieldState>();

        switch (level)
        {
            case 5:
                state.BlocksDamage = true;
                break;
            case 8:
                state.Slows = true;
                break;
            case 10:
                state.HasImmunity = true;
                state.ImmunityTimer = 0f;   // ready right away
                break;
        }
    }
}