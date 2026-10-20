using System.Collections.Generic;
using UnityEngine;

public class WeaponData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private float damage;
    [SerializeField] private float attackCooldown;
    [SerializeField] private Sprite sprite;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private List<WeaponUpgrade> upgrades;

    public string DisplayName => displayName;
    public string Description => description;
    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public Sprite Sprite => sprite;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public List<WeaponUpgrade> Upgrades => upgrades;
}
