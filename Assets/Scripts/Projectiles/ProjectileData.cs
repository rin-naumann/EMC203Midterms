using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Projectile")]
public class ProjectileData : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private ProjectileBehavior behavior;
    [SerializeField] private float spriteFacing = 0f;
    [SerializeField] private bool endsWithAnimation;

    [Header("Stats")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float hitRadius = 0.25f;

    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public ProjectileBehavior Behavior => behavior;
    public float SpriteFacing => spriteFacing;
    public bool EndsWithAnimation => endsWithAnimation;

    public float Speed => speed;
    public float Damage => damage;
    public float LifeTime => lifeTime;
    public float HitRadius => hitRadius;
}