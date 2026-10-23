using UnityEngine;

/// <summary>
/// Idle until the attack cooldown ends, then jump up off the screen. While it is away a landing
/// indicator shows where the player was standing, and after a moment the slime falls onto that spot.
///   Idle      - stands still, cooldown counting down.
///   Winding   - the first frames of the Launch animation play on the ground, then it leaves.
///   Rising    - moves up out of view.
///   Targeting - hidden and untouchable; the landing spot is locked in and the indicator shows.
///   Falling   - reappears above the landing spot and drops onto it.
///   Landing   - still falling, but the Land animation has started so its impact frame meets the ground.
/// The Animator is told what to do with the Launch and Land triggers; this behavior owns the timing.
/// </summary>
[CreateAssetMenu(fileName = "Slime", menuName = "Enemy Behaviors/Slime")]
public class EBSlime : EnemyBehavior
{
    /// <summary>Raised when the slime lands: where, and the slam's radius. Sound and camera shake listen to it.</summary>
    public static event System.Action<Vector3, float> OnSlam;

    /// <summary>Raised when a slime appears. The music listens, to switch to the boss track.</summary>
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
    [Tooltip("How far above the landing spot the slime rises to and falls from. Should be past the top edge of the screen.")]
    [SerializeField] private float jumpHeight = 12f;
    [SerializeField] private float riseTime = 0.6f;
    [SerializeField] private float fallSpeed = 30f;

    [Header("Animation")]
    [Tooltip("Frames per second of the Launch and Land animations.")]
    [SerializeField] private float animationFps = 12f;
    [Tooltip("How many Launch frames play on the ground before the slime leaves it.")]
    [SerializeField] private int launchFramesBeforeLeaving = 4;
    [Tooltip("Which frame of the Land animation is the moment of impact. The animation starts early enough that this frame meets the ground.")]
    [SerializeField] private int landImpactFrame = 4;

    [Header("Slam")]
    [Tooltip("How long the landing indicator shows before the slime falls.")]
    [SerializeField] private float indicatorTime = 1.5f;
    [Tooltip("The slam's damage is the slime's own Damage in its EnemyData.")]
    [SerializeField] private float slamRadius = 3f;
    [Tooltip("The landing animation: a projectile using the Effect behavior. It is sized to fit the slam's area automatically.")]
    [SerializeField] private ProjectileData landEffect;
    [Tooltip("Fine-tunes the landing effect's size. Raise it if the art has empty space around the shockwave, lower it if it overshoots.")]
    [SerializeField] private float landEffectScale = 1f;
    [Tooltip("A sprite object (a circle) shown where the slime will land. Scaled to the slam's size when it appears. It is created and destroyed per slam.")]
    [SerializeField] private GameObject indicatorPrefab;

    public override void OnInit(Enemy enemy)
    {
        OnSlimeSpawned?.Invoke();

        enemy.State = Idle;
        enemy.AttackTimer = enemy.Data.AttackCooldown;

        // A pooled slime may still be in its last life's animation or hold a trigger nobody used.
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

        // Start the Launch animation now. The slime stays on the ground until its first frames have played.
        enemy.Animator.ResetTrigger(LandTrigger);
        enemy.Animator.SetTrigger(LaunchTrigger);
        enemy.State = Winding;
        enemy.AttackTimer = launchFramesBeforeLeaving / animationFps;
    }

    private void TickWinding(Enemy enemy, float deltaTime)
    {
        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Can't be hurt from the moment it leaves the ground. The rest of the Launch animation plays while it rises.
        enemy.IsInvulnerable = true;
        enemy.State = Rising;
        enemy.AttackTimer = riseTime;
    }

    private void TickRising(Enemy enemy, float deltaTime, Vector3 playerPosition)
    {
        enemy.transform.position += Vector3.up * (jumpHeight / riseTime * deltaTime);

        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Off the screen: lock in the landing spot where the player stands right now.
        enemy.TargetPosition = playerPosition;
        enemy.SetVisible(false);
        enemy.State = Targeting;
        enemy.AttackTimer = indicatorTime;

        ShowIndicator(enemy);
    }

    // One slime exists at a time and slams every few seconds, so creating and destroying one object is fine; a pool would be overkill.
    private void ShowIndicator(Enemy enemy)
    {
        if (indicatorPrefab == null) return;

        enemy.RemoveMarker();
        enemy.Marker = Instantiate(indicatorPrefab, enemy.TargetPosition, Quaternion.identity);
        enemy.Marker.transform.localScale = Vector3.one * (slamRadius * 2f);   // assumes the circle sprite is 1 unit across
    }

    private void TickTargeting(Enemy enemy, float deltaTime)
    {
        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Reappear above the landing spot and drop onto it.
        enemy.transform.position = enemy.TargetPosition + Vector3.up * jumpHeight;
        enemy.SetVisible(true);
        enemy.State = Falling;
    }

    private void TickFalling(Enemy enemy, float deltaTime, Vector3 playerPosition)
    {
        enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, enemy.TargetPosition, fallSpeed * deltaTime);
        float sqrRemaining = (enemy.TargetPosition - enemy.transform.position).sqrMagnitude;

        // Start the Land animation early: the frames before the impact frame take (frame - 1) / fps seconds,
        // and the slime falls at a constant speed, so it is that time times the speed away from the ground.
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

    // The effect is a pooled projectile that only plays its animation, the same as the death effect.
    private void ShowLandEffect(Vector3 position)
    {
        if (landEffect == null) return;

        Projectile shot = ObjectPooler.Instance.ProjectilePool.Get();
        if (shot == null) return;   // pool is full: skipping a picture is better than breaking a weapon

        shot.Init(landEffect, position, Vector3.right, 0f);

        // Init resets the scale of a pooled object, so set it after. The sprite's size in the world depends on its
        // pixels per unit, so ask the sprite instead of guessing: scale it until it is as wide as the slam's circle.
        float spriteWidth = landEffect.Sprite != null ? landEffect.Sprite.bounds.size.x : 1f;
        shot.transform.localScale = Vector3.one * (slamRadius * 2f / spriteWidth * landEffectScale);
    }

    private void Land(Enemy enemy, Vector3 playerPosition)
    {
        // Circle distance check around the landing spot, squared so there is no square root.
        float sqrDistance = (playerPosition - enemy.TargetPosition).sqrMagnitude;
        float reach = slamRadius + Player.Instance.HitRadius;   // the circle touches the player's body, like every other hit
        if (sqrDistance <= reach * reach) Player.Instance.Health.TakeDamage(enemy.Data.Damage);

        enemy.RemoveMarker();
        ShowLandEffect(enemy.TargetPosition);
        OnSlam?.Invoke(enemy.TargetPosition, slamRadius);

        enemy.IsInvulnerable = false;
        enemy.State = Idle;
        enemy.AttackTimer = enemy.Data.AttackCooldown;
    }
}