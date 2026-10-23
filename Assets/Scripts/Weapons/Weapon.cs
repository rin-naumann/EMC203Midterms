using UnityEngine;

/// <summary>
/// One weapon the player owns. Holds its level and live stats.
/// How it attacks is decided by its WeaponBehavior.
/// </summary>
public class Weapon
{
    public WeaponData Data { get; }
    public int Level { get; private set; }
    public bool IsMaxLevel => Level >= Data.MaxLevel;

    // Live stats, copied from the data and changed by upgrades
    public float Damage;
    public float Cooldown;
    public int Count;
    public float Range;

    // Running state for this weapon
    public float AttackTimer;
    public WeaponState State { get; private set; }

    public Weapon(WeaponData data)
    {
        Data = data;
        Reset();
    }

    /// <summary>Back to level 1 with the data's starting stats.</summary>
    public void Reset()
    {
        Level = 1;
        Damage = Data.Damage;
        Cooldown = Data.Cooldown;
        Count = Data.Count;
        Range = Data.Range;
        AttackTimer = 0f;
        State = Data.Behavior.CreateState();
    }

    public void Tick(float deltaTime) => Data.Behavior.Tick(this, deltaTime);

    /// <summary>Raises the level by one and applies that level's changes. False if already at max.</summary>
    public bool LevelUp()
    {
        if (IsMaxLevel) return false;

        Level++;
        Data.Behavior.StatUpgrade(this, Level);
        Data.Behavior.UniqueUpgrade(this, Level);
        return true;
    }

    /// <summary>The weapon-specific state, as the type its behavior created.</summary>
    public T GetState<T>() where T : WeaponState => (T)State;

    // Cooldown helpers for weapons that attack on a timer
    public bool IsReady => AttackTimer <= 0f;
    public void TickCooldown(float deltaTime) { if (AttackTimer > 0f) AttackTimer -= deltaTime; }
    public void StartCooldown() => AttackTimer = Cooldown;

    /// <summary>Behaviors call this when the weapon attacks, so things like sound can react.</summary>
    public static event System.Action<Weapon> OnAttack;
    public void NotifyAttack() => OnAttack?.Invoke(this);
}