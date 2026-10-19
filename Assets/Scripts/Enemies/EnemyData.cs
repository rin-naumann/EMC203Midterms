using UnityEngine;
using UnityEngine.Serialization;

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
    [SerializeField] private int xpDrop;

    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public EnemyBehavior Behavior => behavior;
    public float MaxHealth => maxHealth;
    public float AttackCooldown => attackCooldown;
    public float Damage => damage;
    public float MoveSpeed => moveSpeed;
    public float ContactRadius => contactRadius;
    public int XpDrop => xpDrop;
}