using UnityEngine;

/// <summary>
/// Shows an enemy's death effect where it dies. Enemy only announces its death and knows nothing about
/// effects or drops; this is the one place that connects the announcement to the effect.
/// The effect is a pooled projectile that only plays its animation, so it needs no new pool or manager.
/// </summary>
public class DeathEffectSpawner : MonoBehaviour
{
    // Static events outlive this object, so always unsubscribe in OnDisable.
    private void OnEnable() => Enemy.OnEnemyDeath += ShowEffect;
    private void OnDisable() => Enemy.OnEnemyDeath -= ShowEffect;

    private void ShowEffect(Enemy enemy)
    {
        ProjectileData effect = enemy.Data.DeathEffect;
        if (effect == null) return;

        Projectile shot = ObjectPooler.Instance.ProjectilePool.Get();
        if (shot == null) return;   // pool is full: skipping a picture is better than breaking a weapon

        shot.Init(effect, enemy.transform.position, Vector3.right, 0f);

        // Init resets the scale of a pooled object, so set it after.
        shot.transform.localScale = Vector3.one * enemy.Data.DeathEffectScale;
    }
}