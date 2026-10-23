using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

/// <summary>
/// One sound the game can play: its clips, loudness and how often it may repeat. Plain data, so a weapon
/// can carry its own attack sound in its WeaponData and the manager does not need to know about weapons.
/// </summary>
[System.Serializable]
public class SoundEffect
{
    [Tooltip("One of these is picked at random each time, so a repeated sound doesn't feel identical. One clip is fine.")]
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("The sound can't play again until this many seconds have passed. Stops many kills in the same moment from becoming noise. 0 means no limit.")]
    [SerializeField] private float minInterval = 0f;
    [Tooltip("Random pitch change, plus or minus. 0.05 is 5%.")]
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.05f;

    public float Volume => volume;
    public float MinInterval => minInterval;
    public float PitchVariation => pitchVariation;
    public bool HasClips => clips != null && clips.Length > 0;
    public AudioClip PickClip() => clips[UnityEngine.Random.Range(0, clips.Length)];
}

/// <summary>
/// Plays every sound and the music, on two separate channels (music and sound effects) that each have their own
/// volume, under one master volume. Gameplay scripts never call it: it listens to the events they already
/// raise (a kill, a hit, a level-up, a state change) and decides what to play. The one exception is the UI,
/// which asks for the click sound and sets the volumes.
/// The music follows the run: the menu track, then the early track, the late track once the run reaches a set time, and
/// the boss track when the slime appears. Tracks crossfade into each other.
/// </summary>
public class SoundsManager : MonoBehaviour
{
    public static SoundsManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [Tooltip("The first part of the run.")]
    [FormerlySerializedAs("gameplayMusic")]
    [SerializeField] private AudioClip earlyMusic;
    [Tooltip("The second part of the run, from the time below until the slime appears. Leave empty to keep playing the early track.")]
    [SerializeField] private AudioClip lateMusic;
    [Tooltip("Run time, in seconds, when the late track takes over. 180 is when the Orcs and Demons arrive.")]
    [SerializeField] private float lateMusicStartsAt = 180f;
    [Tooltip("From the moment the slime appears until the run ends. Leave empty to keep the current track.")]
    [SerializeField] private AudioClip bossMusic;
    [Tooltip("How loud the music is at full music volume, before the player's sliders.")]
    [FormerlySerializedAs("musicVolume")]
    [SerializeField, Range(0f, 1f)] private float musicLoudness = 0.5f;
    [Tooltip("Music loudness while paused or choosing an upgrade, as a fraction of normal.")]
    [SerializeField, Range(0f, 1f)] private float pausedMusicScale = 0.4f;
    [Tooltip("Seconds a track takes to fade out as the next fades in.")]
    [SerializeField] private float crossfadeSeconds = 2f;
    [Tooltip("Seconds the music takes to fade out when the run ends (a win or game over).")]
    [SerializeField] private float endFadeSeconds = 0.5f;

    [Header("Sound effects")]
    [SerializeField] private SoundEffect click;
    [Tooltip("An enemy was hurt but survived. Heard very often, so give it a short Min Interval (about 0.05).")]
    [SerializeField] private SoundEffect enemyHit;
    [SerializeField] private SoundEffect enemyDeath;
    [SerializeField] private SoundEffect playerHurt;
    [Tooltip("The Shield blocked a hit completely.")]
    [SerializeField] private SoundEffect playerBlocked;
    [SerializeField] private SoundEffect levelUp;
    [Tooltip("The player collects a drop (an EXP gem or a heart). Gems arrive in streams, so give it a short Min Interval (about 0.05).")]
    [SerializeField] private SoundEffect pickup;
    [Tooltip("The slime lands.")]
    [SerializeField] private SoundEffect slam;
    [Tooltip("Played once when the player wins.")]
    [SerializeField] private SoundEffect win;
    [Tooltip("Played once when the player dies.")]
    [SerializeField] private SoundEffect lose;

    [Header("Voices")]
    [Tooltip("How many sound effects can play at the same time. When they are all busy, the oldest is cut off.")]
    [SerializeField] private int voices = 12;

    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";

    // Two music sources, so one track can fade out while the next fades in.
    private class MusicChannel
    {
        public AudioSource source;
        public float fade;      // 0 silent, 1 full
        public float target;    // where the fade is heading
    }

    private readonly MusicChannel[] channels = new MusicChannel[2];
    private int activeChannel;
    private AudioClip currentTrack;
    private float fadeSeconds;
    private bool musicDucked;

    private AudioSource[] voiceSources;
    private int nextVoice;

    // When each limited sound last played. Kept here and not on the SoundEffect, because that is data shared
    // with the asset and would remember the last play across Play-mode sessions.
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

        // A fixed set of sources used in turn, so the number of simultaneous sounds can never grow without limit.
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

    // Static events outlive this object, so always unsubscribe in OnDisable.
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

    // The player's own events are on the player object, which exists by now, so they are hooked here.
    private void Start()
    {
        // Load every music track now, while the title screen is showing, so the first play of a track
        // in the middle of a run doesn't have to load it then.
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

    // Does nothing for a clip that is already loaded or loading, such as one with Preload Audio Data ticked in its import settings.
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

    // Every frame: work out which track the game wants right now, switch if it changed, and move the fades along.
    // The wanted track comes from the game's state and clock, so there is only one place that decides it.
    private void UpdateMusic()
    {
        if (GameManager.Instance == null) return;

        AudioClip wanted = ChooseTrack();
        if (wanted != currentTrack) CrossfadeTo(wanted);

        // Unscaled time: the game is frozen (timeScale 0) on the title screen, when paused and when it ends, and the fades still need to finish.
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
        float level = musicLoudness * MusicVolume * (musicDucked ? pausedMusicScale : 1f);

        for (int i = 0; i < channels.Length; i++)
        {
            MusicChannel channel = channels[i];
            channel.fade = Mathf.MoveTowards(channel.fade, channel.target, step);
            channel.source.volume = level * channel.fade;

            // Faded all the way out: stop it, so it isn't decoding sound nobody hears.
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

            // The run is over: silence, so the win or game over jingle can be heard.
            case GameState.Win:
            case GameState.Lose:
                return null;

            // Playing, paused and choosing an upgrade are all "in the run": the same track keeps going.
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

        // Whatever is playing fades out...
        for (int i = 0; i < channels.Length; i++) channels[i].target = 0f;
        if (clip == null) return;

        // ...while the new track starts on the other source and fades in.
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

    // A new run starts with the early track again, even if the last one ended on the boss music.
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