using UnityEngine;

/// <summary>
/// One weapon the player owns. It is a plain C# class, not a MonoBehaviour: it needs no scene object
/// because the WeaponManager creates and ticks it. It holds the level and the live stats, and hands
/// every decision about HOW to attack to its behavior.
/// </summary>
public class Weapon
{
    public WeaponData Data { get; }
    public int Level { get; private set; }
    public bool IsMaxLevel => Level >= Data.MaxLevel;

    // Live stats. Copied from the data, then changed by the behavior's upgrade methods.
    public float Damage;
    public float Cooldown;
    public int Count;
    public float Range;

    // Per-weapon running state, the same idea as Enemy.AttackTimer and Enemy.State.
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

    // Cooldown helpers for weapons that attack on a timer (Staff, Sword).
    // The timer stops at 0, so a weapon with no target stays ready and fires the moment one appears.
    public bool IsReady => AttackTimer <= 0f;
    public void TickCooldown(float deltaTime) { if (AttackTimer > 0f) AttackTimer -= deltaTime; }
    public void StartCooldown() => AttackTimer = Cooldown;

    /// <summary>
    /// Behaviors call this when the weapon actually attacks (a shot, a swing, a new shield), so anything that
    /// reacts to attacks, like sound, doesn't need to know how each weapon works.
    /// </summary>
    public static event System.Action<Weapon> OnAttack;
    public void NotifyAttack() => OnAttack?.Invoke(this);
}