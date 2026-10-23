using UnityEngine;

public abstract class DropBehavior : ScriptableObject
{
    public abstract void OnCollect(Player player, float value);
}