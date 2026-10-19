using UnityEngine;

public abstract class EnemyBehavior : ScriptableObject
{
    public virtual void OnInit (Enemy enemy) { }
    public abstract void Tick (Enemy enemy, float deltaTime, Vector3 playerPosition);
}
