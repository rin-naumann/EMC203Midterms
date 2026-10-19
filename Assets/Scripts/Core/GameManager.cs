using UnityEngine;
using System;

public enum GameState
{
    Title,
    Playing,
    Paused,
    LevelUp,
    Win,
    Lose
}

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

        // Pause and level-up also return to Playing, but those continue the same run.
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