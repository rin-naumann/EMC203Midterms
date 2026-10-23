using UnityEngine;

/// <summary>
/// Data for one weapon: its name, look and starting stats.
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
    [SerializeField] private float cooldown = 1f;
    [Tooltip("Staff: fireballs per attack. Sword: swings per attack. Shield: shields at once")]
    [SerializeField] private int count = 1;
    [Tooltip("Staff: targeting range. Sword: swing reach. Shield: orbit distance")]
    [SerializeField] private float range = 5f;

    [Header("Audio")]
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