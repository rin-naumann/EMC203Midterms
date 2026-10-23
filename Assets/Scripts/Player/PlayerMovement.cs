using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    [Tooltip("The child object with the SpriteRenderer and Animator. Only this part turns to face left or right.")]
    [SerializeField] private Transform body;
    private Animator animator;
    private float moveSpeed;
    private bool isMoving;

    public Vector3 Facing => body.right;

    private void Start()
    {
        animator = body.GetComponent<Animator>();
        moveSpeed = Player.Instance.Data.MoveSpeed;
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