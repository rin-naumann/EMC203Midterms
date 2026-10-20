using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class Projectile : MonoBehaviour, IPoolable
{
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private ProjectileData data;

    public ProjectileData Data => data;

    public float LifeTimer;
    public Vector3 Direction;
    public float Damage;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Init(ProjectileData projectileData, Vector3 position, Vector3 direction, float damage)
    {
        data = projectileData;
        Damage = damage;
        LifeTimer = data.LifeTime;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;

        transform.position = position;
        transform.right = Direction;

        spriteRenderer.sprite = data.Sprite;

        if (data.AnimatorController != null && animator.runtimeAnimatorController != data.AnimatorController)
            animator.runtimeAnimatorController = data.AnimatorController;
    }

    public void Tick(float deltaTime, Vector3 playerPosition)
    {
        bool done = data.Behavior.Tick(this, deltaTime, playerPosition);
        LifeTimer -= deltaTime;
        if (done || LifeTimer <= 0f) ObjectPooler.Instance.ProjectilePool.Release(this);
    }

    void IPoolable.OnSpawn() => ProjectileManager.Instance?.Register(this);
    void IPoolable.OnDespawn() => ProjectileManager.Instance?.Unregister(this);
}