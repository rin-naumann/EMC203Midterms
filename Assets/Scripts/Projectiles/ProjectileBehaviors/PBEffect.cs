using UnityEngine;

/// <summary>
/// Plays an animation and deals no damage. Can follow the player.
/// </summary>
[CreateAssetMenu(fileName = "Effect", menuName = "Projectile Behaviors/Effect")]
public class PBEffect : ProjectileBehavior
{
    [SerializeField] private bool followPlayer = false;

    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        if (followPlayer) projectile.transform.position = playerPosition + projectile.PlayerOffset;
        return false;
    }
}