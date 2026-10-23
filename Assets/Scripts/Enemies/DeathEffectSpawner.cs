using UnityEngine;

/// <summary>
/// Shows a death effect where an enemy dies.
/// Uses a pooled projectile that only plays its animation.
/// </summary>
public class DeathEffectSpawner : MonoBehaviour
{
    // Unsubscribe so the static event does not keep this object alive
    private void OnEnable() => Enemy.OnEnemyDeath += ShowEffect;
    private void OnDisable() => Enemy.OnEnemyDeath -= ShowEffect;

    private void ShowEffect(Enemy enemy)
    {
        ProjectileData effect = enemy.Data.DeathEffect;
        if (effect == null) return;

        Projectile shot = ObjectPooler.Instance.ProjectilePool.Get();
        if (shot == null) return;  // pool is full, so skip the effect

        shot.Init(effect, enemy.transform.position, Vector3.right, 0f);

        // Init resets the scale, so set it after
        shot.transform.localScale = Vector3.one * enemy.Data.DeathEffectScale;
    }
}