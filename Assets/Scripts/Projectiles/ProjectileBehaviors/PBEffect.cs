using UnityEngine;

[CreateAssetMenu(fileName = "Effect", menuName = "Projectile Behaviors/Effect")]
public class PBEffect : ProjectileBehavior
{
    [Tooltip("Stay at the same spot relative to the player while it plays, instead of staying where it spawned.")]
    [SerializeField] private bool followPlayer = false;

    public override bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition)
    {
        if (followPlayer) projectile.transform.position = playerPosition + projectile.PlayerOffset;
        return false;
    }
}