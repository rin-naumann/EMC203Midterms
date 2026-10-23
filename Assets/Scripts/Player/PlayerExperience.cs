using UnityEngine;
using System;

/// <summary>
/// Tracks the player's EXP and level, and raises events on level-up.
/// </summary>
public class PlayerExperience : MonoBehaviour
{
    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelUp;

    private int baseCost = 10;
    private int costIncrease = 25;

    public int Level { get; private set; } = 1;
    public int CurrentExp { get; private set; }
    public int PendingLevelUps { get; private set; }

    public int ExpToNextLevel => baseCost + (Level - 1) * costIncrease;

    public void Init(int baseExpCost, int expCostIncrease)
    {
        baseCost = baseExpCost;
        costIncrease = expCostIncrease;
        ResetRun();
    }

    public void ResetRun()
    {
        Level = 1;
        CurrentExp = 0;
        PendingLevelUps = 0;
        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        CurrentExp += amount;

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;
            PendingLevelUps++;
            OnLevelUp?.Invoke(Level);
        }

        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel);
    }

    /// <summary>Used by the upgrade screen once per choice. Returns false if none are waiting.</summary>
    public bool ConsumePendingLevelUp()
    {
        if (PendingLevelUps <= 0) return false;
        PendingLevelUps--;
        return true;
    }
}