using UnityEngine;

/// <summary>
/// Base class for how a projectile moves and hits. Tick returns true when it is done.
/// </summary>
public abstract class ProjectileBehavior : ScriptableObject
{
    public abstract bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition);
}
