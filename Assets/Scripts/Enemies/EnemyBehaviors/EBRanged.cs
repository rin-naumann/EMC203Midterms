using UnityEngine;

/// <summary>
/// Walks toward the player until it is within half of attackRange, and shoots whenever the player
/// is inside attackRange. Between half range and full range it does both: it walks and shoots.
/// </summary>
[CreateAssetMenu(fileName = "Ranged", menuName = "Enemy Behaviors/Ranged")]
public class EBRanged : EnemyBehavior
{
    private static readonly int IsStoppedHash = Animator.StringToHash("IsStopped");

    [SerializeField] private float attackRange = 5f;
    [SerializeField] private ProjectileData projectile;

    // UTIL: A random head start on the cooldown, so enemies spawned together don't all fire on the same frame.
    public override void OnInit(Enemy enemy)
    {
        enemy.AttackTimer = Random.value * enemy.Data.AttackCooldown;
        enemy.Animator.SetBool(IsStoppedHash, false);
    }

    public override void Tick(Enemy enemy, float deltaTime, Vector3 playerPosition)
    {
        enemy.AttackTimer -= deltaTime;

        float sqrDistance = (enemy.transform.position - playerPosition).sqrMagnitude;
        float stopDistance = attackRange * 0.5f;

        bool stopped = sqrDistance <= stopDistance * stopDistance;
        enemy.Animator.SetBool(IsStoppedHash, stopped);

        if (!stopped) WalkToPlayer(enemy, deltaTime, playerPosition);
        if (sqrDistance <= attackRange * attackRange) AttackPlayer(enemy, playerPosition);
    }

    private void WalkToPlayer(Enemy enemy, float deltaTime, Vector3 playerPosition) => enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, playerPosition, enemy.Data.MoveSpeed * deltaTime);

    private void AttackPlayer(Enemy enemy, Vector3 playerPosition)
    {
        if (enemy.AttackTimer > 0f) return;
        enemy.AttackTimer = enemy.Data.AttackCooldown;

        Vector3 direction = playerPosition - enemy.transform.position;
        Projectile shot = ObjectPooler.Instance.ProjectilePool.Get();
        shot.Init(projectile, enemy.transform.position, direction, projectile.Damage);
    }
}