using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// The game screens and their buttons: title, settings, credits, pause, win / game over and level-up.
/// Shows the screen for each game state and handles button presses.
/// The in-game bars and timer are in HUDManager.
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
    [Tooltip("Shared by the pause menu and the end menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject pauseContent;
    [SerializeField] private GameObject endContent;
    [SerializeField] private GameObject levelUpPanel;

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Title buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    [Header("Settings and credits")]
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private ScrollRect creditsScroll;

    [Header("Pause buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseQuitButton;

    [Header("End menu")]
    [SerializeField] private Button endRestartButton;
    [SerializeField] private Button endMenuButton;
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endStatsText;
    [SerializeField] private string winTitle = "YOU SURVIVED!";
    [SerializeField] private string loseTitle = "GAME OVER";

    [Header("Volume")]
    [Tooltip("All of these sliders control the master volume")]
    [SerializeField] private Slider[] volumeSliders;
    [SerializeField] private Slider[] musicSliders;
    [SerializeField] private Slider[] sfxSliders;

    [Header("Level-up screen")]
    [SerializeField] private WeaponData[] allWeapons;
    [SerializeField] private LevelUpCard[] cards;
    [SerializeField] private TMP_Text levelUpTitleText;
    [SerializeField] private string levelUpTitle = "LEVEL UP";
    [Tooltip("Shown when the player has no weapon yet")]
    [SerializeField] private string startingWeaponTitle = "CHOOSE YOUR WEAPON";

    private PlayerExperience experience;
    private WeaponManager weapons;

    private GameObject openSubmenu;  // the settings or credits panel open over the title
    private bool autoResolveLevelUp;
    private bool pickStartingWeapon;  // a run just started

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

        // Hidden in browsers because the game cannot close itself there
#if UNITY_WEBGL && !UNITY_EDITOR
        if (exitButton != null) exitButton.gameObject.SetActive(false);
#endif
    }

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

        // Always start with no submenu open
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

    // A level-up opens the upgrade screen
    private void HandleLevelUp(int level)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.IsPlaying) gameManager.ChangeState(GameState.LevelUp);
    }

    // Opens the first weapon pick if the run starts with no weapon
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

        // On the title screen, Esc closes settings or credits
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

    // Settings and credits replace the title buttons while open
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

    // Quits the build, or stops Play mode in the editor
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

    // Restarting from pause goes through Title first so it counts as a new run
    private void Restart()
    {
        GameManager gameManager = GameManager.Instance;
        GameState current = gameManager.CurrentState;

        if (current != GameState.Title && current != GameState.Win && current != GameState.Lose)
            gameManager.ChangeState(GameState.Title);

        gameManager.ChangeState(GameState.Playing);
    }

    // Every button plays the click sound, and one assigned twice is hooked once
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

    // Sliders in a group follow each other without firing events
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

        // Offer every weapon that can still be upgraded or is not owned yet
        candidates.Clear();
        for (int i = 0; i < allWeapons.Length; i++)
        {
            WeaponData data = allWeapons[i];
            if (data == null) continue;

            Weapon owned = manager != null ? manager.Find(data) : null;
            if (owned != null && owned.IsMaxLevel) continue;
            candidates.Add(data);
        }

        // Nothing to upgrade: Update closes the screen next frame
        if (candidates.Count == 0)
        {
            autoResolveLevelUp = true;
            return;
        }

        // Shuffle so the cards are random
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

        // Deselect the button so Space or Enter cannot pick another card
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        // Show the next pending choice, or go back to playing
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