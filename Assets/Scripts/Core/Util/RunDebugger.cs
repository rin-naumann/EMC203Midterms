using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// TEMPORARY. Test readout and debug keys. Delete it once testing is done.
///   N = skip forward   T = cycle game speed   K = kill the slime (tests the win)   X = kill every other enemy
///   L = level up every weapon   1, 2, 3 = give / level up that test weapon
///   E = add EXP   H = heal to full   J = hurt the player
/// The movement keys (W A S D) are left alone on purpose.
/// </summary>
public class RunDebugger : MonoBehaviour
{
    private static readonly float[] TimeScales = { 1f, 2f, 4f, 8f };

    [SerializeField] private float refreshInterval = 0.5f;
    [SerializeField] private float skipSeconds = 60f;
    [Tooltip("Keys 1, 2, 3... give the player that weapon, or raise it one level if they already have it.")]
    [SerializeField] private WeaponData[] testWeapons;
    [SerializeField] private int expAmount = 25;
    [SerializeField] private float hurtAmount = 10f;

    private readonly Dictionary<EnemyData, int> countsByType = new Dictionary<EnemyData, int>();
    private readonly StringBuilder builder = new StringBuilder(1024);
    private GUIStyle style;
    private string text = "";

    private int scaleIndex;
    private int frames;
    private float windowTime;
    private float worstFrame;

    private void Update()
    {
        HandleKeys();

        // Unscaled, so the numbers stay honest while the game speed is turned up.
        float frameTime = Time.unscaledDeltaTime;
        frames++;
        windowTime += frameTime;
        if (frameTime > worstFrame) worstFrame = frameTime;

        if (windowTime >= refreshInterval) Refresh();
    }

    private void HandleKeys()
    {
        if (Input.GetKeyDown(KeyCode.N)) GameManager.Instance.SkipTime(skipSeconds);
        if (Input.GetKeyDown(KeyCode.K)) KillSlime();
        if (Input.GetKeyDown(KeyCode.X)) KillOthers();
        if (Input.GetKeyDown(KeyCode.L)) LevelUpWeapons();
        if (Input.GetKeyDown(KeyCode.E)) Player.Instance?.Experience.AddExp(expAmount);
        if (Input.GetKeyDown(KeyCode.H)) Player.Instance?.Health.ResetHealth();
        if (Input.GetKeyDown(KeyCode.J)) Player.Instance?.Health.TakeDamage(hurtAmount);

        if (WeaponManager.Instance != null)
        {
            for (int i = 0; i < testWeapons.Length && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) WeaponManager.Instance.AddOrLevelUp(testWeapons[i]);
        }

        // Not while paused: changing timeScale then would un-freeze the game.
        if (Input.GetKeyDown(KeyCode.T) && GameManager.Instance.IsPlaying)
        {
            scaleIndex = (scaleIndex + 1) % TimeScales.Length;
            Time.timeScale = TimeScales[scaleIndex];
        }
    }

    private void LevelUpWeapons()
    {
        if (WeaponManager.Instance == null) return;

        IReadOnlyList<Weapon> weapons = WeaponManager.Instance.Weapons;
        for (int i = 0; i < weapons.Count; i++) WeaponManager.Instance.AddOrLevelUp(weapons[i].Data);
    }

    private void KillSlime()
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (!(enemies[i].Data.Behavior is EBSlime)) continue;

