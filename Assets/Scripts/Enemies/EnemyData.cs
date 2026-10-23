using UnityEngine;

/// <summary>
/// Data for one type of enemy: its look, behavior, stats, death effect and drops.
/// </summary>
[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private EnemyBehavior behavior;

    [Header("Stats")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float damage;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float contactRadius;
    [SerializeField] private float moveSpeed;
    [Tooltip("Seconds the enemy is red and cannot be hurt again after a hit")]
    [SerializeField] private float hitInvulnerability = 0.1f;

    [Header("Death")]
    [SerializeField] private ProjectileData deathEffect;
    [SerializeField] private float deathEffectScale = 1f;

    [Header("Drops")]
    [SerializeField] private DropEntry[] drops;

    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public EnemyBehavior Behavior => behavior;
    public float MaxHealth => maxHealth;
    public float AttackCooldown => attackCooldown;
    public float Damage => damage;
    public float MoveSpeed => moveSpeed;
    public float ContactRadius => contactRadius;
    public float HitInvulnerability => hitInvulnerability;
    public DropEntry[] Drops => drops;
    public ProjectileData DeathEffect => deathEffect;
    public float DeathEffectScale => deathEffectScale;
}

/// <summary>One line of an enemy's drop table: what can drop and its chance.</summary>
[System.Serializable]
public class DropEntry
{
    [SerializeField] private DropData drop;
    [Tooltip("Chance to drop (1 = always, 0.1 = 10%)")]
    [SerializeField, Range(0f, 1f)] private float chance = 1f;

    public DropData Drop => drop;
    public float Chance => chance;
}