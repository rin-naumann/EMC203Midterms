using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Lives on the player. Owns every weapon, ticks them, and is the one place that adds or levels a
/// weapon. The level-up screen will call AddOrLevelUp; the UI listens to the events.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    public event Action<Weapon> OnWeaponAdded;
    public event Action<Weapon> OnWeaponLevelChanged;

    [Tooltip("Weapons the player owns at the start of every run.")]
    [SerializeField] private List<WeaponData> startingWeapons = new List<WeaponData>();

    private readonly List<Weapon> weapons = new List<Weapon>();
    public IReadOnlyList<Weapon> Weapons => weapons;

    private PlayerHealth health;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;

        // PlayerHealth knows nothing about weapons. It only offers a hook for anything that wants to
        // change incoming damage, and this is the one place that plugs the weapons into it.
        health = GetComponent<PlayerHealth>();
        if (health != null) health.DamageFilter = FilterPlayerDamage;
    }

    // Static events outlive this object, so always unsubscribe in OnDisable.
    private void OnEnable() => GameManager.OnRunStarted += ResetRun;
    private void OnDisable() => GameManager.OnRunStarted -= ResetRun;
    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        if (health != null) health.DamageFilter = null;
    }

    private void Update()
    {
        // Time.timeScale is 0 when paused, but a weapon that is ready would still fire once per
        // frame with a delta time of 0, so weapons are only ticked while playing.
        if (!GameManager.Instance.IsPlaying) return;

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < weapons.Count; i++) weapons[i].Tick(deltaTime);
    }

    // Passes the damage through every weapon in turn. Most weapons leave it unchanged.
    private float FilterPlayerDamage(float damage)
    {
        for (int i = 0; i < weapons.Count; i++)
            damage = weapons[i].Data.Behavior.FilterPlayerDamage(weapons[i], damage);
        return damage;
    }

    /// <summary>Gives the player the weapon, or raises it one level if they already have it.</summary>
    public Weapon AddOrLevelUp(WeaponData data)
    {
        Weapon owned = Find(data);
        if (owned == null) return AddWeapon(data);

        if (owned.LevelUp()) OnWeaponLevelChanged?.Invoke(owned);
        return owned;
    }

    public Weapon Find(WeaponData data)
    {
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i].Data == data) return weapons[i];
        return null;
    }

    private Weapon AddWeapon(WeaponData data)
    {
        if (data == null || data.Behavior == null)
        {
            Debug.LogError("A weapon needs WeaponData with a Behavior assigned.", this);
            return null;
        }

        Weapon weapon = new Weapon(data);
        weapons.Add(weapon);
        OnWeaponAdded?.Invoke(weapon);
        return weapon;
    }

    // New run: forget the old weapons and hand out the starting ones again.
    private void ResetRun()
    {
        weapons.Clear();
        for (int i = 0; i < startingWeapons.Count; i++) AddWeapon(startingWeapons[i]);
    }
}