            // Killing removes it from the list, so stop looking right away.
            // Does nothing while the slime is in the air, because it can't be hurt then.
            enemies[i].TakeDamage(float.MaxValue);
            return;
        }
    }

    // Kills everything except the slime, so it also tests the drops. An enemy still inside its
    // short hit invulnerability survives, so press it twice if one is left.
    private void KillOthers()
    {
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i].Data.Behavior is EBSlime) continue;
            enemies[i].TakeDamage(float.MaxValue);
        }
    }

    // The text is rebuilt a couple of times a second, not every frame, so the readout itself
    // doesn't create garbage or skew the numbers it is measuring.
    private void Refresh()
    {
        builder.Clear();
        AppendRun();
        AppendPlayer();
        AppendWorld();
        AppendWeapons();
        builder.Append("[N] +").Append(skipSeconds).Append("s  [T] speed  [K] kill slime  [X] kill others\n");
        builder.Append("[L] level all weapons  [1-3] weapon  [E] +").Append(expAmount).Append(" EXP  [H] heal  [J] hurt ").Append(hurtAmount);

        text = builder.ToString();

        frames = 0;
        windowTime = 0f;
        worstFrame = 0f;
    }

    private void AppendRun()
    {
        int seconds = (int)GameManager.Instance.ElapsedTime;
        float fps = frames / windowTime;

        builder.Append("Run ").Append(seconds / 60).Append(':').Append((seconds % 60).ToString("00"));
        builder.Append("  ").Append(GameManager.Instance.CurrentState);
        builder.Append("   Speed x").Append(Time.timeScale);
        builder.Append("   FPS ").Append(fps.ToString("0"));
        builder.Append("   Worst ").Append((worstFrame * 1000f).ToString("0")).Append(" ms\n");
    }

    private void AppendPlayer()
    {
        Player player = Player.Instance;
        if (player == null) { builder.Append("NO PLAYER IN THE SCENE\n"); return; }

        PlayerHealth health = player.Health;
        PlayerExperience exp = player.Experience;

        builder.Append("HP ").Append(health.CurrentHealth.ToString("0")).Append('/').Append(health.MaxHealth.ToString("0"));
        if (health.IsInvulnerable) builder.Append(" (invulnerable)");
        if (health.CurrentHealth < health.MaxHealth)
            builder.Append(health.SecondsUntilRegen > 0f ? " regen in " + health.SecondsUntilRegen.ToString("0.0") + "s" : " regenerating");
        builder.Append("   Level ").Append(exp.Level);
        builder.Append("   EXP ").Append(exp.CurrentExp).Append('/').Append(exp.ExpToNextLevel);
        builder.Append("   Pending level-ups ").Append(exp.PendingLevelUps);
        builder.Append("   Pickup radius ").Append(player.PickupRadius.ToString("0.0")).Append('\n');
    }

    private void AppendWorld()
    {
        countsByType.Clear();
        IReadOnlyList<Enemy> enemies = EnemyManager.Instance.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData type = enemies[i].Data;
            if (type == null) continue;

            countsByType.TryGetValue(type, out int count);
            countsByType[type] = count + 1;
        }

        builder.Append("Enemies ").Append(enemies.Count);
        foreach (KeyValuePair<EnemyData, int> pair in countsByType)
            builder.Append("  ").Append(pair.Key.name).Append(' ').Append(pair.Value);

        builder.Append("   Projectiles ").Append(ProjectileManager.Instance.Projectiles.Count);
        if (DropManager.Instance != null) builder.Append("   Drops ").Append(DropManager.Instance.Drops.Count);
        builder.Append('\n');
    }

    private void AppendWeapons()
    {
        if (WeaponManager.Instance == null) { builder.Append("NO WEAPON MANAGER ON THE PLAYER\n"); return; }

        IReadOnlyList<Weapon> weapons = WeaponManager.Instance.Weapons;

        for (int i = 0; i < weapons.Count; i++)
        {
            Weapon weapon = weapons[i];
            int key = System.Array.IndexOf(testWeapons, weapon.Data);

            builder.Append(key >= 0 ? "[" + (key + 1) + "] " : "    ");
            builder.Append(weapon.Data.DisplayName).Append(" Lv ").Append(weapon.Level).Append('/').Append(weapon.Data.MaxLevel);
            builder.Append("  dmg ").Append(weapon.Damage.ToString("0.0"));
            builder.Append("  cd ").Append(weapon.Cooldown.ToString("0.00"));
            builder.Append("  count ").Append(weapon.Count);
            builder.Append("  range ").Append(weapon.Range.ToString("0.0"));
            builder.Append("  | ");
            weapon.Data.Behavior.DescribeState(weapon, builder);
            builder.Append('\n');
        }

        // Test weapons the player doesn't have yet, so the number keys are easy to find.
        for (int i = 0; i < testWeapons.Length; i++)
        {
            if (testWeapons[i] == null || WeaponManager.Instance.Find(testWeapons[i]) != null) continue;
            builder.Append('[').Append(i + 1).Append("] ").Append(testWeapons[i].DisplayName).Append(" - not owned, press ").Append(i + 1).Append(" to add\n");
        }
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            style.normal.textColor = Color.white;
        }

        // A dark backdrop so the text stays readable over any scene.
        Rect area = new Rect(6f, 6f, 1100f, 230f);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(area.x + 6f, area.y + 4f, area.width - 12f, area.height - 8f), text, style);
    }
}