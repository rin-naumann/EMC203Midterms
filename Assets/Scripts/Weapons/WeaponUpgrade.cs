using UnityEngine;

public abstract class WeaponUpgrade : ScriptableObject
{
    private string displayName;
    private int unlockLevel;
    private string description;

    public string DisplayName => displayName;
    [TextArea] public string Description => description;
    public int UnlockLevel => unlockLevel;
    public virtual void OnWeaponUpgrade() { }
    public virtual void OnWeaponAttack() { }
    public virtual void OnEnemyHit() { }
}
