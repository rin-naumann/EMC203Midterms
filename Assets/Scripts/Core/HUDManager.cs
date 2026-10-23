using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// The in-game display: HP bar with a ghost bar, EXP bar, timer and weapon slots.
/// Listens to game events and only shows things. Buttons and screens are in UIManager.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSlot
    {
        public GameObject root;
        public Image icon;
        public TMP_Text levelText;
    }

    [Header("HUD")]
    [SerializeField] private GameObject hudRoot;

    [Header("HP bar")]
    [SerializeField] private Image hpFill;
    [Tooltip("Drains down to the real HP over the regen delay")]
    [SerializeField] private Image hpGhost;
    [SerializeField] private TMP_Text hpText;

    [Header("EXP bar")]
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private float expLerpSpeed = 8f;

    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;

    [Header("Weapons display")]
    [SerializeField] private WeaponSlot[] weaponSlots;

    private PlayerHealth health;
    private PlayerExperience experience;
    private WeaponManager weapons;

    // HP bar
    private float hpTarget = 1f;
    private float ghostShown = 1f;
    private float ghostFrom = 1f;  // ghost position when the regen wait began
    private float lastHealth;
    private int lastHpShown = -1;
    private int lastMaxShown = -1;

    // EXP bar
    private float expTarget;
    private float expShown;
    private int wrapCount;  // level-ups still waiting to animate
    private int lastLevelShown = -1;

    private bool snapBars;  // jump bars to real values instead of animating
    private int lastSecond = -1;

    // Unsubscribe so the static events do not keep this object alive
    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleState;
        GameManager.OnRunStarted += HandleRunStarted;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleState;
        GameManager.OnRunStarted -= HandleRunStarted;
    }

    // Hooked here because the player and weapons exist by now
    private void Start()
    {
        Player player = Player.Instance;
        if (player != null)
        {
            health = player.Health;
            experience = player.Experience;
            health.OnHealthChanged += HandleHealthChanged;
            health.OnBlocked += HandleBlocked;
            experience.OnExpChanged += HandleExpChanged;
            experience.OnLevelUp += HandleLevelUp;
        }

        weapons = WeaponManager.Instance;
        if (weapons != null)
        {
            weapons.OnWeaponAdded += HandleWeaponChanged;
            weapons.OnWeaponLevelChanged += HandleWeaponChanged;
        }

        snapBars = true;
        if (health != null) HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
        if (experience != null) HandleExpChanged(experience.CurrentExp, experience.ExpToNextLevel);
        RefreshWeapons();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnBlocked -= HandleBlocked;
        }
        if (experience != null)
        {
            experience.OnExpChanged -= HandleExpChanged;
            experience.OnLevelUp -= HandleLevelUp;
        }
        if (weapons != null)
        {
            weapons.OnWeaponAdded -= HandleWeaponChanged;
            weapons.OnWeaponLevelChanged -= HandleWeaponChanged;
        }
    }

    private void Update()
    {
        // Unscaled time, so the bars finish animating while paused
        AnimateBars(Time.unscaledDeltaTime);
        UpdateTimer();
    }

    // ---- Visibility and new runs ----

    private void HandleState(GameState state)
    {
        if (hudRoot != null) hudRoot.SetActive(state != GameState.Title);
    }

    private void HandleRunStarted()
    {
        // Bars jump to the new values next frame, after health and EXP reset
        snapBars = true;
        lastSecond = -1;
        RefreshWeapons();
    }

    // ---- HP, EXP, timer ----

    private void HandleHealthChanged(float current, float max)
    {
        float previousTarget = hpTarget;
        hpTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Took damage: the ghost starts a new drain from its highest point
        if (current < lastHealth) ghostFrom = Mathf.Max(ghostShown, previousTarget);
        lastHealth = current;

        if (hpFill != null) hpFill.fillAmount = hpTarget;

        // Only rebuild the text when the number changes
        if (hpText == null) return;
        int currentInt = Mathf.CeilToInt(current);
        int maxInt = Mathf.CeilToInt(max);
        if (currentInt == lastHpShown && maxInt == lastMaxShown) return;
        lastHpShown = currentInt;
        lastMaxShown = maxInt;
        hpText.text = currentInt + " / " + maxInt;
    }

    // A blocked hit also restarts the regen wait
    private void HandleBlocked() => ghostFrom = ghostShown;

    private void HandleExpChanged(int current, int needed)
    {
        expTarget = needed > 0 ? (float)current / needed : 0f;
        UpdateLevelText();
    }

    private void HandleLevelUp(int level)
    {
        // The bar fills to the top first, then wraps around
        wrapCount++;
        UpdateLevelText();
    }

    private void UpdateLevelText()
    {
        if (levelText == null || experience == null || experience.Level == lastLevelShown) return;
        lastLevelShown = experience.Level;
        levelText.text = "Lv " + lastLevelShown;
    }

    private void AnimateBars(float deltaTime)
    {
        if (snapBars)
        {
            snapBars = false;
            ghostShown = hpTarget;
            ghostFrom = hpTarget;
            expShown = expTarget;
            wrapCount = 0;
        }

        // Ghost: sits between its start and the real HP, based on the regen wait left
        float wait = health != null ? health.SecondsUntilRegen : 0f;
        float delay = health != null ? health.RegenDelay : 0f;
        float gap = Mathf.Max(0f, ghostFrom - hpTarget);
        ghostShown = wait > 0f && delay > 0f ? hpTarget + gap * Mathf.Clamp01(wait / delay) : hpTarget;
        if (hpGhost != null) hpGhost.fillAmount = ghostShown;

        // EXP: eases toward the target, wrapping to empty after a level-up
        float goal = wrapCount > 0 ? 1f : expTarget;
        expShown = Mathf.Lerp(expShown, goal, 1f - Mathf.Exp(-expLerpSpeed * deltaTime));
        if (wrapCount > 0 && expShown >= 0.99f)
        {
            expShown = 0f;
            wrapCount--;
        }
        if (expFill != null) expFill.fillAmount = expShown;
    }

    private void UpdateTimer()
    {
        if (timerText == null || GameManager.Instance == null) return;

        // Only rebuild the text when the second changes
        int seconds = Mathf.FloorToInt(GameManager.Instance.ElapsedTime);
        if (seconds == lastSecond) return;
        lastSecond = seconds;
        timerText.text = FormatTime(seconds);
    }

    private static string FormatTime(int totalSeconds) => (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");

    // ---- Weapons display ----

    private void HandleWeaponChanged(Weapon weapon) => RefreshWeapons();

    private void RefreshWeapons()
    {
        IReadOnlyList<Weapon> owned = WeaponManager.Instance != null ? WeaponManager.Instance.Weapons : null;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            WeaponSlot slot = weaponSlots[i];
            bool hasWeapon = owned != null && i < owned.Count;

            if (slot.root != null) slot.root.SetActive(hasWeapon);
            if (!hasWeapon) continue;

            Weapon weapon = owned[i];
            if (slot.icon != null) slot.icon.sprite = weapon.Data.Sprite;
            if (slot.levelText != null) slot.levelText.text = weapon.IsMaxLevel ? "MAX" : "Lv " + weapon.Level;
        }
    }
}