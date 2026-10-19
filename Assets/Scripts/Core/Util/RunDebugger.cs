using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// TEMPORARY. Stress-test readout and debug keys. Delete it once the simulation is done.
///   N = skip forward   T = cycle game speed   K = kill the slime (tests the win)
/// </summary>
public class RunDebugger : MonoBehaviour
{
    private static readonly float[] TimeScales = { 1f, 2f, 4f, 8f };

    [SerializeField] private float refreshInterval = 0.5f;
    [SerializeField] private float skipSeconds = 60f;

    private readonly Dictionary<EnemyData, int> countsByType = new Dictionary<EnemyData, int>();
    private readonly StringBuilder builder = new StringBuilder(256);
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

        // Not while paused: changing timeScale then would un-freeze the game.
        if (Input.GetKeyDown(KeyCode.T) && GameManager.Instance.IsPlaying)
        {
            scaleIndex = (scaleIndex + 1) % TimeScales.Length;
            Time.timeScale = TimeScales[scaleIndex];
        }
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

    // The text is rebuilt a couple of times a second, not every frame, so the readout itself
    // doesn't create garbage or skew the numbers it is measuring.
    private void Refresh()
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

        int seconds = (int)GameManager.Instance.ElapsedTime;
        float fps = frames / windowTime;

        builder.Clear();
        builder.Append("Run time ").Append(seconds / 60).Append(':').Append((seconds % 60).ToString("00"));
        builder.Append("   Speed x").Append(Time.timeScale).Append('\n');
        builder.Append("FPS ").Append(fps.ToString("0")).Append("   Worst frame ").Append((worstFrame * 1000f).ToString("0")).Append(" ms\n");
        builder.Append("Enemies ").Append(enemies.Count);
        foreach (KeyValuePair<EnemyData, int> pair in countsByType)
            builder.Append("   ").Append(pair.Key.name).Append(' ').Append(pair.Value);
        builder.Append('\n');
        builder.Append("Projectiles ").Append(ProjectileManager.Instance.Projectiles.Count).Append('\n');
        builder.Append("[N] +").Append(skipSeconds).Append("s   [T] speed   [K] kill slime");

        text = builder.ToString();

        frames = 0;
        windowTime = 0f;
        worstFrame = 0f;
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            style.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(10f, 10f, 900f, 140f), text, style);
    }
}