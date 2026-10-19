using UnityEngine;

/// <summary>
/// Melee attacks the player when close enough.
/// </summary>
[CreateAssetMenu(fileName = "Melee", menuName = "Enemy Behaviors/Melee")]
public class EBMelee : EnemyBehavior
{
    public override void Tick(Enemy enemy, float deltaTime, Vector3 targetPosition)
    {
        // The cooldown keeps counting while the enemy walks, so a hit is ready on arrival.
        enemy.AttackTimer -= deltaTime;

        WalkToPlayer(enemy, deltaTime, targetPosition);
        AttackPlayer(enemy, targetPosition);
    }

    private void WalkToPlayer(Enemy enemy, float deltaTime, Vector3 targetPosition) => enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, targetPosition, enemy.Data.MoveSpeed * deltaTime);

    private void AttackPlayer(Enemy enemy, Vector3 targetPosition)
    {
        float sqrDistance = (enemy.transform.position - targetPosition).sqrMagnitude;
        float contactRange = enemy.Data.ContactRadius * enemy.Data.ContactRadius;

        if (sqrDistance > contactRange || enemy.AttackTimer > 0f) return;
        enemy.AttackTimer = enemy.Data.AttackCooldown;

        // TODO: damage the player with enemy.Data.Damage. Needs a player health script that
        // implements IDamageable, plus a way for enemies to reach it.
    }
}