using UnityEngine;

/// <summary>
/// A pooled drop on the ground.
/// Flies to the player once in magnet range and is collected on reaching them.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DropPickup : MonoBehaviour, IPoolable
{
    /// <summary>Raised when a drop is collected, after its effect is applied.</summary>
    public static event System.Action<DropPickup> OnCollected;

    private SpriteRenderer spriteRenderer;
    private DropData data;

    public DropData Data => data;
    public bool IsAttracted;
    public float CurrentSpeed;

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    public void Init(DropData dropData, Vector3 position)
    {
        data = dropData;
        transform.position = position;
        spriteRenderer.sprite = data.Sprite;

        // Reset the old pooled state
        IsAttracted = false;
        CurrentSpeed = data.AttractStartSpeed;
    }

    public void Tick(float deltaTime, Vector3 playerPosition, float magnetRadius, float collectRadius)
    {
        if (!IsAttracted)
        {
            // Once started, it never stops
            if ((playerPosition - transform.position).sqrMagnitude > magnetRadius * magnetRadius) return;
            IsAttracted = true;
        }

        CurrentSpeed += data.AttractAcceleration * deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, playerPosition, CurrentSpeed * deltaTime);

        if ((playerPosition - transform.position).sqrMagnitude > collectRadius * collectRadius) return;

        data.Behavior.OnCollect(Player.Instance, data.Value);
        OnCollected?.Invoke(this);
        ObjectPooler.Instance.DropPool.Release(this);
    }

    void IPoolable.OnSpawn() => DropManager.Instance?.Register(this);
    void IPoolable.OnDespawn() => DropManager.Instance?.Unregister(this);
}