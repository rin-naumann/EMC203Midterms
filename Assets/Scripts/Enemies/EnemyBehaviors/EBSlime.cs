using UnityEngine;

/// <summary>
/// Jumps off the screen, then lands on the spot where the player was standing.
/// A landing indicator shows the spot before the slam.
/// </summary>
[CreateAssetMenu(fileName = "Slime", menuName = "Enemy Behaviors/Slime")]
public class EBSlime : EnemyBehavior
{
    /// <summary>Raised when the slime lands, with the position and radius.</summary>
    public static event System.Action<Vector3, float> OnSlam;

    /// <summary>Raised when a slime appears.</summary>
    public static event System.Action OnSlimeSpawned;

    // Values for Enemy.State.
    private const int Idle = 0;
    private const int Rising = 1;
    private const int Targeting = 2;
    private const int Falling = 3;
    private const int Winding = 4;
    private const int Landing = 5;

    private static readonly int LaunchTrigger = Animator.StringToHash("Launch");
    private static readonly int LandTrigger = Animator.StringToHash("Land");

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 12f;
    [SerializeField] private float riseTime = 0.6f;
    [SerializeField] private float fallSpeed = 30f;

    [Header("Animation")]
    [SerializeField] private float animationFps = 12f;
    [SerializeField] private int launchFramesBeforeLeaving = 4;
    [SerializeField] private int landImpactFrame = 4;

    [Header("Slam")]
    [SerializeField] private float indicatorTime = 1.5f;
    [Tooltip("Slam damage comes from the slime's EnemyData")]
    [SerializeField] private float slamRadius = 3f;
    [SerializeField] private ProjectileData landEffect;
    [SerializeField] private float landEffectScale = 1f;
    [SerializeField] private GameObject indicatorPrefab;

    public override void OnInit(Enemy enemy)
    {
        OnSlimeSpawned?.Invoke();

        enemy.State = Idle;
        enemy.AttackTimer = enemy.Data.AttackCooldown;

        // Reset animation left over from a previous life
        Animator animator = enemy.Animator;
        if (animator.runtimeAnimatorController == null) return;
        animator.Rebind();
        animator.ResetTrigger(LaunchTrigger);
        animator.ResetTrigger(LandTrigger);
    }

    public override void Tick(Enemy enemy, float deltaTime, Vector3 playerPosition)
    {
        switch (enemy.State)
        {
            case Idle: TickIdle(enemy, deltaTime); break;
            case Rising: TickRising(enemy, deltaTime, playerPosition); break;
            case Targeting: TickTargeting(enemy, deltaTime); break;
            case Falling:
            case Landing: TickFalling(enemy, deltaTime, playerPosition); break;
            case Winding: TickWinding(enemy, deltaTime); break;
        }
    }

    private void TickIdle(Enemy enemy, float deltaTime)
    {
        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Start the Launch animation, but stay on the ground for its first frames
        enemy.Animator.ResetTrigger(LandTrigger);
        enemy.Animator.SetTrigger(LaunchTrigger);
        enemy.State = Winding;
        enemy.AttackTimer = launchFramesBeforeLeaving / animationFps;
    }

    private void TickWinding(Enemy enemy, float deltaTime)
    {
        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Cannot be hurt once it leaves the ground
        enemy.IsInvulnerable = true;
        enemy.State = Rising;
        enemy.AttackTimer = riseTime;
    }

    private void TickRising(Enemy enemy, float deltaTime, Vector3 playerPosition)
    {
        enemy.transform.position += Vector3.up * (jumpHeight / riseTime * deltaTime);

        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Off screen: lock in the landing spot
        enemy.TargetPosition = playerPosition;
        enemy.SetVisible(false);
        enemy.State = Targeting;
        enemy.AttackTimer = indicatorTime;

        ShowIndicator(enemy);
    }

    // Only one slime exists, so creating and destroying the indicator is fine
    private void ShowIndicator(Enemy enemy)
    {
        if (indicatorPrefab == null) return;

        enemy.RemoveMarker();
        enemy.Marker = Instantiate(indicatorPrefab, enemy.TargetPosition, Quaternion.identity);
        enemy.Marker.transform.localScale = Vector3.one * (slamRadius * 2f);  // assumes the circle sprite is 1 unit across
    }

    private void TickTargeting(Enemy enemy, float deltaTime)
    {
        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Reappear above the landing spot and drop
        enemy.transform.position = enemy.TargetPosition + Vector3.up * jumpHeight;
        enemy.SetVisible(true);
        enemy.State = Falling;
    }

    private void TickFalling(Enemy enemy, float deltaTime, Vector3 playerPosition)
    {
        enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, enemy.TargetPosition, fallSpeed * deltaTime);
        float sqrRemaining = (enemy.TargetPosition - enemy.transform.position).sqrMagnitude;

        // Start the Land animation early so its impact frame meets the ground
        if (enemy.State == Falling)
        {
            float leadDistance = fallSpeed * (landImpactFrame - 1) / animationFps;
            if (sqrRemaining <= leadDistance * leadDistance)
            {
                enemy.Animator.ResetTrigger(LaunchTrigger);
                enemy.Animator.SetTrigger(LandTrigger);
                enemy.State = Landing;
            }
        }

        if (sqrRemaining > 0.0001f) return;

        Land(enemy, playerPosition);
    }

    // A pooled projectile that only plays its animation
    private void ShowLandEffect(Vector3 position)
    {
        if (landEffect == null) return;

        Projectile shot = ObjectPooler.Instance.ProjectilePool.Get();
        if (shot == null) return;  // pool is full, so skip the effect

        shot.Init(landEffect, position, Vector3.right, 0f);

        // Init resets the scale, so set it after, sized to the slam circle
        float spriteWidth = landEffect.Sprite != null ? landEffect.Sprite.bounds.size.x : 1f;
        shot.transform.localScale = Vector3.one * (slamRadius * 2f / spriteWidth * landEffectScale);
    }

    private void Land(Enemy enemy, Vector3 playerPosition)
    {
        // Damage the player if inside the slam circle
        float sqrDistance = (playerPosition - enemy.TargetPosition).sqrMagnitude;
        float reach = slamRadius + Player.Instance.HitRadius;  // the circle touches the player's body
        if (sqrDistance <= reach * reach) Player.Instance.Health.TakeDamage(enemy.Data.Damage);

        enemy.RemoveMarker();
        ShowLandEffect(enemy.TargetPosition);
        OnSlam?.Invoke(enemy.TargetPosition, slamRadius);

        enemy.IsInvulnerable = false;
        enemy.State = Idle;
        enemy.AttackTimer = enemy.Data.AttackCooldown;
    }
}