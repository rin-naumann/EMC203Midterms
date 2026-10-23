using UnityEngine;

/// <summary>
/// Base class for what happens when the player collects a drop.
/// </summary>
public abstract class DropBehavior : ScriptableObject
{
    public abstract void OnCollect(Player player, float value);
}