using UnityEngine;
using System.Text;

/// <summary>
/// The Staff: aims at the closest enemy in range and shoots fireballs.
///   Level 2, 6   damage up          Level 5   fireball explodes on the last enemy
///   Level 3, 7   one more fireball  Level 8   fireball explodes on every enemy it hits
///   Level 4, 9   pierce up          Level 10  chance to spawn another fireball on explosion
/// </summary>
[CreateAssetMenu(fileName = "Staff", menuName = "Weapon Behaviors/Staff")]
public class WBStaff : WeaponBehavior
{
    // Weapon-specific state. It lives on the Weapon, never on this asset, which every Staff shares.
    // Public because PBFireball reads the Staff's unlocked rules from it.
    public class StaffState : WeaponState
    {
        public int Pierce;
        public bool ExplodeOnLast;
        public bool ExplodeOnHit;
        public float ChainChance;
    }

    [Header("Fireball")]
    [SerializeField] private ProjectileData projectile;
    [Tooltip("Angle in degrees between neighbouring fireballs when firing more than one.")]
    [SerializeField] private float spreadAngle = 12f;

    [Header("Upgrades")]
    [Tooltip("Damage up levels multiply damage by 1 plus this. 0.5 is +50%.")]
    [SerializeField] private float damageUp = 0.5f;
    [Tooltip("Extra enemies a fireball passes through, added at each pierce level.")]
    [SerializeField] private int pierceIncrease = 2;
    [SerializeField, Range(0f, 1f)] private float chainChance = 0.2f;

    public override WeaponState CreateState() => new StaffState();

    public override void Tick(Weapon weapon, float deltaTime)
    {
        weapon.TickCooldown(deltaTime);
        if (!weapon.IsReady) return;

        Player player = Player.Instance;
        if (player == null) return;

        // Closest enemy that can be hurt. No enemy in range: do nothing, so the Staff stays ready.
        Enemy target = EnemyManager.Instance.NearestEnemy(player.Position);
        if (target == null) return;

        Vector3 aim = target.transform.position - player.Position;
        if (aim.sqrMagnitude > weapon.Range * weapon.Range) return;

        Fire(weapon, player.Position, aim);
        weapon.StartCooldown();
        weapon.NotifyAttack();
    }

    private void Fire(Weapon weapon, Vector3 origin, Vector3 aim)
    {
        StaffState state = weapon.GetState<StaffState>();

        // Several fireballs fan out evenly around the aim direction.
        float firstAngle = -spreadAngle * (weapon.Count - 1) * 0.5f;

        for (int i = 0; i < weapon.Count; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, 0f, firstAngle + spreadAngle * i) * aim;

            Projectile shot = ObjectPooler.Instance.ProjectilePool.Get();
            shot.Init(projectile, origin, direction, weapon.Damage);

            // Pierce is copied because each shot uses it up as it flies. The explosion and chain rules
            // never change per shot, so PBFireball reads them from the Staff through Source.
            shot.Source = weapon;
            shot.PierceLeft = state.Pierce;
        }
    }

    // Debug only.
    public override void DescribeState(Weapon weapon, StringBuilder builder)
    {
        StaffState state = weapon.GetState<StaffState>();
        builder.Append("pierce ").Append(state.Pierce);
        if (state.ExplodeOnLast) builder.Append("  explode-last");
        if (state.ExplodeOnHit) builder.Append("  explode-all");
        if (state.ChainChance > 0f) builder.Append("  chain ").Append((state.ChainChance * 100f).ToString("0")).Append('%');
    }

    public override void StatUpgrade(Weapon weapon, int level)
    {
        switch (level)
        {
            case 2:
            case 6:
                weapon.Damage *= 1f + damageUp;
                break;
            case 3:
            case 7:
                weapon.Count++;
                break;
        }
    }

    public override void UniqueUpgrade(Weapon weapon, int level)
    {
        StaffState state = weapon.GetState<StaffState>();

        switch (level)
        {
            case 4:
            case 9:
                state.Pierce += pierceIncrease;
                break;
            case 5:
                state.ExplodeOnLast = true;
                break;
            case 8:
                state.ExplodeOnHit = true;
                break;
            case 10:
                state.ChainChance = chainChance;
                break;
        }
    }
}