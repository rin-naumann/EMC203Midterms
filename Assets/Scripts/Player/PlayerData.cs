using UnityEngine;

/// <summary>
/// Data for the player: speed, health, regeneration, EXP and pickup range.
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Player")]
public class PlayerData : ScriptableObject
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float hitInvulnerability = 0.5f;
    [SerializeField] private float hitRadius = 0.4f;

    [Header("Regeneration")]
    [SerializeField, Min(0f)] private float regenDelay = 5f;
    [SerializeField, Min(0f)] private float regenPerSecond = 2f;

    [Header("Experience")]
    [SerializeField] private int baseExpToLevel = 10;
    [SerializeField] private int expCostIncrease = 25;
    [SerializeField] private float pickupRadius = 2f;

    public float MoveSpeed => moveSpeed;
    public float MaxHealth => maxHealth;
    public float HitInvulnerability => hitInvulnerability;
    public float HitRadius => hitRadius;
    public float RegenDelay => regenDelay;
    public float RegenPerSecond => regenPerSecond;
    public int BaseExpToLevel => baseExpToLevel;
    public int ExpCostIncrease => expCostIncrease;
    public float PickupRadius => pickupRadius;
}