using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// The game's screens and what their buttons do: title, pause, win / game over, and the level-up choice. It listens
/// to the state changes the game already raises and shows the matching screen; no gameplay script knows it exists.
/// The only things it does to the game are what a player's button press does: change the state, or pick an upgrade.
/// The in-game display (bars, timer, weapon slots) is the HUDManager's job.
/// </summary>
public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelUpCard
    {
        public Button button;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text descriptionText;
    }

    [Header("Screens")]
    [SerializeField] private GameObject titlePanel;
    [Tooltip("The panel shared by the pause menu and the end menu. It is shown for both; the two groups below decide which one.")]
    [SerializeField] private GameObject menuPanel;
    [Tooltip("Inside the shared panel: the pause menu's content. Shown only while paused.")]
    [SerializeField] private GameObject pauseContent;
    [Tooltip("Inside the shared panel: the end menu's content. Shown on win and on game over.")]
    [SerializeField] private GameObject endContent;
    [SerializeField] private GameObject levelUpPanel;

    [Tooltip("Opened from the title screen. Make it a sibling of the title panel, not a child, or it hides with it.")]
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Opened from the title screen. Make it a sibling of the title panel, not a child, or it hides with it.")]
    [SerializeField] private GameObject creditsPanel;

    [Header("Title buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [Tooltip("Closes the game. It does nothing in a browser, so it is hidden in a WebGL build.")]
    [SerializeField] private Button exitButton;

    [Header("Settings and credits")]
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button creditsBackButton;
    [Tooltip("The credits list. It is scrolled back to the top every time it opens.")]
    [SerializeField] private ScrollRect creditsScroll;

    [Header("Pause buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [Tooltip("Goes back to the title screen. (A browser game can't close itself.)")]
    [SerializeField] private Button pauseQuitButton;

    [Header("End menu")]
    [SerializeField] private Button endRestartButton;
    [SerializeField] private Button endMenuButton;
    [Tooltip("The heading: it shows one of the two texts below.")]
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endStatsText;
    [SerializeField] private string winTitle = "YOU SURVIVED!";
    [SerializeField] private string loseTitle = "GAME OVER";

    [Header("Volume")]
    [Tooltip("Every slider here controls the same MASTER volume (put one on the settings screen and one on the pause screen).")]
    [SerializeField] private Slider[] volumeSliders;
    [Tooltip("Sliders for the music volume. Any number, kept in step with each other.")]
    [SerializeField] private Slider[] musicSliders;
    [Tooltip("Sliders for the sound effects volume. Any number, kept in step with each other.")]
    [SerializeField] private Slider[] sfxSliders;

    [Header("Level-up screen")]
    [Tooltip("Every weapon that can be offered as an upgrade.")]
    [SerializeField] private WeaponData[] allWeapons;
    [Tooltip("The choice buttons. Three cards means three choices.")]
    [SerializeField] private LevelUpCard[] cards;
    [Tooltip("The heading on the level-up screen. Optional: it changes between the two texts below.")]
    [SerializeField] private TMP_Text levelUpTitleText;
    [SerializeField] private string levelUpTitle = "LEVEL UP";
    [Tooltip("Shown instead when the player has no weapon yet: at the start of a run they pick their first one.")]
    [SerializeField] private string startingWeaponTitle = "CHOOSE YOUR WEAPON";

    private PlayerExperience experience;
    private WeaponManager weapons;

    private GameObject openSubmenu;   // the settings or credits panel that is open over the title screen, if any
    private bool autoResolveLevelUp;
    private bool pickStartingWeapon;   // a run just started: if the player has no weapon, open the first pick

    private readonly List<WeaponData> candidates = new List<WeaponData>();
    private readonly HashSet<Button> hooked = new HashSet<Button>();

    private void Awake()
    {
        Hook(playButton, StartRun);
        Hook(settingsButton, () => OpenSubmenu(settingsPanel));
        Hook(creditsButton, () => OpenSubmenu(creditsPanel));
        Hook(settingsBackButton, CloseSubmenu);
        Hook(creditsBackButton, CloseSubmenu);
        Hook(exitButton, ExitGame);
        Hook(resumeButton, Resume);
        Hook(pauseRestartButton, Restart);
        Hook(pauseQuitButton, BackToTitle);
        Hook(endRestartButton, Restart);
        Hook(endMenuButton, BackToTitle);

        // A browser game can't close itself, so a dead button would only confuse players there.
#if UNITY_WEBGL && !UNITY_EDITOR
        if (exitButton != null) exitButton.gameObject.SetActive(false);
#endif
    }

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

    // The player and weapons exist by now, so their events are hooked here.
    private void Start()
    {
        Player player = Player.Instance;
        if (player != null)
        {
            experience = player.Experience;
            experience.OnLevelUp += HandleLevelUp;
        }

        weapons = WeaponManager.Instance;
        SetupVolumeSliders();
    }

    private void OnDestroy()
    {
        if (experience != null) experience.OnLevelUp -= HandleLevelUp;
    }

    private void Update()
    {
        HandlePauseInput();
        OpenStartingWeaponPick();
        ResolveEmptyLevelUp();
    }

    // ---- Screens ----

    private void HandleState(GameState state)
    {
        bool paused = state == GameState.Paused;
        bool ended = state == GameState.Win || state == GameState.Lose;

        // Leaving the title screen, or coming back to it, always starts with no submenu open.
        openSubmenu = null;
        SetActive(settingsPanel, false);
        SetActive(creditsPanel, false);

        SetActive(titlePanel, state == GameState.Title);
        SetActive(menuPanel, paused || ended);
        SetActive(pauseContent, paused);
        SetActive(endContent, ended);
        SetActive(levelUpPanel, state == GameState.LevelUp);

        if (state == GameState.LevelUp) ShowLevelUpChoices();
        else if (ended) ShowEnd(state == GameState.Win);
    }

    private void HandleRunStarted() => pickStartingWeapon = true;

    // A level-up pauses the game and opens the upgrade screen.
    private void HandleLevelUp(int level)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.IsPlaying) gameManager.ChangeState(GameState.LevelUp);
    }

    // A run starts with no weapon unless the WeaponManager gives starting ones, and the player chooses theirs on the same
    // screen that level-ups use. It waits for the next frame, so the WeaponManager has finished resetting first (the order
    // of the run-started handlers isn't fixed), and so the state isn't changed from inside the change that started the run.
    private void OpenStartingWeaponPick()
    {
        if (!pickStartingWeapon) return;
        pickStartingWeapon = false;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.IsPlaying) return;
        if (weapons != null && weapons.Weapons.Count == 0) gameManager.ChangeState(GameState.LevelUp);
    }

    private void HandlePauseInput()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) && !Input.GetKeyDown(KeyCode.P)) return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        // On the title screen, Esc backs out of settings or credits.
        if (gameManager.CurrentState == GameState.Title)
        {
            if (openSubmenu != null) CloseSubmenu();
            return;
        }

        if (gameManager.CurrentState == GameState.Playing) gameManager.ChangeState(GameState.Paused);
        else if (gameManager.CurrentState == GameState.Paused) gameManager.ChangeState(GameState.Playing);
    }

    private void ShowEnd(bool won)
    {
        if (endTitleText != null) endTitleText.text = won ? winTitle : loseTitle;
        if (endStatsText == null || GameManager.Instance == null) return;

        int level = experience != null ? experience.Level : 1;
        endStatsText.text = (won ? "Survived " : "Lasted ") + FormatTime(Mathf.FloorToInt(GameManager.Instance.ElapsedTime)) + "   Level " + level;
    }

    private static string FormatTime(int totalSeconds) => (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");

    // ---- Button actions ----

    // Settings and credits replace the title buttons while they are open, so nothing underneath can be clicked.
    private void OpenSubmenu(GameObject panel)
    {
        if (panel == null) return;

        openSubmenu = panel;
        SetActive(titlePanel, false);
        panel.SetActive(true);

        if (panel == creditsPanel && creditsScroll != null) creditsScroll.verticalNormalizedPosition = 1f;
    }

    private void CloseSubmenu()
    {
        SetActive(openSubmenu, false);
        openSubmenu = null;
        SetActive(titlePanel, true);
    }

    // For an installed build. (The editor's Play button is stopped instead, since Quit does nothing there.)
    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartRun() => GameManager.Instance.ChangeState(GameState.Playing);
    private void Resume() => GameManager.Instance.ChangeState(GameState.Playing);
    private void BackToTitle() => GameManager.Instance.ChangeState(GameState.Title);

    // The GameManager counts "Playing" as a new run only when it comes from Title, Win or Lose. Restarting
    // from the pause screen therefore goes through Title for an instant, in the same frame, so the same rule applies.
    private void Restart()
    {
        GameManager gameManager = GameManager.Instance;
        GameState current = gameManager.CurrentState;

        if (current != GameState.Title && current != GameState.Win && current != GameState.Lose)
            gameManager.ChangeState(GameState.Title);

        gameManager.ChangeState(GameState.Playing);
    }

    // Every button also plays the click sound, so that lives in one place.
    // A button that is assigned to two fields is only hooked once, so one press can't run its action twice.
    private void Hook(Button button, System.Action action)
    {
        if (button == null || !hooked.Add(button)) return;
        button.onClick.AddListener(() =>
        {
            if (SoundsManager.Instance != null) SoundsManager.Instance.PlayClick();
            action();
        });
    }

    // ---- Volume ----

    private void SetupVolumeSliders()
    {
        SoundsManager sound = SoundsManager.Instance;
        if (sound == null) return;

        SetupSliders(volumeSliders, sound.Volume, sound.SetVolume);
        SetupSliders(musicSliders, sound.MusicVolume, sound.SetMusicVolume);
        SetupSliders(sfxSliders, sound.SfxVolume, sound.SetSfxVolume);
    }

    // All the sliders in one group control the same value, so when one moves the others follow, without firing their own events.
    private static void SetupSliders(Slider[] sliders, float current, System.Action<float> apply)
    {
        if (sliders == null) return;

        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null) continue;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(current);
            slider.onValueChanged.AddListener(value =>
            {
                apply(value);
                for (int j = 0; j < sliders.Length; j++)
                    if (sliders[j] != null) sliders[j].SetValueWithoutNotify(value);
            });
        }
    }

    // ---- Level-up screen ----

    private void ShowLevelUpChoices()
    {
        WeaponManager manager = WeaponManager.Instance;

        if (levelUpTitleText != null)
            levelUpTitleText.text = manager != null && manager.Weapons.Count == 0 ? startingWeaponTitle : levelUpTitle;

        // Offer every weapon that can still be upgraded (or isn't owned yet).
        candidates.Clear();
        for (int i = 0; i < allWeapons.Length; i++)
        {
            WeaponData data = allWeapons[i];
            if (data == null) continue;

            Weapon owned = manager != null ? manager.Find(data) : null;
            if (owned != null && owned.IsMaxLevel) continue;
            candidates.Add(data);
        }

        // Nothing left to upgrade: there is no choice to show. This can't close the screen from here,
        // because the state is still in the middle of changing, so Update does it next frame.
        if (candidates.Count == 0)
        {
            autoResolveLevelUp = true;
            return;
        }

        // Shuffle (Fisher-Yates), so the cards shown are a random selection.
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int shown = Mathf.Min(candidates.Count, cards.Length);
        for (int i = 0; i < cards.Length; i++)
        {
            LevelUpCard card = cards[i];
            if (card.button == null) continue;

            bool used = i < shown;
            card.button.gameObject.SetActive(used);
            if (!used) continue;

            WeaponData choice = candidates[i];
            Weapon owned = manager != null ? manager.Find(choice) : null;

            if (card.icon != null) card.icon.sprite = choice.Sprite;
            if (card.nameText != null) card.nameText.text = choice.DisplayName;
            if (card.levelText != null) card.levelText.text = owned == null ? "NEW" : "Lv " + owned.Level + " -> " + (owned.Level + 1);
            if (card.descriptionText != null) card.descriptionText.text = choice.Description;

            card.button.onClick.RemoveAllListeners();
            card.button.onClick.AddListener(() =>
            {
                if (SoundsManager.Instance != null) SoundsManager.Instance.PlayClick();
                ChooseUpgrade(choice);
            });
        }
    }

    private void ChooseUpgrade(WeaponData choice)
    {
        if (weapons != null) weapons.AddOrLevelUp(choice);
        if (experience != null) experience.ConsumePendingLevelUp();

        // Don't leave a button selected, or pressing Space or Enter would pick the next card by accident.
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        // Level-ups can stack up (one big gem can give two): show the next choice, or go back to playing.
        if (experience != null && experience.PendingLevelUps > 0) ShowLevelUpChoices();
        else GameManager.Instance.ChangeState(GameState.Playing);
    }

    private void ResolveEmptyLevelUp()
    {
        if (!autoResolveLevelUp) return;
        autoResolveLevelUp = false;

        if (experience != null) while (experience.ConsumePendingLevelUp()) { }
        if (GameManager.Instance.CurrentState == GameState.LevelUp) GameManager.Instance.ChangeState(GameState.Playing);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}