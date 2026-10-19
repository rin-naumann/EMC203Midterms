using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform body;
    private Animator animator;
    private bool isMoving;

    void Start()
    {
        animator = body.GetComponent<Animator>();
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        isMoving = input.sqrMagnitude > 0f;

        if (isMoving) transform.position += (Vector3) (input.normalized * (moveSpeed * Time.deltaTime));
        animator.SetBool(IsMovingHash, isMoving);

        if (input.x < 0f) body.localRotation = Quaternion.Euler(0f, 180f, 0f);
        else if (input.x > 0f) body.localRotation = Quaternion.identity;
    }
}