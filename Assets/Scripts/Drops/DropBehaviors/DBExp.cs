using UnityEngine;

/// <summary>Collecting it gives the player EXP.</summary>
[CreateAssetMenu(fileName = "Exp", menuName = "Drop Behaviors/Exp")]
public class DBExp : DropBehavior
{
    public override void OnCollect(Player player, float value) => player.Experience.AddExp(Mathf.RoundToInt(value));
}