using UnityEngine;

/// <summary>
/// One kind of drop (an EXP gem, a heart): how it looks, what it does when collected, and how it
/// flies to the player. Read-only at runtime. The per-drop state lives on DropPickup.
/// </summary>
[CreateAssetMenu(fileName = "New Drop", menuName = "Drop")]
public class DropData : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [SerializeField] private DropBehavior behavior;

    [Header("Stats")]
    [SerializeField] private float value = 1f;

    [Header("Attraction")]
    [SerializeField] private float attractStartSpeed = 6f;
    [SerializeField] private float attractAcceleration = 20f;

    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public DropBehavior Behavior => behavior;
    public float Value => value;
    public float AttractStartSpeed => attractStartSpeed;
    public float AttractAcceleration => attractAcceleration;
}