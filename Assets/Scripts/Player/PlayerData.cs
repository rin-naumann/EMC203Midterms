using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Player")]
public class PlayerData : ScriptableObject
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float hitInvulnerability = 0.5f;
    [SerializeField] private float hitRadius = 0.4f;

    public float MoveSpeed => moveSpeed;
    public float MaxHealth => maxHealth;
    public float HitInvulnerability => hitInvulnerability;
    public float HitRadius => hitRadius;
}