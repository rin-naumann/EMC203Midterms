using UnityEngine;

public abstract class ProjectileBehavior : ScriptableObject
{
    public abstract void Tick(Projectile projectile, float deltaTime, Vector3 targetPosition);
}
