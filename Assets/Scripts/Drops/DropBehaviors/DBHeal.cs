using UnityEngine;

/// <summary>Heals the player when collected.</summary>
[CreateAssetMenu(fileName = "Heal", menuName = "Drop Behaviors/Heal")]
public class DBHeal : DropBehavior
{
    public override void OnCollect(Player player, float value) => player.Health.Heal(value);
}