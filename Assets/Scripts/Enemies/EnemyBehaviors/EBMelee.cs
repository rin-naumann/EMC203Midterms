using UnityEngine;

/// <summary>
/// Walks toward the player and attacks when close enough.
/// </summary>
[CreateAssetMenu(fileName = "Melee", menuName = "Enemy Behaviors/Melee")]
public class EBMelee : EnemyBehavior
{
    public override void Tick(Enemy enemy, float deltaTime, Vector3 targetPosition)
    {
        // The cooldown counts down while walking, so a hit is ready on arrival
        enemy.AttackTimer -= deltaTime;

        WalkToPlayer(enemy, deltaTime, targetPosition);
        AttackPlayer(enemy, targetPosition);
    }

    private void WalkToPlayer(Enemy enemy, float deltaTime, Vector3 targetPosition) => enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, targetPosition, enemy.MoveSpeed * deltaTime);

    private void AttackPlayer(Enemy enemy, Vector3 targetPosition)
    {
        if (enemy.AttackTimer > 0f) return;

        // Circle overlap: enemy radius plus the player's body radius
        float reach = enemy.Data.ContactRadius + Player.Instance.HitRadius;
        if ((enemy.transform.position - targetPosition).sqrMagnitude > reach * reach) return;

        enemy.AttackTimer = enemy.Data.AttackCooldown;
        Player.Instance.Health.TakeDamage(enemy.Data.Damage);
    }
}