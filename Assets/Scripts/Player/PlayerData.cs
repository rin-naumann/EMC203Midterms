using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Player")]
public class PlayerData : ScriptableObject
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxHealth = 100f;
    public float MoveSpeed => moveSpeed;
    public float MaxHealth => maxHealth;
}