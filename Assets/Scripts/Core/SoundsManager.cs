using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// One sound the game can play: its clips, volume and repeat limit.
/// </summary>
[System.Serializable]
public class SoundEffect
{
    [Tooltip("One is picked at random each time")]
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("The sound cannot repeat until this many seconds pass (0 = no limit)")]
    [SerializeField] private float minInterval = 0f;
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.05f;

    public float Volume => volume;
    public float MinInterval => minInterval;
    public float PitchVariation => pitchVariation;
    public bool HasClips => clips != null && clips.Length > 0;
    public AudioClip PickClip() => clips[UnityEngine.Random.Range(0, clips.Length)];
}

/// <summary>
/// Plays all music and sound effects. Music and effects have their own volume under one master volume.
/// Listens to game events to decide what to play.
/// Music goes menu, early, late, then boss, and crossfades between tracks.
/// </summary>
public class SoundsManager : MonoBehaviour
{
    public static SoundsManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip earlyMusic;
    [SerializeField] private AudioClip lateMusic;
    [SerializeField] private float lateMusicStartsAt = 180f;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField, Range(0f, 1f)] private float musicLoudness = 0.5f;
    [Tooltip("Music volume while paused or choosing an upgrade, as a fraction")]
    [SerializeField, Range(0f, 1f)] private float pausedMusicScale = 0.4f;
    [SerializeField] private float crossfadeSeconds = 2f;
    [SerializeField] private float endFadeSeconds = 0.5f;

    [Header("Sound effects")]
    [SerializeField] private SoundEffect click;
    [SerializeField] private SoundEffect enemyHit;
    [SerializeField] private SoundEffect enemyDeath;
    [SerializeField] private SoundEffect playerHurt;
    [SerializeField] private SoundEffect playerBlocked;
    [SerializeField] private SoundEffect levelUp;
    [SerializeField] private SoundEffect pickup;
    [SerializeField] private SoundEffect slam;
    [SerializeField] private SoundEffect win;
    [SerializeField] private SoundEffect lose;

    [Header("Voices")]
    [Tooltip("How many sound effects can play at once")]
    [SerializeField] private int voices = 12;

    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";

    // Two music sources so one can fade out while the other fades in
    private class MusicChannel
    {
        public AudioSource source;
        public float fade;  // 0 silent, 1 full
        public float target;  // where the fade is heading
    }

    private readonly MusicChannel[] channels = new MusicChannel[2];
    private int activeChannel;
    private AudioClip currentTrack;
    private float fadeSeconds;
    private bool musicDucked;

    private AudioSource[] voiceSources;
    private int nextVoice;

    // When each limited sound last played
    private readonly Dictionary<SoundEffect, float> lastPlayed = new Dictionary<SoundEffect, float>();

    private bool bossActive;

    private PlayerHealth health;
    private PlayerExperience experience;

    /// <summary>Master volume from 0 to 1. Applies to everything.</summary>
    public float Volume { get; private set; } = 1f;
    /// <summary>Music volume from 0 to 1.</summary>
    public float MusicVolume { get; private set; } = 1f;
    /// <summary>Sound effects volume from 0 to 1.</summary>
    public float SfxVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;

        for (int i = 0; i < channels.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            channels[i] = new MusicChannel { source = source };
        }
        fadeSeconds = crossfadeSeconds;

        // A fixed set of sources, so sounds cannot pile up endlessly
        voiceSources = new AudioSource[Mathf.Max(1, voices)];
        for (int i = 0; i < voiceSources.Length; i++)
        {
            voiceSources[i] = gameObject.AddComponent<AudioSource>();
            voiceSources[i].playOnAwake = false;
        }

        Volume = PlayerPrefs.GetFloat(MasterKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
        AudioListener.volume = Volume;
    }

    // Unsubscribe so the static events do not keep this object alive
    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleState;
        GameManager.OnRunStarted += HandleRunStarted;
        Enemy.OnEnemyDeath += HandleEnemyDeath;
        Enemy.OnEnemyHit += HandleEnemyHit;
        Weapon.OnAttack += HandleWeaponAttack;
        EBSlime.OnSlam += HandleSlam;
        DropPickup.OnCollected += HandlePickup;
        EBSlime.OnSlimeSpawned += HandleSlimeSpawned;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleState;
        GameManager.OnRunStarted -= HandleRunStarted;
        Enemy.OnEnemyDeath -= HandleEnemyDeath;
        Enemy.OnEnemyHit -= HandleEnemyHit;
        Weapon.OnAttack -= HandleWeaponAttack;
        EBSlime.OnSlam -= HandleSlam;
        DropPickup.OnCollected -= HandlePickup;
        EBSlime.OnSlimeSpawned -= HandleSlimeSpawned;
    }

    // Hooked here because the player exists by now
    private void Start()
    {
        // Load the music now so playing it later does not lag
        PreloadMusic(menuMusic);
        PreloadMusic(earlyMusic);
        PreloadMusic(lateMusic);
        PreloadMusic(bossMusic);

        Player player = Player.Instance;
        if (player == null) return;

        health = player.Health;
        experience = player.Experience;
        health.OnDamaged += HandlePlayerDamaged;
        health.OnBlocked += HandlePlayerBlocked;
        experience.OnLevelUp += HandleLevelUp;
    }

    // Skips clips that are already loaded
    private static void PreloadMusic(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= HandlePlayerDamaged;
            health.OnBlocked -= HandlePlayerBlocked;
        }
        if (experience != null) experience.OnLevelUp -= HandleLevelUp;
        if (Instance == this) Instance = null;
    }

    private void Update() => UpdateMusic();

    // ---- What the UI can ask for ----

    public void PlayClick() => Play(click);

    public void SetVolume(float value)
    {
        Volume = Mathf.Clamp01(value);
        AudioListener.volume = Volume;
        PlayerPrefs.SetFloat(MasterKey, Volume);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
    }

    // ---- Sound effects ----

    public void Play(SoundEffect effect)
    {
        if (effect == null || !effect.HasClips) return;

        if (effect.MinInterval > 0f)
        {
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(effect, out float last) && now - last < effect.MinInterval) return;
            lastPlayed[effect] = now;
        }

        AudioSource source = voiceSources[nextVoice];
        nextVoice = (nextVoice + 1) % voiceSources.Length;

        source.clip = effect.PickClip();
        source.volume = effect.Volume * SfxVolume;
        source.pitch = 1f + Random.Range(-effect.PitchVariation, effect.PitchVariation);
        source.Play();
    }

    // ---- Music ----

    // Picks the wanted track, switches if it changed, and moves the fades
    private void UpdateMusic()
    {
        if (GameManager.Instance == null) return;

        AudioClip wanted = ChooseTrack();
        if (wanted != currentTrack) CrossfadeTo(wanted);

        // Unscaled time, so fades finish while the game is frozen
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
        float level = musicLoudness * MusicVolume * (musicDucked ? pausedMusicScale : 1f);

        for (int i = 0; i < channels.Length; i++)
        {
            MusicChannel channel = channels[i];
            channel.fade = Mathf.MoveTowards(channel.fade, channel.target, step);
            channel.source.volume = level * channel.fade;

            // Stop fully faded out channels
            if (channel.fade <= 0f && channel.target <= 0f && channel.source.isPlaying) channel.source.Stop();
        }
    }

    private AudioClip ChooseTrack()
    {
        GameManager gameManager = GameManager.Instance;

        switch (gameManager.CurrentState)
        {
            case GameState.Title:
                return menuMusic;

            // Run over: silence so the jingle can be heard
            case GameState.Win:
            case GameState.Lose:
                return null;

            // Playing, paused and level-up share the same track
            default:
                if (bossActive && bossMusic != null) return bossMusic;
                if (lateMusic != null && gameManager.ElapsedTime >= lateMusicStartsAt) return lateMusic;
                return earlyMusic;
        }
    }

    private void CrossfadeTo(AudioClip clip)
    {
        currentTrack = clip;
        fadeSeconds = clip == null ? endFadeSeconds : crossfadeSeconds;

        // Fade out what is playing...
        for (int i = 0; i < channels.Length; i++) channels[i].target = 0f;
        if (clip == null) return;

        // ...and fade in the new track on the other source
        activeChannel = 1 - activeChannel;
        MusicChannel next = channels[activeChannel];
        next.source.clip = clip;
        next.fade = 0f;
        next.target = 1f;
        next.source.Play();
    }

    // ---- Reacting to the game ----

    private void HandleState(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
            case GameState.LevelUp:
                musicDucked = true;
                break;
            case GameState.Win:
                musicDucked = false;
                Play(win);
                break;
            case GameState.Lose:
                musicDucked = false;
                Play(lose);
                break;
            default:
                musicDucked = false;
                break;
        }
    }

    // A new run starts with the early track again
    private void HandleRunStarted() => bossActive = false;

    private void HandleSlimeSpawned() => bossActive = true;

    private void HandleEnemyDeath(Enemy enemy) => Play(enemyDeath);
    private void HandleEnemyHit(Enemy enemy) => Play(enemyHit);
    private void HandleWeaponAttack(Weapon weapon) => Play(weapon.Data.AttackSound);
    private void HandlePlayerDamaged(float damage) => Play(playerHurt);
    private void HandlePlayerBlocked() => Play(playerBlocked);
    private void HandleLevelUp(int level) => Play(levelUp);
    private void HandleSlam(Vector3 position, float radius) => Play(slam);
    private void HandlePickup(DropPickup drop) => Play(pickup);
}