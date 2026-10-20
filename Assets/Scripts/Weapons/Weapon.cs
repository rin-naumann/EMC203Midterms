using UnityEngine;

public class Weapon : MonoBehaviour
{
    private WeaponData data;
    private Animator animator;
    private SpriteRenderer sprite;

    public WeaponData Data => data;
    public Animator Animator => animator;
}
