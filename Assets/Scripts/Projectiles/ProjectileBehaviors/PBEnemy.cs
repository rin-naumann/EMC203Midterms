using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Projectile Behaviors/Enemy")]
public class PBEnemy : ProjectileBehavior
{
    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        projectile.transform.position += projectile.Direction * (projectile.Data.Speed * deltaTime);

        float reach = projectile.Data.HitRadius + Player.Instance.HitRadius;
        if ((playerPosition - projectile.transform.position).sqrMagnitude > reach * reach) return false;

        Player.Instance.Health.TakeDamage(projectile.Damage);
        return true;
    }
}