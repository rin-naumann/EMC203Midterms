using UnityEngine;

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
    [Tooltip("Seconds after taking a hit during which the enemy is red and can't be hurt again. Keep it shorter than the Sword's swing interval.")]
    [SerializeField] private float hitInvulnerability = 0.1f;

    [Header("Death")]
    [Tooltip("The effect shown where the enemy dies: a projectile using the Effect behavior. Leave empty for none.")]
    [SerializeField] private ProjectileData deathEffect;
    [Tooltip("Size of the effect. Use a bigger number for bigger enemies.")]
    [SerializeField] private float deathEffectScale = 1f;

    [Header("Drops")]
    [Tooltip("Everything this enemy can drop when it dies. Each entry rolls its own chance. Leave empty for no drops (the slime).")]
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

/// <summary>One line of an enemy's drop table: what can drop, and how likely it is.</summary>
[System.Serializable]
public class DropEntry
{
    [SerializeField] private DropData drop;
    [Tooltip("Chance to drop when the enemy dies. 1 is always, 0.1 is 10%.")]
    [SerializeField, Range(0f, 1f)] private float chance = 1f;

    public DropData Drop => drop;
    public float Chance => chance;
}