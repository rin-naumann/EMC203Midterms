using UnityEngine;

public abstract class ProjectileBehavior : ScriptableObject
{
    public abstract bool Tick(Projectile projectile, float deltaTime, Vector3 playerPosition);
}
