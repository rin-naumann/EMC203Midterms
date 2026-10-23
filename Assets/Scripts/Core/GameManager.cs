using UnityEngine;
using System;

/// <summary>
/// The states the game can be in.
/// </summary>
public enum GameState
{
    Title,
    Playing,
    Paused,
    LevelUp,
    Win,
    Lose
}

/// <summary>
/// Controls the game state and the run timer.
/// Tells other scripts when the state changes or a new run starts.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<GameState> OnStateChanged;
    public static event Action OnRunStarted;

    [SerializeField] private GameState startState = GameState.Title;
    private bool hasState;
    public GameState CurrentState { get; private set; }
    public bool IsPlaying => CurrentState == GameState.Playing;
    public float ElapsedTime { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        else Instance = this;
    }

    private void Start() => ChangeState(startState);

    private void Update()
    {
        if (CurrentState == GameState.Playing) ElapsedTime += Time.deltaTime;
    }

    public void ChangeState(GameState newState)
    {
        if (hasState && newState == CurrentState) return;

        // A new run only starts from Title, Win or Lose
        bool startsRun = newState == GameState.Playing &&
            (!hasState || CurrentState == GameState.Title || CurrentState == GameState.Win || CurrentState == GameState.Lose);

        hasState = true;
        CurrentState = newState;

        Time.timeScale = newState == GameState.Playing ? 1f : 0f;

        if (startsRun)
        {
            ElapsedTime = 0f;
            OnRunStarted?.Invoke();
        }

        OnStateChanged?.Invoke(newState);
    }

    /// <summary>Debug only: jumps the run clock forward.</summary>
    public void SkipTime(float seconds) => ElapsedTime += seconds;
}