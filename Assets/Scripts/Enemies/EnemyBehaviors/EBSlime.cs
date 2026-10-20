using UnityEngine;

/// <summary>
/// Idle until the attack cooldown ends, then jump up off the screen. While it is away a landing
/// indicator shows where the player was standing, and after a moment the slime falls onto that spot.
///   Idle      - stands still, cooldown counting down.
///   Winding   - the Launch animation has started, but the slime is still on the ground.
///   Rising    - moves up out of view.
///   Targeting - hidden and untouchable; the landing spot is locked in and the indicator shows.
///   Falling   - reappears above the landing spot and drops onto it.
///   Landing   - still falling, but the Land animation has started so its ground frame hits at impact.
/// Animator: "Launch" fires a few frames before it leaves the ground, "Land" fires shortly before impact.
/// Land -> Idle is an Animator transition on exit time, so no Idle trigger is needed.
/// </summary>
[CreateAssetMenu(fileName = "Slime", menuName = "Enemy Behaviors/Slime")]
public class EBSlime : EnemyBehavior
{
    // Values for Enemy.State.
    private const int Idle = 0;
    private const int Rising = 1;
    private const int Targeting = 2;
    private const int Falling = 3;
    private const int Landing = 4;
    private const int Winding = 5;

    private static readonly int LaunchHash = Animator.StringToHash("Launch");
    private static readonly int LandHash = Animator.StringToHash("Land");

    [Header("Jump")]
    [Tooltip("How far above the landing spot the slime rises to and falls from. Should be past the top edge of the screen.")]
    [SerializeField] private float jumpHeight = 12f;
    [SerializeField] private float riseTime = 0.6f;
    [SerializeField] private float fallSpeed = 30f;

    [Header("Launch Animation Sync")]
    [Tooltip("Sample rate (frames per second) of the Launch clip.")]
    [SerializeField] private float launchClipFps = 12f;
    [Tooltip("How many Launch frames play on the ground before the slime starts moving up.")]
    [SerializeField] private int launchWindupFrames = 4;

    [Header("Land Animation Sync")]
    [Tooltip("Sample rate (frames per second) of the Land clip. Check it in the Animation window.")]
    [SerializeField] private float landClipFps = 12f;
    [Tooltip("The Land animation frame (1 = first frame) on which the slime touches the ground.")]
    [SerializeField] private int groundFrame = 4;

    [Header("Slam")]
    [Tooltip("How long the landing indicator shows before the slime falls.")]
    [SerializeField] private float indicatorTime = 1.5f;
    [SerializeField] private float slamRadius = 3f;
    [SerializeField] private float slamDamage = 30f;

    public override void OnInit(Enemy enemy)
    {
        enemy.State = Idle;
        enemy.AttackTimer = enemy.Data.AttackCooldown;

        // Pooled enemy: clear triggers left over from a previous life.
        enemy.Animator.ResetTrigger(LaunchHash);
        enemy.Animator.ResetTrigger(LandHash);
    }

    public override void Tick(Enemy enemy, float deltaTime, Vector3 playerPosition)
    {
        switch (enemy.State)
        {
            case Idle: TickIdle(enemy, deltaTime); break;
            case Winding: TickWinding(enemy, deltaTime); break;
            case Rising: TickRising(enemy, deltaTime, playerPosition); break;
            case Targeting: TickTargeting(enemy, deltaTime); break;
            case Falling: TickFalling(enemy, deltaTime, playerPosition); break;
            case Landing: TickFalling(enemy, deltaTime, playerPosition); break;
        }
    }

    private void TickIdle(Enemy enemy, float deltaTime)
    {
        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Start the Launch animation now, but stay on the ground until its windup frames have played.
        enemy.Animator.ResetTrigger(LandHash);
        enemy.Animator.SetTrigger(LaunchHash);
        enemy.State = Winding;
        enemy.AttackTimer = launchWindupFrames / launchClipFps;
    }

    private void TickWinding(Enemy enemy, float deltaTime)
    {
        enemy.AttackTimer -= deltaTime;
        if (enemy.AttackTimer > 0f) return;

        // Windup finished: leave the ground. Can't be hurt from this moment.
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

        // TODO: show the landing indicator at enemy.TargetPosition, sized to slamRadius.
        // Needs an indicator object (a pooled prefab or its own script).
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
        // Start the Land animation early enough that its ground frame lines up with the impact.
        // Frame N starts (N - 1) / fps seconds into the clip, so that is how far ahead we start it.
        if (enemy.State == Falling)
        {
            float distanceLeft = (enemy.TargetPosition - enemy.transform.position).magnitude;
            float timeToImpact = distanceLeft / fallSpeed;
            float leadTime = (groundFrame - 1) / landClipFps;

            // If the fall is shorter than the lead time, this fires on the first falling frame.
            if (timeToImpact <= leadTime)
            {
                enemy.Animator.SetTrigger(LandHash);
                enemy.State = Landing;
            }
        }

        enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, enemy.TargetPosition, fallSpeed * deltaTime);
        if ((enemy.TargetPosition - enemy.transform.position).sqrMagnitude > 0.0001f) return;

        Land(enemy, playerPosition);
    }

    private void Land(Enemy enemy, Vector3 playerPosition)
    {
        // Circle overlap around the landing spot: slam radius plus the player's body radius. Squared, so no square root.
        float sqrDistance = (playerPosition - enemy.TargetPosition).sqrMagnitude;
        float reach = slamRadius + Player.Instance.HitRadius;
        if (sqrDistance <= reach * reach) Player.Instance.Health.TakeDamage(slamDamage);

        // TODO: hide the landing indicator.

        enemy.IsInvulnerable = false;
        enemy.State = Idle;
        enemy.AttackTimer = enemy.Data.AttackCooldown;
    }
}