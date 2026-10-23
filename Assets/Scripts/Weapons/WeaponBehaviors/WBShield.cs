using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// The Shield weapon. Shields orbit the player and damage enemies they touch.
/// A new shield appears each cooldown until the limit is reached.
/// Higher levels add more shields, slow enemies, reduce damage taken and block a hit.
/// </summary>
[CreateAssetMenu(fileName = "Shield", menuName = "Weapon Behaviors/Shield")]
public class WBShield : WeaponBehavior
{
    // Weapon-specific state, kept on the Weapon (public because PBShield reads it)
    public class ShieldState : WeaponState
    {
        public readonly List<Projectile> Shields = new List<Projectile>();
        public float Angle;  // degrees the ring has turned
        public float RotationSpeed;      // degrees per second

        public bool BlocksDamage;        // level 5
        public bool Slows;               // level 8: PBShield slows the enemies it hits
        public bool HasImmunity;         // level 10
        public float ImmunityTimer;      // time until the immunity is ready again
    }

    [Header("Shield")]
    [SerializeField] private ProjectileData shield;
    [SerializeField] private float baseRotationSpeed = 120f;

    [Header("Upgrades")]
    [Tooltip("Damage multiplier per damage up level is 1 plus this (0.5 = +50%)")]
    [SerializeField] private float damageUp = 0.5f;
    [SerializeField] private float rotationUp = 0.5f;
    [Tooltip("Level 5: fraction of damage removed when a shield is lost (0.3 = 30%)")]
    [SerializeField, Range(0f, 1f)] private float damageReduction = 0.3f;
    [Tooltip("Level 10: seconds until the immunity is ready again")]
    [SerializeField] private float immunityInterval = 10f;

    public override WeaponState CreateState() => new ShieldState { RotationSpeed = baseRotationSpeed };

    public override void Tick(Weapon weapon, float deltaTime)
    {
        Player player = Player.Instance;
        if (player == null) return;

        ShieldState state = weapon.GetState<ShieldState>();

        weapon.TickCooldown(deltaTime);
        if (state.ImmunityTimer > 0f) state.ImmunityTimer -= deltaTime;

        // Make a new shield when below the count and ready
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

        // PBShield reads damage and Slows from the Source, so upgrades reach existing shields
        piece.Source = weapon;

        // Shields never expire, they are only removed when lost
        piece.LifeTimer = float.MaxValue;
        state.Shields.Add(piece);
    }

    // Spreads the shields evenly around a circle
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

    // Immunity blocks a hit completely. Otherwise a shield is lost and damage is reduced
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

            // The replacement takes a full cooldown
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
                weapon.Count = 4;
                break;
            case 4:
            case 9:
                state.RotationSpeed *= 1f + rotationUp;
                break;
            case 7:
                weapon.Count = 6;
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
                state.ImmunityTimer = 0f;  // ready right away
                break;
        }
    }
}