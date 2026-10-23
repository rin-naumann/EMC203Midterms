using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// The in-game display: the HP bar (with its ghost), the EXP bar, the timer and the weapon slots. It listens to the
/// events the game already raises (health, EXP, weapons) and updates what is on screen; no gameplay script knows it exists.
/// It only shows things. The screens and buttons live in UIManager.
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
    [Tooltip("Everything the HUD shows. Hidden on the title screen, visible otherwise.")]
    [SerializeField] private GameObject hudRoot;

    [Header("HP bar")]
    [SerializeField] private Image hpFill;
    [Tooltip("Sits behind the HP fill. After a hit it drains down to the real HP over exactly the regeneration delay, so it reaches the real bar at the moment regeneration starts.")]
    [SerializeField] private Image hpGhost;
    [SerializeField] private TMP_Text hpText;

    [Header("EXP bar")]
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text levelText;
    [Tooltip("How quickly the bar catches up to the real EXP. Higher is snappier.")]
    [SerializeField] private float expLerpSpeed = 8f;

    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;

    [Header("Weapons display")]
    [Tooltip("One slot per weapon the player can own at once.")]
    [SerializeField] private WeaponSlot[] weaponSlots;

    private PlayerHealth health;
    private PlayerExperience experience;
    private WeaponManager weapons;

    // HP bar
    private float hpTarget = 1f;
    private float ghostShown = 1f;
    private float ghostFrom = 1f;   // where the ghost was when the current regeneration wait began
    private float lastHealth;
    private int lastHpShown = -1;
    private int lastMaxShown = -1;

    // EXP bar
    private float expTarget;
    private float expShown;
    private int wrapCount;       // level-ups whose "fill to the top" animation hasn't played yet
    private int lastLevelShown = -1;

    private bool snapBars;       // jump the bars to their real values instead of animating (new run, first frame)
    private int lastSecond = -1;

    // Static events outlive this object, so always unsubscribe in OnDisable.
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

    // The player and weapons exist by now, so their events are hooked here and the HUD is filled in once.
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
        // Unscaled time: the game is frozen (timeScale 0) during a pause and the level-up screen,
        // and the EXP bar should still finish its animation then.
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
        // The player's health and EXP reset in their own OnRunStarted handlers, which may run after this one,
        // so the bars jump to their new values on the next frame, once everything has settled.
        snapBars = true;
        lastSecond = -1;
        RefreshWeapons();
    }

    // ---- HP, EXP, timer ----

    private void HandleHealthChanged(float current, float max)
    {
        float previousTarget = hpTarget;
        hpTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Took damage: the regeneration wait starts over, so the ghost starts a new drain from where it is now,
        // or from the HP the player had before this hit, whichever is higher.
        if (current < lastHealth) ghostFrom = Mathf.Max(ghostShown, previousTarget);
        lastHealth = current;

        if (hpFill != null) hpFill.fillAmount = hpTarget;

        // The text only changes when the whole number does. Regeneration changes health every frame,
        // and rebuilding a string that often creates garbage for no visible change.
        if (hpText == null) return;
        int currentInt = Mathf.CeilToInt(current);
        int maxInt = Mathf.CeilToInt(max);
        if (currentInt == lastHpShown && maxInt == lastMaxShown) return;
        lastHpShown = currentInt;
        lastMaxShown = maxInt;
        hpText.text = currentInt + " / " + maxInt;
    }

    // A blocked hit restarts the regeneration wait too, so the ghost starts over from where it is now.
    private void HandleBlocked() => ghostFrom = ghostShown;

    private void HandleExpChanged(int current, int needed)
    {
        expTarget = needed > 0 ? (float)current / needed : 0f;
        UpdateLevelText();
    }

    private void HandleLevelUp(int level)
    {
        // The bar fills to the top first, then wraps to the leftover EXP; see AnimateBars.
        // (Opening the upgrade screen is the UIManager's job.)
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

        // Ghost: the regeneration wait made visible. It sits between where it started (ghostFrom) and the real HP,
        // in proportion to how much of the wait is left, so it reaches the real bar exactly when regeneration begins.
        // It reads the player's own timer, so it freezes during a pause and can't drift out of step with it.
        // With no wait running (full HP, or already regenerating) it simply follows the real bar.
        float wait = health != null ? health.SecondsUntilRegen : 0f;
        float delay = health != null ? health.RegenDelay : 0f;
        float gap = Mathf.Max(0f, ghostFrom - hpTarget);
        ghostShown = wait > 0f && delay > 0f ? hpTarget + gap * Mathf.Clamp01(wait / delay) : hpTarget;
        if (hpGhost != null) hpGhost.fillAmount = ghostShown;

        // EXP: eases toward its target. After a level-up the target is the top of the bar; once it gets
        // there the bar starts again from empty, so it never slides backwards across the screen.
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

        // Only rebuild the string when the second changes, not every frame.
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