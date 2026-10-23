using UnityEngine;
using System.Text;

/// <summary>
/// Base class for how a weapon works. It is shared by all weapons using it, so it keeps no per-weapon data.
/// </summary>
public abstract class WeaponBehavior : ScriptableObject
{
    /// <summary>Creates the weapon-specific state, or null if none is needed.</summary>
    public virtual WeaponState CreateState() => null;

    /// <summary>Called every frame. Decides whether and how to attack.</summary>
    public abstract void Tick(Weapon weapon, float deltaTime);

    /// <summary>Called when the weapon reaches a level, for plain stat upgrades.</summary>
    public virtual void StatUpgrade(Weapon weapon, int level) { }

    /// <summary>Called when the weapon reaches a level, for special upgrades.</summary>
    public virtual void UniqueUpgrade(Weapon weapon, int level) { }

    /// <summary>Called when the player takes damage. Returns the damage to take (0 blocks it).</summary>
    public virtual float FilterPlayerDamage(Weapon weapon, float damage) => damage;

    /// <summary>Debug only: adds weapon details to the RunDebugger readout.</summary>
    public virtual void DescribeState(Weapon weapon, StringBuilder builder) { }
}

/// <summary>Base class for the state a behavior keeps on a Weapon.</summary>
public abstract class WeaponState { }