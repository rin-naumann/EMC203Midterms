using UnityEngine;

/// <summary>Flies in a straight line in the direction it was fired. Used by arrows and energy orbs.</summary>
[CreateAssetMenu(fileName = "Basic", menuName = "Projectile Behaviors/Basic")]
public class PBBasic : ProjectileBehavior
{
    public override void Tick(Projectile projectile, float deltaTime, Vector3 targetPosition)
    {
        projectile.transform.position += projectile.Direction * (projectile.Data.Speed * deltaTime);
    }
}