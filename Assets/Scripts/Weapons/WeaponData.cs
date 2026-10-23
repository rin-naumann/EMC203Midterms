using UnityEngine;

/// <summary>
/// What a weapon IS: its name, look and starting stats. Read-only at runtime. How it works lives in
/// its WeaponBehavior, and the live numbers (which change as it levels up) live on the Weapon instance.
/// </summary>
[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite sprite;
    [SerializeField] private WeaponBehavior behavior;

    [Header("Stats")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private float damage = 10f;
    [Tooltip("Seconds between attacks.")]
    [SerializeField] private float cooldown = 1f;
    [Tooltip("Staff: fireballs per attack. Sword: swings per attack. Shield: shields at once.")]
    [SerializeField] private int count = 1;
    [Tooltip("Staff: how far it can target. Sword: how far the swing reaches. Shield: orbit distance from the player.")]
    [SerializeField] private float range = 5f;

    [Header("Audio")]
    [Tooltip("Played each time the weapon attacks: a shot for the Staff, a swing for the Sword, a new shield for the Shield.")]
    [SerializeField] private SoundEffect attackSound;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Sprite => sprite;
    public WeaponBehavior Behavior => behavior;
    public SoundEffect AttackSound => attackSound;

    public int MaxLevel => maxLevel;
    public float Damage => damage;
    public float Cooldown => cooldown;
    public int Count => count;
    public float Range => range;
}