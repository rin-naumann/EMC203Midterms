using UnityEngine;
using System.Text;

/// <summary>
/// HOW a weapon works. Like EnemyBehavior and ProjectileBehavior, it is a ScriptableObject that is
/// shared by everything using it, so it must stay stateless. Anything that changes per weapon lives on
/// the Weapon: the shared stats directly, and weapon-specific ones in the WeaponState it creates.
/// </summary>
public abstract class WeaponBehavior : ScriptableObject
{
    /// <summary>Creates the weapon-specific state (pierce, explode flags...). Null if it needs none.</summary>
    public virtual WeaponState CreateState() => null;

    /// <summary>Called every frame. This is where the weapon decides whether and how to attack.</summary>
    public abstract void Tick(Weapon weapon, float deltaTime);

    /// <summary>Called when the weapon reaches a level. For the plain "UP" levels (damage, speed, count, range).</summary>
    public virtual void StatUpgrade(Weapon weapon, int level) { }

    /// <summary>Called when the weapon reaches a level. For the special levels (pierce, explosion, new swings...).</summary>
    public virtual void UniqueUpgrade(Weapon weapon, int level) { }

    /// <summary>
    /// Called when the player is about to take damage. Return the damage that should actually go through,
    /// 0 to block it completely. Only weapons that protect the player (the Shield) override this.
    /// </summary>
    public virtual float FilterPlayerDamage(Weapon weapon, float damage) => damage;

    /// <summary>Debug only: adds the weapon-specific numbers (unlocked upgrades, state) to the RunDebugger's readout.</summary>
    public virtual void DescribeState(Weapon weapon, StringBuilder builder) { }
}

/// <summary>Base class for the per-weapon state a behavior keeps on a Weapon. Each behavior defines its own.</summary>
public abstract class WeaponState { }