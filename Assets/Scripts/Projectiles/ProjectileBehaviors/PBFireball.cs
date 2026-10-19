using UnityEngine;

/// <summary>The Staff's fireball.</summary>
[CreateAssetMenu(fileName = "Fireball", menuName = "Projectile Behaviors/Fireball")]
public class PBFireball : ProjectileBehavior
{
    public override void Tick(Projectile projectile, float deltaTime, Vector3 targetPosition)
    {
        projectile.transform.position += projectile.Direction * (projectile.Data.Speed * deltaTime);

        // TODO (needs the Staff weapon script, which owns the level):
        //  - pierce (levels 4 and 9): how many enemies a shot passes through.
        //  - explode on the last enemy (level 5) and on every enemy hit (level 8).
        //  - 20% chance to spawn another fireball on explosion (level 10).
        // Each needs per-shot state on Projectile (pierce left, hit list) set from the weapon at Init.
    }
}