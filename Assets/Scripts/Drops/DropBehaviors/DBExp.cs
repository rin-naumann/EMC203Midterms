using UnityEngine;

/// <summary>Gives the player EXP when collected.</summary>
[CreateAssetMenu(fileName = "Exp", menuName = "Drop Behaviors/Exp")]
public class DBExp : DropBehavior
{
    public override void OnCollect(Player player, float value) => player.Experience.AddExp(Mathf.RoundToInt(value));
}