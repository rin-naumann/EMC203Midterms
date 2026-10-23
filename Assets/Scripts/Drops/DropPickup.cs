using UnityEngine;

/// <summary>
/// A pooled drop on the ground. Sits still until the player comes within the magnet radius, then
/// flies to the player, speeding up, and is collected when it reaches the player's body.
/// What collecting does is up to its DropBehavior.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DropPickup : MonoBehaviour, IPoolable
{
    /// <summary>Raised when the player collects a drop, after its effect (EXP, healing) has been applied. The pickup sound listens to it.</summary>
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

        // A pooled object carries its old state, so reset all of it.
        IsAttracted = false;
        CurrentSpeed = data.AttractStartSpeed;
    }

    public void Tick(float deltaTime, Vector3 playerPosition, float magnetRadius, float collectRadius)
    {
        if (!IsAttracted)
        {
            // Once started, it never stops: a fast player can't outrun a drop that is already flying.
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